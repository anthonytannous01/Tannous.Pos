package com.tannous.pos.feature.sell

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.local.entity.AddOnEntity
import com.tannous.pos.core.data.local.entity.CategoryEntity
import com.tannous.pos.core.data.local.entity.MenuItemEntity
import com.tannous.pos.core.data.model.OrderDto
import com.tannous.pos.core.data.model.PaymentDto
import com.tannous.pos.core.data.repository.CatalogRepository
import com.tannous.pos.core.data.repository.OrderRepository
import com.tannous.pos.core.data.repository.SettingsRepository
import com.tannous.pos.core.data.repository.ShiftRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import timber.log.Timber
import java.math.BigDecimal
import javax.inject.Inject

@HiltViewModel
class SellViewModel @Inject constructor(
    private val catalogRepository: CatalogRepository,
    private val orderRepository: OrderRepository,
    private val shiftRepository: ShiftRepository,
    private val settingsRepository: SettingsRepository
) : ViewModel() {
    
    private val _uiState = MutableStateFlow(SellUiState())
    val uiState: StateFlow<SellUiState> = _uiState.asStateFlow()
    
    private val _selectedCategory = MutableStateFlow<CategoryEntity?>(null)
    val selectedCategory: StateFlow<CategoryEntity?> = _selectedCategory.asStateFlow()
    
    private val _cartItems = MutableStateFlow<List<CartItem>>(emptyList())
    val cartItems: StateFlow<List<CartItem>> = _cartItems.asStateFlow()
    
    private val _finalizedOrder = MutableStateFlow<OrderDto?>(null)
    val finalizedOrder: StateFlow<OrderDto?> = _finalizedOrder.asStateFlow()
    
    init {
        loadCategories()
        observeCatalogData()
        observeAddOns()
        loadCurrency()
    }

    private fun loadCurrency() {
        viewModelScope.launch {
            try {
                settingsRepository.getSettings() // warm the tax-rate/currency cache (best-effort)
            } catch (e: Exception) {
                Timber.w(e, "Settings warm-up failed — using defaults")
            }
            try {
                val currency = settingsRepository.getCurrency()
                _uiState.update { it.copy(currencyCode = currency) }
            } catch (e: Exception) {
                Timber.w(e, "Could not load currency; using default")
            }
        }
    }
    
    private fun loadCategories() {
        viewModelScope.launch {
            try {
                catalogRepository.getAllCategories()
                    .collect { categories ->
                        _uiState.update { it.copy(categories = categories) }
                        if (categories.isNotEmpty() && _selectedCategory.value == null) {
                            _selectedCategory.value = categories.first()
                        }
                    }
            } catch (e: Exception) {
                Timber.e(e, "Error loading categories")
                _uiState.update { it.copy(error = e.message) }
            }
        }
    }
    
    private fun observeCatalogData() {
        viewModelScope.launch {
            try {
                catalogRepository.getAllMenuItems()
                    .collect { menuItems ->
                        _uiState.update { it.copy(menuItems = menuItems) }
                    }
            } catch (e: Exception) {
                Timber.e(e, "Error loading menu items")
                _uiState.update { it.copy(error = e.message) }
            }
        }
    }

    private fun observeAddOns() {
        viewModelScope.launch {
            try {
                catalogRepository.getAllAddOns()
                    .collect { addOns ->
                        _uiState.update { it.copy(availableAddOns = addOns) }
                    }
            } catch (e: Exception) {
                Timber.e(e, "Error loading add-ons")
            }
        }
    }
    
    fun selectCategory(category: CategoryEntity) {
        _selectedCategory.value = category
    }
    
    fun addItemToCart(menuItem: MenuItemEntity, selectedAddOns: List<CartAddOn> = emptyList()) {
        val currentCart = _cartItems.value.toMutableList()
        val existingItem = currentCart.find { it.menuItem.id == menuItem.id }
        
        if (existingItem != null) {
            val index = currentCart.indexOf(existingItem)
            currentCart[index] = existingItem.copy(quantity = existingItem.quantity + 1)
        } else {
            currentCart.add(
                CartItem(menuItem = menuItem, quantity = 1, addOns = selectedAddOns)
            )
        }
        
        _cartItems.value = currentCart
        updateCartTotal()
    }
    
    fun removeItemFromCart(menuItem: MenuItemEntity) {
        val currentCart = _cartItems.value.toMutableList()
        val existingItem = currentCart.find { it.menuItem.id == menuItem.id }
        
        if (existingItem != null) {
            if (existingItem.quantity > 1) {
                val index = currentCart.indexOf(existingItem)
                currentCart[index] = existingItem.copy(quantity = existingItem.quantity - 1)
            } else {
                currentCart.remove(existingItem)
            }
        }
        
        _cartItems.value = currentCart
        updateCartTotal()
    }
    
    fun clearCart() {
        _cartItems.value = emptyList()
        updateCartTotal()
    }
    
    private fun updateCartTotal() {
        val total = _cartItems.value.sumOf { item ->
            val itemTotal = item.menuItem.price.toDouble() * item.quantity
            val addOnsTotal = item.addOns.sumOf { addOn -> 
                addOn.price.toDouble() * addOn.quantity
            }
            itemTotal + addOnsTotal
        }
        _uiState.update { it.copy(cartTotal = total) }
    }
    
    fun refreshCatalogData() {
        viewModelScope.launch {
            try {
                _uiState.update { it.copy(isLoading = true, error = null) }
                catalogRepository.refreshAllCatalogData()
                _uiState.update { it.copy(isLoading = false) }
            } catch (e: Exception) {
                Timber.e(e, "Error refreshing catalog data")
                _uiState.update { it.copy(isLoading = false, error = e.message) }
            }
        }
    }
    
    fun clearError() {
        _uiState.update { it.copy(error = null) }
    }
    
    /**
     * Creates an order from cart items and finalizes it with the given payments.
     * Returns Result with finalized OrderDto on success.
     */
    fun finalizeOrder(payments: List<PaymentDto>) {
        viewModelScope.launch {
            try {
                _uiState.update { it.copy(isLoading = true, isFinalizing = true, error = null) }
                
                // Get active shift
                val activeShift = shiftRepository.getActiveShift()
                if (activeShift == null) {
                    _uiState.update { 
                        it.copy(
                            isLoading = false,
                            isFinalizing = false,
                            error = "No active shift. Please open a shift first."
                        )
                    }
                    return@launch
                }
                
                // Convert cart items to repository format
                val cartItemsForOrder = _cartItems.value.map { cartItem ->
                    com.tannous.pos.core.data.repository.CartItem(
                        menuItem = cartItem.menuItem,
                        quantity = cartItem.quantity,
                        addOns = cartItem.addOns.map { addOn ->
                            com.tannous.pos.core.data.repository.CartAddOn(
                                id = addOn.id,
                                name = addOn.name,
                                price = BigDecimal.valueOf(addOn.price),
                                quantity = addOn.quantity
                            )
                        }
                    )
                }
                
                // Create order from cart
                val createResult = orderRepository.createOrderFromCart(
                    shiftId = activeShift.id,
                    cartItems = cartItemsForOrder
                )
                
                if (createResult.isFailure) {
                    _uiState.update { 
                        it.copy(
                            isLoading = false,
                            isFinalizing = false,
                            error = createResult.exceptionOrNull()?.message ?: "Failed to create order"
                        )
                    }
                    return@launch
                }
                
                val orderId = createResult.getOrThrow()
                
                // Finalize order
                val finalizeResult = orderRepository.finalizeOrder(orderId, payments)
                
                if (finalizeResult.isFailure) {
                    val error = finalizeResult.exceptionOrNull()
                    val errorMessage = when {
                        error is java.net.UnknownHostException || error is java.net.ConnectException -> {
                            "Network error. Order queued for sync when connection is restored."
                        }
                        error?.message?.contains("401") == true -> {
                            "Authentication error. Please login again."
                        }
                        error?.message?.contains("403") == true -> {
                            "You don't have permission to finalize orders."
                        }
                        error?.message?.contains("409") == true -> {
                            "Order conflict. Please try again."
                        }
                        else -> error?.message ?: "Failed to finalize order"
                    }
                    
                    _uiState.update { 
                        it.copy(
                            isLoading = false,
                            isFinalizing = false,
                            error = errorMessage
                        )
                    }
                    return@launch
                }
                
                val finalizedOrder = finalizeResult.getOrThrow()
                _finalizedOrder.value = finalizedOrder
                
                // Clear cart on success
                clearCart()
                
                _uiState.update { it.copy(isLoading = false, isFinalizing = false, error = null) }
                
                Timber.d("Order finalized successfully: ${finalizedOrder.id}, Receipt: ${finalizedOrder.receiptNumber}")
                
            } catch (e: Exception) {
                Timber.e(e, "Error finalizing order")
                _uiState.update { 
                    it.copy(
                        isLoading = false,
                        isFinalizing = false,
                        error = e.message ?: "An unexpected error occurred"
                    )
                }
            }
        }
    }
    
    fun clearFinalizedOrder() {
        _finalizedOrder.value = null
    }
}

data class SellUiState(
    val categories: List<CategoryEntity> = emptyList(),
    val menuItems: List<MenuItemEntity> = emptyList(),
    val availableAddOns: List<AddOnEntity> = emptyList(),
    val cartTotal: Double = 0.0,
    val isLoading: Boolean = false,
    val error: String? = null,
    val isFinalizing: Boolean = false,
    val currencyCode: String = "USD"
)

data class CartItem(
    val menuItem: MenuItemEntity,
    val quantity: Int,
    val addOns: List<CartAddOn>
)

data class CartAddOn(
    val id: String,
    val name: String,
    val price: Double,
    val quantity: Int
)
