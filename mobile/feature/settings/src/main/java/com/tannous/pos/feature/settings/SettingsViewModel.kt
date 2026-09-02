package com.tannous.pos.feature.settings

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.local.dao.OutboxDao
import com.tannous.pos.core.data.local.entity.OutboxStatus
import com.tannous.pos.core.data.model.PrinterConfig
import com.tannous.pos.core.data.model.PrinterConnectionType
import com.tannous.pos.core.data.model.UpdateSettingsRequest
import com.tannous.pos.core.data.repository.SettingsRepository
import com.tannous.pos.core.printing.PrintResult
import com.tannous.pos.core.printing.PrinterService
import com.tannous.pos.core.printing.TestReceiptFactory
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
    private val outboxDao: OutboxDao,
    private val printerService: PrinterService
) : ViewModel() {

    private val _uiState = MutableStateFlow(SettingsUiState())
    val uiState: StateFlow<SettingsUiState> = _uiState.asStateFlow()

    init {
        loadSettings()
        observeFailedSyncCount()
        loadLanguage()
        loadKioskPin()
        loadPrinterConfig()
    }

    private fun loadPrinterConfig() {
        val config = settingsRepository.getPrinterConfig()
        _uiState.update {
            it.copy(
                printerConnectionType = config.connectionType,
                printerBluetoothAddress = config.bluetoothAddress,
                printerBluetoothDeviceName = config.bluetoothDeviceName,
                printerNetworkHost = config.networkHost.orEmpty(),
                printerNetworkPort = config.networkPort.toString(),
                printerPaperWidthMm = config.paperWidthMm
            )
        }
    }

    private fun persistPrinterConfig(update: (PrinterConfig) -> PrinterConfig) {
        val current = settingsRepository.getPrinterConfig()
        val updated = update(current)
        settingsRepository.setPrinterConfig(updated)
        _uiState.update {
            it.copy(
                printerConnectionType = updated.connectionType,
                printerBluetoothAddress = updated.bluetoothAddress,
                printerBluetoothDeviceName = updated.bluetoothDeviceName,
                printerNetworkHost = updated.networkHost.orEmpty(),
                printerNetworkPort = updated.networkPort.toString(),
                printerPaperWidthMm = updated.paperWidthMm
            )
        }
    }

    fun setPrinterConnectionType(type: PrinterConnectionType) {
        persistPrinterConfig { it.copy(connectionType = type) }
    }

    fun setBluetoothPrinter(name: String, address: String) {
        persistPrinterConfig {
            it.copy(
                connectionType = PrinterConnectionType.BLUETOOTH,
                bluetoothAddress = address,
                bluetoothDeviceName = name
            )
        }
    }

    fun clearBluetoothPrinter() {
        persistPrinterConfig {
            it.copy(bluetoothAddress = null, bluetoothDeviceName = null)
        }
    }

    fun setPrinterNetworkHost(host: String) {
        persistPrinterConfig { it.copy(networkHost = host.ifBlank { null }) }
    }

    fun setPrinterNetworkPort(port: String) {
        val parsed = port.toIntOrNull()?.coerceIn(1, 65535) ?: 9100
        persistPrinterConfig { it.copy(networkPort = parsed) }
    }

    fun setPrinterPaperWidth(widthMm: Int) {
        persistPrinterConfig { it.copy(paperWidthMm = widthMm) }
    }

    fun refreshBondedDevices() {
        // Bonded devices are read in the composable via BluetoothAdapter; state hook for future use.
    }

    fun showBluetoothDeviceSheet() {
        _uiState.update { it.copy(showBluetoothDeviceSheet = true) }
    }

    fun dismissBluetoothDeviceSheet() {
        _uiState.update { it.copy(showBluetoothDeviceSheet = false) }
    }

    fun printTestReceipt() {
        viewModelScope.launch {
            _uiState.update { it.copy(printerPrintState = PrinterPrintState.Printing) }
            val isArabic = settingsRepository.isArabic()
            val result = printerService.printReceipt(TestReceiptFactory.sample())
            _uiState.update {
                it.copy(
                    printerPrintState = when (result) {
                        is PrintResult.Success -> PrinterPrintState.Success(
                            if (isArabic) "تمت الطباعة بنجاح" else "Test receipt printed"
                        )
                        is PrintResult.Failure -> PrinterPrintState.Error(result.message)
                    }
                )
            }
        }
    }

    fun clearPrinterPrintState() {
        _uiState.update { it.copy(printerPrintState = PrinterPrintState.Idle) }
    }

    private fun loadLanguage() {
        viewModelScope.launch {
            val lang = settingsRepository.getLanguage()
            _uiState.update { it.copy(language = lang) }
        }
    }

    private fun loadKioskPin() {
        viewModelScope.launch {
            val pin = settingsRepository.getKioskPin()
            _uiState.update { it.copy(kioskPin = pin) }
        }
    }

    fun saveKioskPin(pin: String) {
        viewModelScope.launch {
            settingsRepository.setKioskPin(pin)
            _uiState.update { it.copy(kioskPin = pin) }
        }
    }

    fun toggleLanguage() {
        val newLang = if (_uiState.value.language == SettingsRepository.LANG_AR)
            SettingsRepository.LANG_EN else SettingsRepository.LANG_AR
        viewModelScope.launch {
            settingsRepository.setLanguage(newLang)
            _uiState.update { it.copy(language = newLang) }
        }
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
                        loyaltyEnabled = settings.loyaltyEnabled,
                        loyaltyPointsPerDollar = settings.loyaltyPointsPerDollar.toString(),
                        loyaltyPointValueUsd = settings.loyaltyPointValueUsd
                            .stripTrailingZeros().toPlainString(),
                        loyaltyMinRedeemPoints = settings.loyaltyMinRedeemPoints.toString(),
                        exchangeRateLbpPerUsd = settings.exchangeRateLbpPerUsd
                            .stripTrailingZeros().toPlainString(),
                        showLbpOnReceipt = settings.showLbpOnReceipt,
                        stampDutyEnabled = settings.stampDutyEnabled,
                        stampDutyAmountUsd = settings.stampDutyAmountUsd
                            .stripTrailingZeros().toPlainString(),
                        notifyOnLoyaltyEarn = settings.notifyOnLoyaltyEarn,
                        notifyOnReservationConfirm = settings.notifyOnReservationConfirm
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
                SettingsField.LoyaltyPointsPerDollar -> state.copy(loyaltyPointsPerDollar = value)
                SettingsField.LoyaltyPointValueUsd -> state.copy(loyaltyPointValueUsd = value)
                SettingsField.LoyaltyMinRedeemPoints -> state.copy(loyaltyMinRedeemPoints = value)
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
                SettingsToggle.LoyaltyEnabled -> state.copy(loyaltyEnabled = value)
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
        val request = buildUpdateRequest(state) ?: return

        viewModelScope.launch {
            _uiState.update { it.copy(isSaving = true, error = null, saveSuccess = false) }
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

    fun saveNotifyOnLoyaltyEarn(enabled: Boolean) {
        _uiState.update { it.copy(notifyOnLoyaltyEarn = enabled) }
        persistNotificationToggle()
    }

    fun saveNotifyOnReservationConfirm(enabled: Boolean) {
        _uiState.update { it.copy(notifyOnReservationConfirm = enabled) }
        persistNotificationToggle()
    }

    private fun persistNotificationToggle() {
        val state = _uiState.value
        if (state.storeName.isBlank()) return
        val request = buildUpdateRequest(state) ?: return

        viewModelScope.launch {
            val result = settingsRepository.updateSettings(request)
            if (result.isFailure) {
                _uiState.update { it.copy(error = result.exceptionOrNull()?.message) }
            }
        }
    }

    private fun buildUpdateRequest(state: SettingsUiState): UpdateSettingsRequest? {
        val taxRatePercent = try {
            BigDecimal(state.taxRate.trim())
        } catch (_: Exception) {
            _uiState.update { it.copy(error = "Invalid tax rate") }
            return null
        }
        val exchangeRate = try {
            BigDecimal(state.exchangeRateLbpPerUsd.trim().ifBlank { "0" })
        } catch (_: Exception) {
            _uiState.update { it.copy(error = "Invalid exchange rate") }
            return null
        }
        val stampDutyAmount = try {
            BigDecimal(state.stampDutyAmountUsd.trim().ifBlank { "2.00" })
        } catch (_: Exception) {
            _uiState.update { it.copy(error = "Invalid stamp duty amount") }
            return null
        }
        val loyaltyPpd = state.loyaltyPointsPerDollar.trim().toIntOrNull() ?: 10
        val loyaltyPv  = try { BigDecimal(state.loyaltyPointValueUsd.trim().ifBlank { "0.01" }) }
                         catch (_: Exception) { BigDecimal("0.01") }
        val loyaltyMin = state.loyaltyMinRedeemPoints.trim().toIntOrNull() ?: 100

        return UpdateSettingsRequest(
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
            loyaltyEnabled = state.loyaltyEnabled,
            loyaltyPointsPerDollar = loyaltyPpd,
            loyaltyPointValueUsd = loyaltyPv,
            loyaltyMinRedeemPoints = loyaltyMin,
            exchangeRateLbpPerUsd = exchangeRate,
            showLbpOnReceipt = state.showLbpOnReceipt,
            stampDutyEnabled = state.stampDutyEnabled,
            stampDutyAmountUsd = stampDutyAmount,
            notifyOnLoyaltyEarn = state.notifyOnLoyaltyEarn,
            notifyOnReservationConfirm = state.notifyOnReservationConfirm
        )
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
    // Loyalty
    val loyaltyEnabled: Boolean = false,
    val loyaltyPointsPerDollar: String = "10",
    val loyaltyPointValueUsd: String = "0.01",
    val loyaltyMinRedeemPoints: String = "100",
    // Lebanese market
    val exchangeRateLbpPerUsd: String = "0",
    val showLbpOnReceipt: Boolean = false,
    val stampDutyEnabled: Boolean = false,
    val stampDutyAmountUsd: String = "2.00",
    val notifyOnLoyaltyEarn: Boolean = false,
    val notifyOnReservationConfirm: Boolean = false,
    val failedSyncCount: Int = 0,
    val language: String = SettingsRepository.LANG_EN,
    val kioskPin: String = SettingsRepository.DEFAULT_KIOSK_PIN,
    val printerConnectionType: PrinterConnectionType = PrinterConnectionType.BLUETOOTH,
    val printerBluetoothAddress: String? = null,
    val printerBluetoothDeviceName: String? = null,
    val printerNetworkHost: String = "",
    val printerNetworkPort: String = "9100",
    val printerPaperWidthMm: Int = 80,
    val showBluetoothDeviceSheet: Boolean = false,
    val bondedBluetoothDevices: List<Pair<String, String>> = emptyList(),
    val printerPrintState: PrinterPrintState = PrinterPrintState.Idle
)

sealed class PrinterPrintState {
    data object Idle : PrinterPrintState()
    data object Printing : PrinterPrintState()
    data class Success(val message: String) : PrinterPrintState()
    data class Error(val message: String) : PrinterPrintState()
}

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
    StampDutyAmountUsd,
    LoyaltyPointsPerDollar,
    LoyaltyPointValueUsd,
    LoyaltyMinRedeemPoints
}

enum class SettingsToggle {
    TaxEnabled,
    RequireCustomerInfo,
    EnableInventoryTracking,
    EnableRecipeManagement,
    ShowLbpOnReceipt,
    StampDutyEnabled,
    LoyaltyEnabled
}
