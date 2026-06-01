package com.tannous.pos.feature.sell

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.local.entity.OrderEntity
import com.tannous.pos.core.data.repository.OrderRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

sealed class OrderReceiptState {
    data object Loading : OrderReceiptState()
    data class Found(val entity: OrderEntity) : OrderReceiptState()
    data object NotFound : OrderReceiptState()
}

@HiltViewModel
class OrderReceiptViewModel @Inject constructor(
    private val orderRepository: OrderRepository
) : ViewModel() {

    private val _state = MutableStateFlow<OrderReceiptState>(OrderReceiptState.Loading)
    val state: StateFlow<OrderReceiptState> = _state.asStateFlow()

    fun loadOrder(id: String) {
        viewModelScope.launch {
            _state.value = OrderReceiptState.Loading
            val entity = orderRepository.getOrderById(id)
            _state.value = if (entity != null) {
                OrderReceiptState.Found(entity)
            } else {
                OrderReceiptState.NotFound
            }
        }
    }
}
