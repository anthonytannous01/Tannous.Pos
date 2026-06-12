package com.tannous.pos.feature.settings.printer

import android.graphics.Bitmap
import android.graphics.Canvas
import android.graphics.Color
import android.text.Layout
import android.text.StaticLayout
import android.text.TextPaint
import android.bluetooth.BluetoothAdapter
import com.dantsu.escposprinter.EscPosPrinter
import com.dantsu.escposprinter.connection.DeviceConnection
import com.dantsu.escposprinter.connection.bluetooth.BluetoothConnection
import com.dantsu.escposprinter.connection.tcp.TcpConnection
import com.dantsu.escposprinter.textparser.PrinterTextParserImg
import com.tannous.pos.core.data.model.PrinterConfig
import com.tannous.pos.core.data.model.PrinterConnectionType
import com.tannous.pos.core.data.model.ReceiptDto
import com.tannous.pos.core.data.repository.SettingsRepository
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.math.BigDecimal
import java.math.RoundingMode
import java.text.NumberFormat
import java.text.SimpleDateFormat
import java.util.Locale
import java.util.TimeZone
import javax.inject.Inject
import javax.inject.Singleton

sealed class PrintResult {
    data object Success : PrintResult()
    data class Failure(val message: String) : PrintResult()
}

@Singleton
class PrinterService @Inject constructor(
    private val settingsRepository: SettingsRepository
) {

    suspend fun printReceipt(receipt: ReceiptDto, isArabic: Boolean): PrintResult =
        withContext(Dispatchers.IO) {
            try {
                val config = settingsRepository.getPrinterConfig()
                val connection = openConnection(config)
                    ?: return@withContext PrintResult.Failure(connectionError(config))

                val charsPerLine = if (config.paperWidthMm <= 58) 32 else 48
                val printer = EscPosPrinter(
                    connection,
                    203,
                    config.paperWidthMm.toFloat(),
                    charsPerLine
                )
                val taxPercent = settingsRepository.getTaxRate()
                    .multiply(BigDecimal.valueOf(100))
                    .setScale(0, RoundingMode.HALF_UP)
                    .toPlainString()
                val text = buildReceiptText(printer, receipt, isArabic, taxPercent)
                printer.printFormattedTextAndCut(text)
                PrintResult.Success
            } catch (e: Exception) {
                PrintResult.Failure(e.message ?: "Print failed")
            }
        }

    private fun openConnection(config: PrinterConfig): DeviceConnection? {
        return when (config.connectionType) {
            PrinterConnectionType.BLUETOOTH -> {
                val mac = config.bluetoothAddress ?: return null
                val adapter = BluetoothAdapter.getDefaultAdapter() ?: return null
                BluetoothConnection(adapter.getRemoteDevice(mac))
            }
            PrinterConnectionType.NETWORK -> {
                val host = config.networkHost ?: return null
                TcpConnection(host, config.networkPort, 15)
            }
            PrinterConnectionType.USB -> null
        }
    }

    private fun connectionError(config: PrinterConfig): String = when (config.connectionType) {
        PrinterConnectionType.BLUETOOTH -> "No Bluetooth printer configured"
        PrinterConnectionType.NETWORK   -> "No network printer host configured"
        PrinterConnectionType.USB       -> "USB printing is not yet supported"
    }

    private fun buildReceiptText(
        printer: EscPosPrinter,
        receipt: ReceiptDto,
        isArabic: Boolean,
        taxPercent: String
    ): String {
        val sb = StringBuilder()
        // TODO: logo

        sb.append("[C]<b><font size='big'>${safe(receipt.businessName)}</font></b>\n")
        val headerLine = listOfNotNull(
            receipt.businessAddress?.takeIf { it.isNotBlank() },
            receipt.businessPhone?.takeIf { it.isNotBlank() }
        ).joinToString("  ")
        if (headerLine.isNotEmpty()) {
            sb.append("[C]${safe(headerLine)}\n")
        }
        sb.append("[C]--------------------------------\n")

        val dateStr = formatPrintedAt(receipt.printedAt)
        sb.append("[L]Order: ${safe(receipt.orderNumber)}   [R]${safe(dateStr)}\n")
        sb.append("[L]Type: ${safe(receipt.orderType)}\n")
        receipt.tableLabel?.takeIf { it.isNotBlank() }?.let {
            sb.append("[L]Table: ${safe(it)}\n")
        }
        receipt.customerName?.takeIf { it.isNotBlank() }?.let {
            sb.append("[L]Customer: ${safe(it)}\n")
        }
        sb.append("[C]================================\n")
        sb.append("[L]<b>ITEM</b>            [R]<b>TOTAL</b>\n")

        receipt.lines.forEach { line ->
            sb.append("[L]${line.qty}x ${safe(line.name)}          [R]${usd(line.lineTotal)}\n")
            val nameAr = line.nameAr
            if (isArabic && !nameAr.isNullOrBlank()) {
                sb.append(arabicImageLine(printer, nameAr))
            }
        }

        sb.append("[C]--------------------------------\n")
        sb.append("[L]Subtotal               [R]${usd(receipt.subTotal)}\n")
        if (receipt.discountAmount > BigDecimal.ZERO) {
            sb.append("[L]Discount               [R]-${usd(receipt.discountAmount)}\n")
        }
        if (receipt.taxAmount > BigDecimal.ZERO) {
            sb.append("[L]VAT (${taxPercent}%)              [R]${usd(receipt.taxAmount)}\n")
        }
        if (receipt.stampDutyEnabled && receipt.stampDuty > BigDecimal.ZERO) {
            sb.append("[L]Stamp Duty             [R]${usd(receipt.stampDuty)}\n")
        }
        sb.append("[C]================================\n")
        sb.append("[L]<b>TOTAL USD</b>       [R]<b>${usd(receipt.totalUsd)}</b>\n")
        if (receipt.totalLbp > BigDecimal.ZERO) {
            sb.append("[L]TOTAL LBP              [R]${lbp(receipt.totalLbp)}\n")
        }
        sb.append("[C]================================\n")

        receipt.payments.forEach { payment ->
            sb.append("[L]${safe(payment.method)}               [R]${usd(payment.amount)}\n")
        }
        sb.append("[L]Tendered               [R]${usd(receipt.amountTendered)}\n")
        if (receipt.changeDue > BigDecimal.ZERO) {
            sb.append("[L]Change                 [R]${usd(receipt.changeDue)}\n")
        }
        sb.append("[C]--------------------------------\n")
        sb.append("[C]${safe(receipt.footerMessage)}\n")
        if (isArabic && receipt.footerMessageAr.isNotBlank()) {
            sb.append(arabicImageLine(printer, receipt.footerMessageAr))
        }
        sb.append("[C]================================\n")
        return sb.toString()
    }

    private fun arabicImageLine(printer: EscPosPrinter, text: String): String {
        val bitmap = arabicToBitmap(text)
        val hex = PrinterTextParserImg.bitmapToHexadecimalString(printer, bitmap, false)
        return "[C]<img>$hex</img>\n"
    }

    private fun arabicToBitmap(text: String, widthPx: Int = 380): Bitmap {
        val paint = TextPaint().apply {
            textSize = 28f
            color = Color.BLACK
            isAntiAlias = true
        }
        val layout = StaticLayout.Builder
            .obtain(text, 0, text.length, paint, widthPx)
            .setAlignment(Layout.Alignment.ALIGN_OPPOSITE)
            .setLineSpacing(0f, 1f)
            .setIncludePad(true)
            .build()
        val height = layout.height.coerceAtLeast(1)
        val bitmap = Bitmap.createBitmap(widthPx, height, Bitmap.Config.ARGB_8888)
        Canvas(bitmap).apply {
            drawColor(Color.WHITE)
            layout.draw(this)
        }
        return bitmap
    }

    private fun formatPrintedAt(iso: String): String {
        if (iso.isBlank()) return ""
        return try {
            val instant = java.time.Instant.parse(iso)
            val formatter = SimpleDateFormat("MM/dd/yyyy HH:mm", Locale.US).apply {
                timeZone = TimeZone.getDefault()
            }
            formatter.format(java.util.Date.from(instant))
        } catch (_: Exception) {
            iso.take(16)
        }
    }

    private fun usd(amount: BigDecimal): String =
        "$${amount.setScale(2, RoundingMode.HALF_UP)}"

    private fun lbp(amount: BigDecimal): String =
        NumberFormat.getNumberInstance(Locale.US).format(amount.setScale(0, RoundingMode.HALF_UP))

    private fun safe(value: String): String =
        value.replace("[", "(").replace("]", ")")
}

object TestReceiptFactory {

    fun sample(): ReceiptDto = ReceiptDto(
        orderId = "00000000-0000-0000-0000-000000000001",
        orderNumber = "TEST-001",
        orderType = "Dine-In",
        printedAt = java.time.Instant.now().toString(),
        businessName = "Tannous Test Kitchen",
        businessPhone = "+961 1 234 567",
        businessAddress = "Beirut, Lebanon",
        tableLabel = "T3",
        lines = listOf(
            com.tannous.pos.core.data.model.ReceiptLineDto(
                name = "Shawarma Plate",
                nameAr = "طبق شاورما",
                qty = 2,
                unitPrice = BigDecimal("12.50"),
                lineTotal = BigDecimal("25.00")
            )
        ),
        subTotal = BigDecimal("25.00"),
        taxAmount = BigDecimal("2.75"),
        totalUsd = BigDecimal("27.75"),
        totalLbp = BigDecimal("2480000"),
        stampDutyEnabled = false,
        payments = listOf(
            com.tannous.pos.core.data.model.ReceiptPaymentDto(
                method = "Cash",
                amount = BigDecimal("30.00")
            )
        ),
        amountTendered = BigDecimal("30.00"),
        changeDue = BigDecimal("2.25")
    )
}
