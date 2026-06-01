package com.tannous.pos.feature.sell

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.local.entity.OrderEntity
import com.tannous.pos.core.data.repository.OrderRepository
import com.tannous.pos.core.data.repository.SettingsRepository
import com.tannous.pos.core.data.repository.isAlreadyVoidedStatus
import com.tannous.pos.core.data.repository.isVoidableStatus
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import java.io.IOException
import javax.inject.Inject

enum class OrderHistoryFilter {
    All,
    Paid,
    Open,
    Voided,
    PendingSync
}

@HiltViewModel
class OrderHistoryViewModel @Inject constructor(
    private val orderRepository: OrderRepository,
    private val settingsRepository: SettingsRepository
) : ViewModel() {

    private val _rawOrders = MutableStateFlow<List<OrderEntity>>(emptyList())
    private val _uiState = MutableStateFlow(OrderHistoryUiState())
    val uiState: StateFlow<OrderHistoryUiState> = _uiState.asStateFlow()

    init {
        viewModelScope.launch {
            val currency = settingsRepository.getCurrency()
            _uiState.update { it.copy(currencyCode = currency) }
        }
        observeOrders()
        refresh()
    }

    private fun observeOrders() {
        viewModelScope.launch {
            orderRepository.observeAllOrders().collect { all ->
                _rawOrders.value = all
                _uiState.update { state ->
                    state.copy(orders = applyFilter(all, state.filter))
                }
            }
        }
    }

    fun setFilter(filter: OrderHistoryFilter) {
        _uiState.update { state ->
            state.copy(
                filter = filter,
                orders = applyFilter(_rawOrders.value, filter)
            )
        }
    }

    fun refresh() {
        viewModelScope.launch {
            _uiState.update { it.copy(isRefreshing = true, refreshError = null) }
            val result = orderRepository.refreshOrders()
            _uiState.update { state ->
                state.copy(
                    isRefreshing = false,
                    refreshError = result.exceptionOrNull()
                        ?.takeIf { it !is IOException }
                        ?.message
                )
            }
        }
    }

    fun voidOrder(orderId: String, reason: String) {
        viewModelScope.launch {
            _uiState.update { it.copy(voidingOrderId = orderId, voidError = null) }
            val result = orderRepository.voidOrder(orderId, reason)
            _uiState.update {
                it.copy(
                    voidingOrderId = null,
                    voidError = result.exceptionOrNull()?.message
                )
            }
        }
    }

    fun clearVoidError() {
        _uiState.update { it.copy(voidError = null) }
    }

    private fun applyFilter(
        orders: List<OrderEntity>,
        filter: OrderHistoryFilter
    ): List<OrderEntity> = when (filter) {
        OrderHistoryFilter.All -> orders
        OrderHistoryFilter.Paid -> orders.filter {
            it.status in setOf("6", "Paid", "PAID") &&
                !it.receiptNumber.orEmpty().startsWith("PENDING")
        }
        OrderHistoryFilter.Open -> orders.filter {
            it.status in setOf("1", "Open", "OPEN")
        }
        OrderHistoryFilter.Voided -> orders.filter { it.status.isAlreadyVoidedStatus() }
        OrderHistoryFilter.PendingSync -> orders.filter {
            it.receiptNumber.orEmpty().startsWith("PENDING") || it.syncedAt == null
        }
    }
}

data class OrderHistoryUiState(
    val orders: List<OrderEntity> = emptyList(),
    val filter: OrderHistoryFilter = OrderHistoryFilter.All,
    val isRefreshing: Boolean = false,
    val refreshError: String? = null,
    val currencyCode: String = "USD",
    val voidingOrderId: String? = null,
    val voidError: String? = null
)
