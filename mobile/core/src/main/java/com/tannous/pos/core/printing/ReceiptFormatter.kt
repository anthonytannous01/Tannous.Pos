package com.tannous.pos.core.printing

import com.tannous.pos.core.data.model.OrderDto
import java.text.NumberFormat
import java.time.Instant
import java.time.format.DateTimeFormatter
import java.util.*

/**
 * Utility class for formatting OrderDto into ReceiptToPrint.
 */
object ReceiptFormatter {
    
    private val currencyFormatter = NumberFormat.getCurrencyInstance(Locale.US)
    private val dateTimeFormatter = DateTimeFormatter.ofPattern("MM/dd/yyyy HH:mm:ss")
    
    /**
     * Converts an OrderDto to ReceiptToPrint.
     * This creates a minimal receipt if items are not available (offline scenario).
     */
    fun formatReceipt(
        order: OrderDto,
        items: List<ReceiptItem>? = null,
        payments: List<ReceiptPayment>
    ): ReceiptToPrint {
        // Parse date/time from order
        val dateTime = try {
            Instant.parse(order.createdAt).let { instant ->
                dateTimeFormatter.format(instant.atZone(java.time.ZoneId.systemDefault()))
            }
        } catch (e: Exception) {
            // Fallback to current time if parsing fails
            dateTimeFormatter.format(java.time.LocalDateTime.now())
        }
        
        return ReceiptToPrint(
            orderNumber = order.orderNumber,
            receiptNumber = order.receiptNumber,
            dateTime = dateTime,
            items = items, // Can be null for offline scenarios
            subtotal = currencyFormatter.format(order.subTotal),
            tax = currencyFormatter.format(order.tax),
            total = currencyFormatter.format(order.total),
            payments = payments,
            footerText = null
        )
    }
    
    /**
     * Creates a receipt text representation for sharing (plain text format).
     * @param isArabic When true, item names use the Arabic field (falls back to English if blank).
     */
    fun formatReceiptText(receipt: ReceiptToPrint, isArabic: Boolean = false): String {
        val text = StringBuilder()

        text.append("=".repeat(40)).append("\n")
        text.append("TANNOUS POS\n")
        text.append("=".repeat(40)).append("\n\n")
        
        if (receipt.receiptNumber != null) {
            text.append(if (isArabic) "رقم الإيصال: ${receipt.receiptNumber}\n" else "Receipt #: ${receipt.receiptNumber}\n")
        }
        if (receipt.orderNumber != null) {
            text.append(if (isArabic) "رقم الطلب: ${receipt.orderNumber}\n" else "Order #: ${receipt.orderNumber}\n")
        }
        text.append(if (isArabic) "التاريخ: ${receipt.dateTime}\n" else "Date: ${receipt.dateTime}\n")
        text.append("-".repeat(40)).append("\n\n")

        // Items
        if (receipt.items != null && receipt.items.isNotEmpty()) {
            text.append(if (isArabic) "الأصناف:\n" else "ITEMS:\n")
            receipt.items.forEach { item ->
                val displayName = if (isArabic) item.nameAr?.takeIf { it.isNotBlank() } ?: item.name else item.name
                text.append("${item.quantity}x $displayName\n")
                text.append("  ${item.unitPrice} each = ${item.totalPrice}\n")
            }
            text.append("-".repeat(40)).append("\n\n")
        } else {
            text.append("⚠️ Minimal receipt - Full details available after sync\n")
            text.append("-".repeat(40)).append("\n\n")
        }
        
        // Totals
        text.append(if (isArabic) "المجموع الجزئي: ${receipt.subtotal}\n" else "Subtotal: ${receipt.subtotal}\n")
        text.append(if (isArabic) "الضريبة: ${receipt.tax}\n" else "Tax: ${receipt.tax}\n")
        text.append(if (isArabic) "الإجمالي: ${receipt.total}\n" else "TOTAL: ${receipt.total}\n")
        text.append("=".repeat(40)).append("\n\n")

        // Payments
        text.append(if (isArabic) "الدفع:\n" else "PAYMENT:\n")
        receipt.payments.forEach { payment ->
            text.append("${payment.method}: ${payment.amount}\n")
            if (payment.change != null) {
                text.append(if (isArabic) "  الباقي: ${payment.change}\n" else "  Change: ${payment.change}\n")
            }
        }
        text.append("=".repeat(40)).append("\n\n")

        // Footer
        text.append(if (isArabic) "شكراً لزيارتكم!\nنتطلع لرؤيتكم مجدداً!\n" else "Thank you for your business!\nPlease come again!\n")
        
        return text.toString()
    }
}


