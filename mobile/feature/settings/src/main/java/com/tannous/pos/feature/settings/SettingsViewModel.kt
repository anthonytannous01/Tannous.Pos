package com.tannous.pos.feature.settings

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.local.dao.OutboxDao
import com.tannous.pos.core.data.local.entity.OutboxStatus
import com.tannous.pos.core.data.model.UpdateSettingsRequest
import com.tannous.pos.core.data.repository.SettingsRepository
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
class SettingsViewModel @Inject constructor(
    private val settingsRepository: SettingsRepository,
    private val outboxDao: OutboxDao
) : ViewModel() {

    private val _uiState = MutableStateFlow(SettingsUiState())
    val uiState: StateFlow<SettingsUiState> = _uiState.asStateFlow()

    init {
        loadSettings()
        loadFailedSyncCount()
    }

    private fun loadFailedSyncCount() {
        viewModelScope.launch {
            try {
                val counts = outboxDao.getOperationCounts()
                val failedCount = counts
                    .filter {
                        it.status == OutboxStatus.FAILED ||
                            it.status == OutboxStatus.FAILED_CONFLICT
                    }
                    .sumOf { it.count }
                _uiState.update { it.copy(failedSyncCount = failedCount) }
            } catch (e: Exception) {
                Timber.w(e, "Could not load outbox counts")
            }
        }
    }

    fun loadSettings() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                val settings = settingsRepository.getSettings()
                _uiState.update {
                    it.copy(
                        isLoading = false,
                        storeName = settings.storeName,
                        address = settings.address.orEmpty(),
                        phone = settings.phone.orEmpty(),
                        email = settings.email.orEmpty(),
                        website = settings.website.orEmpty(),
                        taxNumber = settings.taxNumber.orEmpty(),
                        taxRate = settings.taxRate.stripTrailingZeros().toPlainString(),
                        currency = settings.currency,
                        taxEnabled = settings.taxEnabled,
                        receiptHeader = settings.receiptHeader.orEmpty(),
                        receiptFooter = settings.receiptFooter.orEmpty(),
                        requireCustomerInfo = settings.requireCustomerInfo,
                        enableInventoryTracking = settings.enableInventoryTracking,
                        enableRecipeManagement = settings.enableRecipeManagement
                    )
                }
            } catch (e: Exception) {
                _uiState.update {
                    it.copy(
                        isLoading = false,
                        error = e.message ?: "Failed to load settings"
                    )
                }
            }
        }
    }

    fun onFieldChange(field: SettingsField, value: String) {
        _uiState.update { state ->
            when (field) {
                SettingsField.StoreName -> state.copy(storeName = value)
                SettingsField.Address -> state.copy(address = value)
                SettingsField.Phone -> state.copy(phone = value)
                SettingsField.Email -> state.copy(email = value)
                SettingsField.Website -> state.copy(website = value)
                SettingsField.TaxNumber -> state.copy(taxNumber = value)
                SettingsField.TaxRate -> state.copy(taxRate = value)
                SettingsField.Currency -> state.copy(currency = value)
                SettingsField.ReceiptHeader -> state.copy(receiptHeader = value)
                SettingsField.ReceiptFooter -> state.copy(receiptFooter = value)
            }
        }
    }

    fun onToggleChange(field: SettingsToggle, value: Boolean) {
        _uiState.update { state ->
            when (field) {
                SettingsToggle.TaxEnabled -> state.copy(taxEnabled = value)
                SettingsToggle.RequireCustomerInfo -> state.copy(requireCustomerInfo = value)
                SettingsToggle.EnableInventoryTracking -> state.copy(enableInventoryTracking = value)
                SettingsToggle.EnableRecipeManagement -> state.copy(enableRecipeManagement = value)
            }
        }
    }

    fun saveSettings() {
        val state = _uiState.value
        if (state.storeName.isBlank()) {
            _uiState.update { it.copy(error = "Store name is required") }
            return
        }
        val taxRatePercent = try {
            BigDecimal(state.taxRate.trim())
        } catch (_: Exception) {
            _uiState.update { it.copy(error = "Invalid tax rate") }
            return
        }

        viewModelScope.launch {
            _uiState.update { it.copy(isSaving = true, error = null, saveSuccess = false) }
            val request = UpdateSettingsRequest(
                storeName = state.storeName.trim(),
                address = state.address.takeIf { it.isNotBlank() },
                phone = state.phone.takeIf { it.isNotBlank() },
                email = state.email.takeIf { it.isNotBlank() },
                website = state.website.takeIf { it.isNotBlank() },
                taxNumber = state.taxNumber.takeIf { it.isNotBlank() },
                taxRate = taxRatePercent,
                currency = state.currency.trim().uppercase(),
                taxEnabled = state.taxEnabled,
                receiptHeader = state.receiptHeader.takeIf { it.isNotBlank() },
                receiptFooter = state.receiptFooter.takeIf { it.isNotBlank() },
                requireCustomerInfo = state.requireCustomerInfo,
                enableInventoryTracking = state.enableInventoryTracking,
                enableRecipeManagement = state.enableRecipeManagement
            )
            val result = settingsRepository.updateSettings(request)
            _uiState.update {
                it.copy(
                    isSaving = false,
                    saveSuccess = result.isSuccess,
                    error = result.exceptionOrNull()?.message
                )
            }
        }
    }

    fun clearError() {
        _uiState.update { it.copy(error = null, saveSuccess = false) }
    }
}

data class SettingsUiState(
    val isLoading: Boolean = false,
    val isSaving: Boolean = false,
    val error: String? = null,
    val saveSuccess: Boolean = false,
    val storeName: String = "",
    val address: String = "",
    val phone: String = "",
    val email: String = "",
    val website: String = "",
    val taxNumber: String = "",
    val taxRate: String = "10",
    val currency: String = "USD",
    val taxEnabled: Boolean = true,
    val receiptHeader: String = "",
    val receiptFooter: String = "",
    val requireCustomerInfo: Boolean = false,
    val enableInventoryTracking: Boolean = true,
    val enableRecipeManagement: Boolean = false,
    val failedSyncCount: Int = 0
)

enum class SettingsField {
    StoreName,
    Address,
    Phone,
    Email,
    Website,
    TaxNumber,
    TaxRate,
    Currency,
    ReceiptHeader,
    ReceiptFooter
}

enum class SettingsToggle {
    TaxEnabled,
    RequireCustomerInfo,
    EnableInventoryTracking,
    EnableRecipeManagement
}
