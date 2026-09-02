package com.tannous.pos.core.printing

import com.tannous.pos.core.data.model.ReceiptDto
import java.math.BigDecimal
import java.math.RoundingMode
import java.text.NumberFormat
import java.text.SimpleDateFormat
import java.time.Instant
import java.util.Date
import java.util.Locale
import java.util.TimeZone

/**
 * A single logical row of a receipt, independent of how it is finally emitted.
 *
 * Rows are produced once by [ReceiptRenderer.rows] and then serialized either as ESC/POS
 * markup for a thermal printer or as plain text for sharing, so both outputs are guaranteed
 * to describe the same receipt.
 */
sealed interface ReceiptRow {
    /** Centered text, optionally bold and/or double-height. */
    data class Centered(
        val text: String,
        val bold: Boolean = false,
        val big: Boolean = false
    ) : ReceiptRow

    /** Left-aligned label with a right-aligned value on the same line. */
    data class LeftRight(
        val left: String,
        val right: String,
        val bold: Boolean = false
    ) : ReceiptRow

    /** Plain left-aligned text. */
    data class Left(val text: String) : ReceiptRow

    /** A horizontal rule spanning the full paper width. */
    data class Rule(val char: Char) : ReceiptRow
}

/**
 * Builds receipt content from a server-rendered [ReceiptDto] and serializes it.
 *
 * Receipts are English-only by design: thermal printers cannot shape Arabic text, and
 * rendering it as bitmaps was removed deliberately. The app UI remains bilingual.
 */
object ReceiptRenderer {

    /** Characters per line for the two supported paper widths. */
    const val CHARS_58MM = 32
    const val CHARS_80MM = 48

    fun charsPerLine(paperWidthMm: Int): Int =
        if (paperWidthMm <= 58) CHARS_58MM else CHARS_80MM

    /**
     * Converts a receipt into layout-independent rows.
     *
     * The tax rate shown is derived from the receipt's own figures, never from current
     * settings: a receipt must stay internally consistent, and a reprint after the store
     * changes its tax rate must not show the new rate against the old amount.
     */
    fun rows(receipt: ReceiptDto): List<ReceiptRow> = buildList {
        add(ReceiptRow.Centered(receipt.businessName, bold = true, big = true))

        val contact = listOfNotNull(
            receipt.businessAddress?.takeIf { it.isNotBlank() },
            receipt.businessPhone?.takeIf { it.isNotBlank() }
        ).joinToString("  ")
        if (contact.isNotEmpty()) add(ReceiptRow.Centered(contact))
        receipt.taxId?.takeIf { it.isNotBlank() }?.let { add(ReceiptRow.Centered("TAX ID: $it")) }

        if (receipt.isReprint) add(ReceiptRow.Centered("*** REPRINT ***", bold = true))

        add(ReceiptRow.Rule('-'))
        add(ReceiptRow.LeftRight("Order: ${receipt.orderNumber}", formatPrintedAt(receipt.printedAt)))
        if (receipt.orderType.isNotBlank()) add(ReceiptRow.Left("Type: ${receipt.orderType}"))
        receipt.tableLabel?.takeIf { it.isNotBlank() }?.let { add(ReceiptRow.Left("Table: $it")) }
        receipt.customerName?.takeIf { it.isNotBlank() }?.let { add(ReceiptRow.Left("Customer: $it")) }

        add(ReceiptRow.Rule('='))
        add(ReceiptRow.LeftRight("ITEM", "TOTAL", bold = true))
        receipt.lines.forEach { line ->
            add(ReceiptRow.LeftRight("${line.qty}x ${line.name}", usd(line.lineTotal)))
            line.notes?.takeIf { it.isNotBlank() }?.let { add(ReceiptRow.Left("   $it")) }
        }

        add(ReceiptRow.Rule('-'))
        add(ReceiptRow.LeftRight("Subtotal", usd(receipt.subTotal)))
        if (receipt.discountAmount > BigDecimal.ZERO) {
            add(ReceiptRow.LeftRight("Discount", "-${usd(receipt.discountAmount)}"))
        }
        if (receipt.taxAmount > BigDecimal.ZERO) {
            val rate = taxPercentFor(receipt)
            add(ReceiptRow.LeftRight(if (rate != null) "VAT ($rate%)" else "VAT", usd(receipt.taxAmount)))
        }
        if (receipt.stampDutyEnabled && receipt.stampDuty > BigDecimal.ZERO) {
            add(ReceiptRow.LeftRight("Stamp Duty", usd(receipt.stampDuty)))
        }

        add(ReceiptRow.Rule('='))
        add(ReceiptRow.LeftRight("TOTAL USD", usd(receipt.totalUsd), bold = true))
        if (receipt.totalLbp > BigDecimal.ZERO) {
            add(ReceiptRow.LeftRight("TOTAL LBP", lbp(receipt.totalLbp)))
        }

        add(ReceiptRow.Rule('='))
        receipt.payments.forEach { payment ->
            add(ReceiptRow.LeftRight(payment.method, usd(payment.amount)))
        }
        add(ReceiptRow.LeftRight("Tendered", usd(receipt.amountTendered)))
        if (receipt.changeDue > BigDecimal.ZERO) {
            add(ReceiptRow.LeftRight("Change", usd(receipt.changeDue)))
        }

        add(ReceiptRow.Rule('-'))
        if (receipt.footerMessage.isNotBlank()) add(ReceiptRow.Centered(receipt.footerMessage))
        add(ReceiptRow.Rule('='))
    }

