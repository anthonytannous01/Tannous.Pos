package com.tannous.pos.feature.inventory

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.InventoryItemDto
import com.tannous.pos.core.data.repository.InventoryRepository
import com.tannous.pos.core.data.repository.SettingsRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import java.math.BigDecimal
import javax.inject.Inject

enum class InventoryFilter {
    All,
    LowStock
}

enum class InventoryAction {
    Adjust,
    Wastage
}

@HiltViewModel
class InventoryViewModel @Inject constructor(
    private val inventoryRepository: InventoryRepository,
    private val settingsRepository: SettingsRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow(InventoryUiState())
    val uiState: StateFlow<InventoryUiState> = _uiState.asStateFlow()

    init {
        viewModelScope.launch {
            val currency = settingsRepository.getCurrency()
            _uiState.update { it.copy(currencyCode = currency) }
        }
        load()
    }

    fun load(filter: InventoryFilter = _uiState.value.filter) {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            val result = inventoryRepository.getInventoryItems(
                lowStockOnly = filter == InventoryFilter.LowStock
            )
            result.fold(
                onSuccess = { items ->
                    _uiState.update {
                        it.copy(isLoading = false, items = items, filter = filter)
                    }
                },
                onFailure = { e ->
                    _uiState.update {
                        it.copy(isLoading = false, error = e.message ?: "Failed to load inventory")
                    }
                }
            )
        }
    }

    fun setFilter(filter: InventoryFilter) {
        load(filter)
    }

    fun openAction(item: InventoryItemDto, action: InventoryAction) {
        _uiState.update {
            it.copy(
                actionItem = item,
                actionType = action,
                submitError = null,
                submitSuccess = null
            )
        }
    }

    fun dismissAction() {
        _uiState.update {
            it.copy(
                actionItem = null,
                actionType = null,
                submitError = null,
                submitSuccess = null
            )
        }
    }

    fun submitAction(quantity: BigDecimal, reason: String) {
        val item = _uiState.value.actionItem ?: return
        val action = _uiState.value.actionType ?: return
        val filter = _uiState.value.filter

        viewModelScope.launch {
            _uiState.update { it.copy(isSubmitting = true, submitError = null) }
            try {
                when (action) {
                    InventoryAction.Adjust ->
                        inventoryRepository.adjustStock(item.ingredientId, quantity, reason)
                    InventoryAction.Wastage ->
                        inventoryRepository.recordWastage(item.ingredientId, quantity, reason)
                }
                val label = if (action == InventoryAction.Adjust) "Adjustment" else "Wastage"
                _uiState.update {
                    it.copy(
                        isSubmitting = false,
                        actionItem = null,
                        actionType = null,
                        submitSuccess = "$label queued for sync"
                    )
                }
                load(filter)
            } catch (e: Exception) {
                _uiState.update {
                    it.copy(
                        isSubmitting = false,
                        submitError = e.message ?: "Failed to queue operation"
                    )
                }
            }
        }
    }

    fun clearSubmitSuccess() {
        _uiState.update { it.copy(submitSuccess = null) }
    }
}

data class InventoryUiState(
    val items: List<InventoryItemDto> = emptyList(),
    val filter: InventoryFilter = InventoryFilter.All,
    val isLoading: Boolean = false,
    val error: String? = null,
    val currencyCode: String = "USD",
    val actionItem: InventoryItemDto? = null,
    val actionType: InventoryAction? = null,
    val isSubmitting: Boolean = false,
    val submitError: String? = null,
    val submitSuccess: String? = null
)
