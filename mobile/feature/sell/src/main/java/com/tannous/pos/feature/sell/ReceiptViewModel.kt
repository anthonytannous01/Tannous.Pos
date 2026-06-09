package com.tannous.pos.feature.sell

import android.content.Context
import android.content.Intent
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.OrderDto
import com.tannous.pos.core.data.remote.OrderService
import com.tannous.pos.core.data.repository.CatalogRepository
import com.tannous.pos.core.data.repository.OrderRepository
import com.tannous.pos.core.data.repository.SettingsRepository
import com.tannous.pos.core.printing.Printer
import com.tannous.pos.core.printing.ReceiptFormatter
import com.tannous.pos.core.printing.ReceiptItem
import com.tannous.pos.core.printing.ReceiptPayment
import com.tannous.pos.core.printing.ReceiptToPrint
import com.tannous.pos.core.util.currencyFormatterFor
import dagger.hilt.android.lifecycle.HiltViewModel
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import timber.log.Timber
import java.math.BigDecimal
import javax.inject.Inject

@HiltViewModel
class ReceiptViewModel @Inject constructor(
    @ApplicationContext private val context: Context,
    private val printer: Printer,
    private val orderService: OrderService,
    private val orderRepository: OrderRepository,
    private val catalogRepository: CatalogRepository,
    private val settingsRepository: SettingsRepository
) : ViewModel() {
    
    private val _printState = kotlinx.coroutines.flow.MutableStateFlow<PrintState>(PrintState.Idle)
    val printState = kotlinx.coroutines.flow.MutableStateFlow(_printState.value)

    private val _orderLines = MutableStateFlow<List<ReceiptLine>>(emptyList())
    val orderLines: StateFlow<List<ReceiptLine>> = _orderLines.asStateFlow()

    private val _currencyCode = MutableStateFlow("USD")
    val currencyCode: StateFlow<String> = _currencyCode.asStateFlow()

    private val _voidState = MutableStateFlow<VoidState>(VoidState.Idle)
    val voidState: StateFlow<VoidState> = _voidState.asStateFlow()

    private val _isArabic = MutableStateFlow(false)
    val isArabic: StateFlow<Boolean> = _isArabic.asStateFlow()

    init {
        viewModelScope.launch {
            try {
                _currencyCode.value = settingsRepository.getCurrency()
                val lang = settingsRepository.getLanguage()
                _isArabic.value = settingsRepository.isArabic(lang)
            } catch (e: Exception) {
                Timber.w(e, "Could not load currency/language for receipt; using default")
            }
        }
    }

    /**
     * Best-effort load of the finalized order's line items for display/printing.
     * Fetches the full order from the server and resolves item names from the local catalog.
     * Never throws and never blocks the receipt: on any failure the receipt simply shows no lines.
     */
    fun loadOrderLines(orderId: String) {
        viewModelScope.launch {
            try {
                val order = orderService.getOrder(orderId)
                val lines = order.orderLines ?: emptyList()
                val resolved = lines.map { line ->
                    val menuItem = try {
                        catalogRepository.getMenuItemById(line.menuItemId)
                    } catch (e: Exception) {
                        null
                    }
                    ReceiptLine(
                        name    = menuItem?.name ?: line.menuItemId.take(8),
                        nameAr  = menuItem?.nameAr,
                        quantity   = line.quantity,
                        unitPrice  = line.unitPrice,
                        totalPrice = line.totalPrice
                    )
                }
                _orderLines.value = resolved
            } catch (e: Exception) {
                Timber.w(e, "Could not load order lines for receipt $orderId (best-effort)")
            }
        }
    }

    private fun currentReceiptItems(): List<ReceiptItem>? {
        val lines = _orderLines.value
        if (lines.isEmpty()) return null
        return lines.map { line ->
            ReceiptItem(
                name       = line.name,
                nameAr     = line.nameAr,
                quantity   = line.quantity,
                unitPrice  = formatCurrency(line.unitPrice),
                totalPrice = formatCurrency(line.totalPrice)
            )
        }
    }
    
    fun printReceipt(order: OrderDto) {
        viewModelScope.launch {
            try {
                _printState.value = PrintState.Printing
                
                // Create receipt payments from order
                // Note: We don't have payment details in OrderDto, so we create a generic payment entry
                // In a real scenario, you might want to store payment info separately or include it in OrderDto
                val payments = listOf(
                    ReceiptPayment(
                        method = "PAID", // Generic since we don't have payment method in OrderDto
                        amount = formatCurrency(order.total)
                    )
                )
                
                // Format receipt with resolved line items when available (null degrades gracefully)
                val receipt = ReceiptFormatter.formatReceipt(
                    order = order,
                    items = currentReceiptItems(),
                    payments = payments
                )
                
                // Print receipt
                val result = printer.printReceipt(receipt)
                
                when (result) {
                    is com.tannous.pos.core.printing.PrintResult.Success -> {
                        _printState.value = PrintState.Success
                        Timber.d("Receipt printed successfully")
                    }
                    is com.tannous.pos.core.printing.PrintResult.Failed -> {
                        _printState.value = PrintState.Error(result.message)
                        Timber.e("Failed to print receipt: ${result.message}")
                    }
                }
            } catch (e: Exception) {
                _printState.value = PrintState.Error(e.message ?: "Unknown error")
                Timber.e(e, "Error printing receipt")
            }
        }
    }
    
    fun shareReceipt(order: OrderDto) {
        viewModelScope.launch {
            try {
                // Create receipt payments (same as print)
                val payments = listOf(
                    ReceiptPayment(
                        method = "PAID",
                        amount = formatCurrency(order.total)
                    )
                )
                
                // Format receipt with resolved line items when available (null degrades gracefully)
                val receipt = ReceiptFormatter.formatReceipt(
                    order = order,
                    items = currentReceiptItems(),
                    payments = payments
                )
                
                // Generate text receipt (bilingual: use Arabic labels + names when language = ar)
                val receiptText = ReceiptFormatter.formatReceiptText(receipt, isArabic = _isArabic.value)
                
                // Create share intent
                val shareIntent = Intent().apply {
                    action = Intent.ACTION_SEND
                    putExtra(Intent.EXTRA_TEXT, receiptText)
                    putExtra(Intent.EXTRA_SUBJECT, "Receipt ${order.receiptNumber ?: order.orderNumber ?: ""}")
                    type = "text/plain"
                }
                
                // Launch share dialog
                val chooserIntent = Intent.createChooser(shareIntent, "Share Receipt")
                chooserIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
                context.startActivity(chooserIntent)
                
                Timber.d("Receipt share intent launched")
            } catch (e: Exception) {
                Timber.e(e, "Error sharing receipt")
            }
        }
    }
    
    fun clearPrintState() {
        _printState.value = PrintState.Idle
    }

    fun voidOrder(orderId: String, reason: String) {
        viewModelScope.launch {
            _voidState.value = VoidState.Voiding
            val result = orderRepository.voidOrder(orderId, reason)
            _voidState.value = result.fold(
                onSuccess = { VoidState.Success(it.status) },
                onFailure = { VoidState.Error(it.message ?: "Failed to void order") }
            )
        }
    }

    fun clearVoidState() {
        _voidState.value = VoidState.Idle
    }
    
    private fun formatCurrency(amount: java.math.BigDecimal): String {
        return currencyFormatterFor(_currencyCode.value).format(amount)
    }
}

sealed class PrintState {
    data object Idle : PrintState()
    data object Printing : PrintState()
    data object Success : PrintState()
    data class Error(val message: String) : PrintState()
}

sealed class VoidState {
    data object Idle : VoidState()
    data object Voiding : VoidState()
    data class Success(val status: String) : VoidState()
    data class Error(val message: String) : VoidState()
}

/**
 * A resolved receipt line for display/printing: the catalog-resolved item name plus quantities
 * and prices from the server order line.
 */
data class ReceiptLine(
    val name: String,
    val nameAr: String? = null,
    val quantity: Int,
    val unitPrice: BigDecimal,
    val totalPrice: BigDecimal
)


