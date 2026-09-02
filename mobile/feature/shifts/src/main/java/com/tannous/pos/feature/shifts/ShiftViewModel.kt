package com.tannous.pos.feature.shifts

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.ShiftDto
import com.tannous.pos.core.data.repository.OrderRepository
import com.tannous.pos.core.data.repository.SettingsRepository
import com.tannous.pos.core.data.repository.ShiftRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import timber.log.Timber
import java.math.BigDecimal
import javax.inject.Inject

@HiltViewModel
class ShiftViewModel @Inject constructor(
    private val shiftRepository: ShiftRepository,
    private val settingsRepository: SettingsRepository,
    private val orderRepository: OrderRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow(ShiftUiState())
    val uiState: StateFlow<ShiftUiState> = _uiState.asStateFlow()

    init {
        loadCurrency()
        loadActiveShift()
    }

    /**
     * Starts observing paid orders for [shiftId] from Room.
     * Automatically updates [ShiftUiState.shiftSalesTotal] and [ShiftUiState.shiftOrderCount]
     * whenever a new order is finalized in [SellViewModel], since both share the same Room database.
     */
    private fun observeShiftSales(shiftId: String) {
        viewModelScope.launch {
            orderRepository.getShiftOrders(shiftId).collect { orders ->
                // Backend sends OrderStatus as integer: 6 = Paid (see OrderStatusExtensions.kt).
                // With isLenient=true, kotlinx.serialization coerces the integer to string "6".
                // Also accept "Paid"/"PAID" for future-proofing if the backend ever switches to
                // string enum serialization.
                val paidStatuses = setOf("6", "Paid", "PAID")
                val paidOrders = orders.filter { it.status in paidStatuses }
                _uiState.update { it.copy(
                    shiftOrderCount = paidOrders.size,
                    shiftSalesTotal = paidOrders.fold(BigDecimal.ZERO) { acc, order -> acc + order.total }
                ) }
            }
        }
    }

    private fun loadCurrency() {
        viewModelScope.launch {
            try {
                settingsRepository.getSettings()
            } catch (e: Exception) {
                Timber.w(e, "Settings warm-up failed — using defaults")
            }
            try {
                val currency = settingsRepository.getCurrency()
                _uiState.update { it.copy(currencyCode = currency) }
            } catch (e: Exception) {
                Timber.w(e, "Could not load currency for shifts; using default")
            }
        }
    }
    
    fun loadActiveShift() {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            try {
                val shift = shiftRepository.getActiveShift()
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    activeShift = shift,
                    errorMessage = null
                )
                if (shift != null) {
                    observeShiftSales(shift.id)
                }
            } catch (e: Exception) {
                Timber.e(e, "Error loading active shift")
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    errorMessage = "Failed to load shift: ${e.message}"
                )
            }
        }
    }
    
    fun openShift(openingBalance: BigDecimal, openingBalanceLbp: BigDecimal = BigDecimal.ZERO, notes: String? = null) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            try {
                val result = shiftRepository.openShift(openingBalance, openingBalanceLbp, notes)
                result.fold(
                    onSuccess = { shift ->
                        _uiState.value = _uiState.value.copy(
                            isLoading = false,
                            activeShift = shift,
                            errorMessage = null
                        )
                        observeShiftSales(shift.id)
                    },
                    onFailure = { error ->
                        Timber.e(error, "Error opening shift")
                        _uiState.value = _uiState.value.copy(
                            isLoading = false,
                            errorMessage = error.message ?: "Failed to open shift"
                        )
                    }
                )
            } catch (e: Exception) {
                Timber.e(e, "Unexpected error opening shift")
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    errorMessage = "Failed to open shift: ${e.message}"
                )
            }
        }
    }
    
    fun closeShift(shiftId: String, closingCount: BigDecimal, closingCountLbp: BigDecimal = BigDecimal.ZERO, note: String? = null) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            try {
                val result = shiftRepository.closeShift(shiftId, closingCount, closingCountLbp, note)
                result.fold(
                    onSuccess = { shift ->
                        _uiState.value = _uiState.value.copy(
                            isLoading = false,
                            activeShift = null, // Shift is now closed
                            errorMessage = null
                        )
                    },
                    onFailure = { error ->
                        Timber.e(error, "Error closing shift")
                        _uiState.value = _uiState.value.copy(
                            isLoading = false,
                            errorMessage = error.message ?: "Failed to close shift"
                        )
                    }
                )
            } catch (e: Exception) {
                Timber.e(e, "Unexpected error closing shift")
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    errorMessage = "Failed to close shift: ${e.message}"
                )
            }
        }
    }
    
    fun cashDrop(shiftId: String, amount: BigDecimal, currency: String = "USD", note: String? = null) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, errorMessage = null)
            try {
                val result = shiftRepository.cashDrop(shiftId, amount, currency, note)
                result.fold(
                    onSuccess = { shift ->
                        _uiState.value = _uiState.value.copy(
                            isLoading = false,
                            activeShift = shift,
                            errorMessage = null
                        )
                    },
                    onFailure = { error ->
                        Timber.e(error, "Error recording cash drop")
                        _uiState.value = _uiState.value.copy(
                            isLoading = false,
                            errorMessage = error.message ?: "Failed to record cash drop"
                        )
                    }
                )
            } catch (e: Exception) {
                Timber.e(e, "Unexpected error recording cash drop")
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    errorMessage = "Failed to record cash drop: ${e.message}"
                )
            }
        }
    }

    fun clearError() {
        _uiState.value = _uiState.value.copy(errorMessage = null)
    }
}

data class ShiftUiState(
    val isLoading: Boolean = false,
    val activeShift: ShiftDto? = null,
    val errorMessage: String? = null,
    val currencyCode: String = "USD",
    /** Number of paid orders in this shift (live, from local Room). */
    val shiftOrderCount: Int = 0,
    /** Sum of totals from paid orders in this shift (live, from local Room). */
    val shiftSalesTotal: BigDecimal = BigDecimal.ZERO
)

