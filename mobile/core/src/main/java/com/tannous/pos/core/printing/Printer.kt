package com.tannous.pos.core.printing

/**
 * Interface for printing receipts.
 * Implementations can support different printer types:
 * - SystemPrintPrinter: Android Print Framework (PDF/PrintManager) - emulator-friendly
 * - EscPosBluetoothPrinter: ESC/POS thermal printers via Bluetooth
 * - EscPosNetworkPrinter: ESC/POS thermal printers via network
 */
interface Printer {
    /**
     * Prints a receipt.
     * @param receipt The receipt data to print
     * @return PrintResult indicating success or failure
     */
    suspend fun printReceipt(receipt: ReceiptToPrint): PrintResult
}

/**
 * Data class containing all information needed to print a receipt.
 */
data class ReceiptToPrint(
    val orderNumber: String?,
    val receiptNumber: String?,
    val dateTime: String, // Formatted date/time string
    val items: List<ReceiptItem>?, // Null if items not available (offline scenario)
    val subtotal: String, // Formatted currency string
    val tax: String, // Formatted currency string
    val total: String, // Formatted currency string
    val payments: List<ReceiptPayment>, // Payment methods and amounts
    val footerText: String? = null // Optional footer text
)

/**
 * Represents an item line on a receipt.
 */
data class ReceiptItem(
    val name: String,
    val nameAr: String? = null, // Arabic name for bilingual receipts
    val quantity: Int,
    val unitPrice: String, // Formatted currency string
    val totalPrice: String // Formatted currency string
)

/**
 * Represents a payment method on a receipt.
 */
data class ReceiptPayment(
    val method: String, // e.g., "CASH", "CARD", "OTHER"
    val amount: String, // Formatted currency string
    val change: String? = null // Change amount (for cash payments)
)

/**
 * Sealed class representing the result of a print operation.
 */
sealed class PrintResult {
    data object Success : PrintResult()
    data class Failed(val message: String) : PrintResult()
}


