package com.tannous.pos.feature.sell

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.PaymentDto
import com.tannous.pos.core.data.model.RecordSplitPaymentRequest
import com.tannous.pos.core.data.model.SplitBillDto
import com.tannous.pos.core.data.remote.OrderService
import com.tannous.pos.core.data.repository.OrderRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import timber.log.Timber
import java.math.BigDecimal
import java.math.RoundingMode
import javax.inject.Inject

enum class SplitPaymentMethod(val apiLabel: String) {
    Cash("Cash"),
    Card("Card"),
    LbpCash("LBP Cash")
}

enum class SplitBillStep {
    ChooseSplit,
    CollectPayment
}

@HiltViewModel
class SplitBillViewModel @Inject constructor(
    private val orderService: OrderService,
    private val orderRepository: OrderRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow(SplitBillState())
    val uiState: StateFlow<SplitBillState> = _uiState.asStateFlow()

    private var loadJob: Job? = null
    private var orderId: String = ""

    fun initialize(orderId: String) {
        if (this.orderId == orderId && _uiState.value.splitData != null) return
        this.orderId = orderId
        loadSplit(orderId, _uiState.value.selectedWays)
    }

    fun loadSplit(orderId: String, ways: Int) {
        this.orderId = orderId
        loadJob?.cancel()
        loadJob = viewModelScope.launch {
            delay(300)
            _uiState.update { it.copy(isLoading = true, error = null, selectedWays = ways) }
            try {
                val split = orderService.getSplitBill(orderId, ways)
                _uiState.update {
                    it.copy(
                        isLoading = false,
                        splitData = split,
                        currentPerson = nextUnpaidPerson(split),
                        tenderedAmount = defaultTendered(split)
                    )
                }
            } catch (e: Exception) {
                Timber.e(e, "Error loading split bill")
                _uiState.update {
                    it.copy(
                        isLoading = false,
                        error = e.message ?: "Could not load split bill"
                    )
                }
            }
        }
    }

    fun incrementWays() {
        val ways = (_uiState.value.selectedWays + 1).coerceAtMost(20)
        if (orderId.isNotBlank()) loadSplit(orderId, ways)
    }

    fun decrementWays() {
        val ways = (_uiState.value.selectedWays - 1).coerceAtLeast(2)
        if (orderId.isNotBlank()) loadSplit(orderId, ways)
    }

    fun continueToCollect() {
        _uiState.update { it.copy(step = SplitBillStep.CollectPayment) }
    }

    fun setPaymentMethod(method: SplitPaymentMethod) {
        _uiState.update { it.copy(selectedMethod = method) }
    }

    fun setTenderedAmount(value: String) {
        _uiState.update { it.copy(tenderedAmount = value) }
    }

    fun setCustomAmountEnabled(enabled: Boolean) {
        _uiState.update { state ->
            val amount = if (enabled) {
                state.customAmount.ifBlank { paymentAmount(state).toPlainString() }
            } else {
                state.customAmount
            }
            state.copy(
                useCustomAmount = enabled,
                customAmount = amount,
                tenderedAmount = if (enabled) amount else defaultTendered(state.splitData)
            )
        }
    }

    fun setCustomAmount(value: String) {
        _uiState.update {
            it.copy(
                customAmount = value,
                tenderedAmount = if (it.useCustomAmount) value else it.tenderedAmount
            )
        }
    }

    fun recordPayment() {
        val state = _uiState.value
        val split = state.splitData ?: return
        if (orderId.isBlank()) return

        val amount = paymentAmount(state)
        if (amount <= BigDecimal.ZERO) {
            _uiState.update { it.copy(error = "Enter a valid payment amount") }
            return
        }

        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                val updated = orderService.recordSplitPayment(
                    orderId,
                    RecordSplitPaymentRequest(
                        totalWays = state.selectedWays,
                        amount = amount,
                        method = state.selectedMethod.apiLabel
                    )
                )
                if (updated.isFullyPaid) {
                    finalizeSplitOrder()
                } else {
                    _uiState.update {
                        it.copy(
                            isLoading = false,
                            splitData = updated,
                            currentPerson = nextUnpaidPerson(updated),
                            tenderedAmount = defaultTendered(updated),
                            customAmount = "",
                            useCustomAmount = false
                        )
                    }
                }
            } catch (e: Exception) {
                Timber.e(e, "Split payment failed")
                _uiState.update {
                    it.copy(isLoading = false, error = e.message ?: "Payment failed")
                }
            }
        }
    }

    private suspend fun finalizeSplitOrder() {
        // Every person's payment is already a row on the order, so there is nothing to send.
        // settleRecordedPayments tells the server that an empty list is intentional; without it
        // the request validator rejected this call as "At least one payment is required" and the
        // final person's payment appeared to fail with a 400.
        val result = orderRepository.finalizeOrder(
            orderId = orderId,
            payments = emptyList(),
            settleRecordedPayments = true
        )
        result.fold(
            onSuccess = { order ->
                _uiState.update {
                    it.copy(isLoading = false, isComplete = true, finalizedOrder = order)
                }
            },
            onFailure = { error ->
                _uiState.update {
                    it.copy(isLoading = false, error = error.message ?: "Failed to finalize order")
                }
            }
        )
    }

    fun clearError() {
        _uiState.update { it.copy(error = null) }
    }

    fun changeDue(state: SplitBillState): BigDecimal {
        val tendered = state.tenderedAmount.toBigDecimalOrNull() ?: BigDecimal.ZERO
        val due = paymentAmount(state)
        return (tendered - due).coerceAtLeast(BigDecimal.ZERO)
            .setScale(2, RoundingMode.HALF_UP)
    }

    private fun paymentAmount(state: SplitBillState): BigDecimal {
        if (state.useCustomAmount) {
            return state.customAmount.toBigDecimalOrNull()?.setScale(2, RoundingMode.HALF_UP)
                ?: BigDecimal.ZERO
        }
        return state.splitData?.amountPerPerson ?: BigDecimal.ZERO
    }

    private fun nextUnpaidPerson(split: SplitBillDto): Int =
        split.portions.firstOrNull { !it.isPaid }?.personNumber ?: split.ways

    private fun defaultTendered(split: SplitBillDto?): String =
        split?.amountPerPerson?.setScale(2, RoundingMode.HALF_UP)?.toPlainString() ?: ""
}

data class SplitBillState(
    val isLoading: Boolean = false,
    val splitData: SplitBillDto? = null,
    val selectedWays: Int = 2,
    val currentPerson: Int = 1,
    val selectedMethod: SplitPaymentMethod = SplitPaymentMethod.Cash,
    val tenderedAmount: String = "",
    val useCustomAmount: Boolean = false,
    val customAmount: String = "",
    val step: SplitBillStep = SplitBillStep.ChooseSplit,
    val error: String? = null,
    val isComplete: Boolean = false,
    val finalizedOrder: com.tannous.pos.core.data.model.OrderDto? = null
)
