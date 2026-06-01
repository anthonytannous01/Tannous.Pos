package com.tannous.pos.feature.printing

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.repository.SettingsRepository
import com.tannous.pos.core.printing.PrintResult
import com.tannous.pos.core.printing.Printer
import com.tannous.pos.core.printing.ReceiptItem
import com.tannous.pos.core.printing.ReceiptPayment
import com.tannous.pos.core.printing.ReceiptToPrint
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter
import javax.inject.Inject

@HiltViewModel
class PrintingPreviewViewModel @Inject constructor(
    private val printer: Printer,
    private val settingsRepository: SettingsRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow(PrintingPreviewUiState())
    val uiState: StateFlow<PrintingPreviewUiState> = _uiState.asStateFlow()

    fun printSampleReceipt(label: String = "Receipt Preview") {
        viewModelScope.launch {
            _uiState.update { it.copy(isPrinting = true, printResult = null) }
            try {
                val settings = try {
                    settingsRepository.getSettings()
                } catch (_: Exception) {
                    null
                }
                val footer = settings?.receiptFooter ?: "Thank you for your purchase!"

                val receipt = ReceiptToPrint(
                    orderNumber = "SAMPLE-001",
                    receiptNumber = "PREVIEW",
                    dateTime = DateTimeFormatter.ofPattern("MM/dd/yyyy HH:mm")
                        .format(LocalDateTime.now()),
                    items = listOf(
                        ReceiptItem("Coffee - Large", 1, "$4.50", "$4.50"),
                        ReceiptItem("+ Extra Shot", 1, "$0.75", "$0.75")
                    ),
                    subtotal = "$5.25",
                    tax = "$0.45",
                    total = "$5.70",
                    payments = listOf(ReceiptPayment("Cash", "$6.00", "$0.30")),
                    footerText = footer
                )

                when (val result = printer.printReceipt(receipt)) {
                    is PrintResult.Success ->
                        _uiState.update {
                            it.copy(isPrinting = false, printResult = "$label sent to printer")
                        }
                    is PrintResult.Failed ->
                        _uiState.update {
                            it.copy(
                                isPrinting = false,
                                printResult = "Print failed: ${result.message}"
                            )
                        }
                }
            } catch (e: Exception) {
                _uiState.update {
                    it.copy(isPrinting = false, printResult = "Print error: ${e.message}")
                }
            }
        }
    }

    fun clearPrintResult() {
        _uiState.update { it.copy(printResult = null) }
    }
}

data class PrintingPreviewUiState(
    val isPrinting: Boolean = false,
    val printResult: String? = null
)
