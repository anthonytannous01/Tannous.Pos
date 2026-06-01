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

@HiltViewModel
class OrderReceiptViewModel @Inject constructor(
    private val orderRepository: OrderRepository
) : ViewModel() {

    private val _order = MutableStateFlow<OrderEntity?>(null)
    val order: StateFlow<OrderEntity?> = _order.asStateFlow()

    fun loadOrder(id: String) {
        viewModelScope.launch {
            _order.value = orderRepository.getOrderById(id)
        }
    }
}
