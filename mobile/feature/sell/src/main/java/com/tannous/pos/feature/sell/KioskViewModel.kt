package com.tannous.pos.feature.sell

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.KioskOrderLineRequest
import com.tannous.pos.core.data.model.KioskOrderRequest
import com.tannous.pos.core.data.model.KioskOrderResultDto
import com.tannous.pos.core.data.model.PublicMenuCategoryDto
import com.tannous.pos.core.data.model.PublicMenuItemDto
import com.tannous.pos.core.data.remote.KioskService
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
class KioskViewModel @Inject constructor(
    private val kioskService: KioskService
) : ViewModel() {

    private val _uiState = MutableStateFlow(KioskUiState())
    val uiState: StateFlow<KioskUiState> = _uiState.asStateFlow()

    init { loadMenu() }

    fun loadMenu() {
        _uiState.update { it.copy(isLoading = true, error = null) }
        viewModelScope.launch {
            try {
                val menu = kioskService.getMenu()
                _uiState.update {
                    it.copy(
                        isLoading    = false,
                        categories   = menu.categories,
                        businessName = menu.businessName,
                        currency     = menu.currency,
                        selectedCategory = menu.categories.firstOrNull()
                    )
                }
            } catch (e: Exception) {
                Timber.w(e, "Kiosk menu load failed")
                _uiState.update { it.copy(isLoading = false, error = "Could not load menu") }
            }
        }
    }

    fun selectCategory(category: PublicMenuCategoryDto) =
        _uiState.update { it.copy(selectedCategory = category) }

    fun addItem(item: PublicMenuItemDto) {
        val cart = _uiState.value.cart.toMutableList()
        val existing = cart.indexOfFirst { it.item.id == item.id }
        if (existing >= 0) {
            cart[existing] = cart[existing].copy(quantity = cart[existing].quantity + 1)
        } else {
            cart.add(KioskCartLine(item = item, quantity = 1))
        }
        _uiState.update { it.copy(cart = cart) }
    }

    fun removeItem(itemId: String) {
        val cart = _uiState.value.cart.toMutableList()
        val idx = cart.indexOfFirst { it.item.id == itemId }
        if (idx >= 0) {
            if (cart[idx].quantity > 1) cart[idx] = cart[idx].copy(quantity = cart[idx].quantity - 1)
            else cart.removeAt(idx)
        }
        _uiState.update { it.copy(cart = cart) }
    }

    fun clearCart() = _uiState.update { it.copy(cart = emptyList()) }

    fun placeOrder(customerName: String?, notes: String?) {
        val state = _uiState.value
        if (state.cart.isEmpty()) return
        _uiState.update { it.copy(isPlacing = true, placeError = null) }
        viewModelScope.launch {
            try {
                val result = kioskService.placeOrder(KioskOrderRequest(
                    customerName = customerName?.takeIf { it.isNotBlank() },
                    notes        = notes?.takeIf { it.isNotBlank() },
                    lines        = state.cart.map { line ->
                        KioskOrderLineRequest(
                            menuItemId = line.item.id,
                            quantity   = line.quantity,
                            unitPrice  = line.item.price
                        )
                    }
                ))
                _uiState.update { it.copy(isPlacing = false, placedOrder = result, cart = emptyList()) }
            } catch (e: Exception) {
                Timber.w(e, "Kiosk order placement failed")
                _uiState.update { it.copy(isPlacing = false, placeError = "Could not place order. Please try again.") }
            }
        }
    }

    fun resetAfterOrder() = _uiState.update { it.copy(placedOrder = null, placeError = null) }
    fun clearError()      = _uiState.update { it.copy(error = null, placeError = null) }

    val cartTotal: BigDecimal get() = _uiState.value.cart
        .fold(BigDecimal.ZERO) { acc, line -> acc + line.item.price * BigDecimal(line.quantity) }
}

data class KioskCartLine(
    val item:     PublicMenuItemDto,
    val quantity: Int
)

data class KioskUiState(
    val isLoading:       Boolean                      = true,
    val categories:      List<PublicMenuCategoryDto>  = emptyList(),
    val selectedCategory:PublicMenuCategoryDto?       = null,
    val businessName:    String                       = "",
    val currency:        String                       = "USD",
    val cart:            List<KioskCartLine>          = emptyList(),
    val isPlacing:       Boolean                      = false,
    val placedOrder:     KioskOrderResultDto?         = null,
    val error:           String?                      = null,
    val placeError:      String?                      = null
)
