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
import kotlinx.coroutines.flow.catch
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
        observeFailedSyncCount()
    }

    private fun observeFailedSyncCount() {
        viewModelScope.launch {
            outboxDao.observeOperationCounts()
                .catch { e -> Timber.w(e, "Could not observe outbox counts") }
                .collect { counts ->
                    val failedCount = counts
                        .filter {
                            it.status == OutboxStatus.FAILED ||
                                it.status == OutboxStatus.FAILED_CONFLICT
                        }
                        .sumOf { it.count }
                    _uiState.update { it.copy(failedSyncCount = failedCount) }
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
                        enableRecipeManagement = settings.enableRecipeManagement,
                        exchangeRateLbpPerUsd = settings.exchangeRateLbpPerUsd
                            .stripTrailingZeros().toPlainString(),
                        showLbpOnReceipt = settings.showLbpOnReceipt,
                        stampDutyEnabled = settings.stampDutyEnabled,
                        stampDutyAmountUsd = settings.stampDutyAmountUsd
                            .stripTrailingZeros().toPlainString()
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
                SettingsField.ExchangeRateLbpPerUsd -> state.copy(exchangeRateLbpPerUsd = value)
                SettingsField.StampDutyAmountUsd -> state.copy(stampDutyAmountUsd = value)
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
                SettingsToggle.ShowLbpOnReceipt -> state.copy(showLbpOnReceipt = value)
                SettingsToggle.StampDutyEnabled -> state.copy(stampDutyEnabled = value)
            }
        }
    }

    /**
     * One-tap Lebanese market preset:
     * sets VAT to 11%, enables LBP on receipt, enables stamp duty at $2.
     * Operator still needs to enter the current exchange rate manually.
     */
    fun applyLebanonPreset() {
        _uiState.update {
            it.copy(
                taxEnabled = true,
                taxRate = "11",
                showLbpOnReceipt = true,
                stampDutyEnabled = true,
                stampDutyAmountUsd = "2.00"
            )
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
        val exchangeRate = try {
            BigDecimal(state.exchangeRateLbpPerUsd.trim().ifBlank { "0" })
        } catch (_: Exception) {
            _uiState.update { it.copy(error = "Invalid exchange rate") }
            return
        }
        val stampDutyAmount = try {
            BigDecimal(state.stampDutyAmountUsd.trim().ifBlank { "2.00" })
        } catch (_: Exception) {
            _uiState.update { it.copy(error = "Invalid stamp duty amount") }
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
                enableRecipeManagement = state.enableRecipeManagement,
                exchangeRateLbpPerUsd = exchangeRate,
                showLbpOnReceipt = state.showLbpOnReceipt,
                stampDutyEnabled = state.stampDutyEnabled,
                stampDutyAmountUsd = stampDutyAmount
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
    // Lebanese market
    val exchangeRateLbpPerUsd: String = "0",
    val showLbpOnReceipt: Boolean = false,
    val stampDutyEnabled: Boolean = false,
    val stampDutyAmountUsd: String = "2.00",
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
    ReceiptFooter,
    ExchangeRateLbpPerUsd,
    StampDutyAmountUsd
}

enum class SettingsToggle {
    TaxEnabled,
    RequireCustomerInfo,
    EnableInventoryTracking,
    EnableRecipeManagement,
    ShowLbpOnReceipt,
    StampDutyEnabled
}