    /**
     * Serializes rows as DantSu ESC/POS markup. Column placement is delegated to the
     * printer's own [L]/[C]/[R] parser rather than hardcoded padding, so the same rows
     * lay out correctly on both 58mm and 80mm paper.
     */
    fun toEscPos(rows: List<ReceiptRow>, charsPerLine: Int): String {
        val sb = StringBuilder()
        rows.forEach { row ->
            when (row) {
                is ReceiptRow.Centered -> {
                    var body = escape(row.text)
                    // 'big' is double-width as well as double-height, so it consumes two
                    // columns per character. On 58mm paper a name longer than 16 characters
                    // overflows the line and the printer cannot centre it - fall back to
                    // normal width rather than letting it wrap or clip.
                    if (row.big && fitsAtDoubleWidth(row.text, charsPerLine)) {
                        body = "<font size='big'>$body</font>"
                    }
                    if (row.bold) body = "<b>$body</b>"
                    sb.append("[C]").append(body).append('\n')
                }
                is ReceiptRow.LeftRight -> {
                    val right = escape(row.right)
                    val left = escape(truncateLeft(row.left, row.right, charsPerLine))
                    if (row.bold) {
                        sb.append("[L]<b>").append(left).append("</b>[R]<b>").append(right).append("</b>\n")
                    } else {
                        sb.append("[L]").append(left).append("[R]").append(right).append('\n')
                    }
                }
                is ReceiptRow.Left -> sb.append("[L]").append(escape(row.text)).append('\n')
                is ReceiptRow.Rule -> sb.append("[C]").append(row.char.toString().repeat(charsPerLine)).append('\n')
            }
        }
        return sb.toString()
    }

    /** Serializes rows as monospaced plain text, for sharing the same receipt outside the app. */
    fun toPlainText(rows: List<ReceiptRow>, charsPerLine: Int): String {
        val sb = StringBuilder()
        rows.forEach { row ->
            when (row) {
                is ReceiptRow.Centered -> sb.append(center(row.text, charsPerLine)).append('\n')
                is ReceiptRow.LeftRight -> {
                    val left = truncateLeft(row.left, row.right, charsPerLine)
                    val gap = (charsPerLine - left.length - row.right.length).coerceAtLeast(1)
                    sb.append(left).append(" ".repeat(gap)).append(row.right).append('\n')
                }
                is ReceiptRow.Left -> sb.append(row.text.take(charsPerLine)).append('\n')
                is ReceiptRow.Rule -> sb.append(row.char.toString().repeat(charsPerLine)).append('\n')
            }
        }
        return sb.toString()
    }

    /** True when [text] still fits on one line at double width. */
    internal fun fitsAtDoubleWidth(text: String, charsPerLine: Int): Boolean =
        text.length * 2 <= charsPerLine

    /**
     * Effective tax rate as a whole-number percentage, derived from the receipt's own
     * subtotal and tax. Returns null when it cannot be computed or rounds to zero, in
     * which case the caller prints "VAT" without a rate rather than a false "VAT (0%)".
     */
    internal fun taxPercentFor(receipt: ReceiptDto): String? {
        val base = receipt.subTotal.subtract(receipt.discountAmount)
        if (base <= BigDecimal.ZERO || receipt.taxAmount <= BigDecimal.ZERO) return null
        val percent = receipt.taxAmount
            .multiply(BigDecimal.valueOf(100))
            .divide(base, 0, RoundingMode.HALF_UP)
        return if (percent <= BigDecimal.ZERO) null else percent.toPlainString()
    }

    /** Keeps a label from colliding with its right-hand value on narrow paper. */
    private fun truncateLeft(left: String, right: String, charsPerLine: Int): String {
        val available = charsPerLine - right.length - 1
        if (available <= 0) return ""
        return if (left.length <= available) left else left.take(available)
    }

    private fun center(text: String, charsPerLine: Int): String {
        val clipped = text.take(charsPerLine)
        val pad = (charsPerLine - clipped.length) / 2
        return " ".repeat(pad.coerceAtLeast(0)) + clipped
    }

    /**
     * Neutralizes characters the ESC/POS text parser treats as markup. Applied to plain-text
     * output too, so shared and printed receipts stay character-for-character comparable.
     */
    private fun escape(value: String): String = value
        .replace("[", "(")
        .replace("]", ")")
        .replace("<", "(")
        .replace(">", ")")

    internal fun formatPrintedAt(iso: String): String {
        if (iso.isBlank()) return ""
        return try {
            val instant = Instant.parse(iso)
            SimpleDateFormat("MM/dd/yyyy HH:mm", Locale.US)
                .apply { timeZone = TimeZone.getDefault() }
                .format(Date.from(instant))
        } catch (_: Exception) {
            iso.take(16)
        }
    }

    private fun usd(amount: BigDecimal): String = "$${amount.setScale(2, RoundingMode.HALF_UP)}"

    private fun lbp(amount: BigDecimal): String =
        NumberFormat.getNumberInstance(Locale.US).format(amount.setScale(0, RoundingMode.HALF_UP))
}
