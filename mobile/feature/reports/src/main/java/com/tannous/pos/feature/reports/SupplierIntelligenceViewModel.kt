package com.tannous.pos.feature.reports

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.CreateSuggestedOrdersRequest
import com.tannous.pos.core.data.model.SupplierIntelligenceDto
import com.tannous.pos.core.data.remote.SupplierIntelligenceService
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import timber.log.Timber
import javax.inject.Inject

@HiltViewModel
class SupplierIntelligenceViewModel @Inject constructor(
    private val service: SupplierIntelligenceService
) : ViewModel() {

    private val _uiState = MutableStateFlow(SupplierIntelligenceUiState())
    val uiState: StateFlow<SupplierIntelligenceUiState> = _uiState.asStateFlow()

    init {
        load()
    }

    fun load() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                val data = service.getIntelligence(forecastDays = 7)
                _uiState.update { it.copy(isLoading = false, data = data) }
            } catch (e: Exception) {
                Timber.w(e, "Failed to load supplier intelligence")
                _uiState.update {
                    it.copy(isLoading = false, error = e.message ?: "Failed to load supplier intelligence")
                }
            }
        }
    }

    fun createOrderForSupplier(supplierId: String, onSuccess: (String) -> Unit) {
        viewModelScope.launch {
            _uiState.update { it.copy(creatingOrder = supplierId, error = null) }
            try {
                val result = service.createSuggestedOrders(
                    CreateSuggestedOrdersRequest(
                        forecastDays = 7,
                        supplierIds = listOf(supplierId)
                    )
                )
                _uiState.update { it.copy(creatingOrder = null) }
                if (result.ordersCreated > 0) {
                    val orderNumber = result.orderNumbers.firstOrNull() ?: ""
                    val supplierName = _uiState.value.data?.orderSuggestions
                        ?.firstOrNull { it.supplierId == supplierId }?.supplierName ?: "supplier"
                    onSuccess("PO #$orderNumber created for $supplierName")
                    load()
                } else {
                    _uiState.update {
                        it.copy(error = result.skippedReason ?: "No orders were created")
                    }
                }
            } catch (e: Exception) {
                Timber.w(e, "Failed to create suggested order for $supplierId")
                _uiState.update {
                    it.copy(creatingOrder = null, error = e.message ?: "Failed to create purchase order")
                }
            }
        }
    }

    fun clearError() = _uiState.update { it.copy(error = null) }
}

data class SupplierIntelligenceUiState(
    val isLoading: Boolean = false,
    val data: SupplierIntelligenceDto? = null,
    val error: String? = null,
    val creatingOrder: String? = null
)
