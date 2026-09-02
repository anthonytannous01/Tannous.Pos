package com.tannous.pos.core.printing

import android.bluetooth.BluetoothAdapter
import android.bluetooth.BluetoothManager
import android.content.Context
import android.os.Build
import com.dantsu.escposprinter.EscPosPrinter
import com.dantsu.escposprinter.connection.DeviceConnection
import com.dantsu.escposprinter.connection.bluetooth.BluetoothConnection
import com.dantsu.escposprinter.connection.tcp.TcpConnection
import com.tannous.pos.core.data.model.PrinterConfig
import com.tannous.pos.core.data.model.PrinterConnectionType
import com.tannous.pos.core.data.model.ReceiptDto
import com.tannous.pos.core.data.repository.SettingsRepository
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import javax.inject.Inject
import javax.inject.Singleton

/** Outcome of a print attempt. */
sealed class PrintResult {
    data object Success : PrintResult()
    data class Failure(val message: String) : PrintResult()
}

/**
 * Sends receipts to an ESC/POS thermal printer over Bluetooth or TCP.
 *
 * Content is produced by [ReceiptRenderer]; this class owns only transport and printer
 * configuration. Receipts print in English only.
 */
@Singleton
class PrinterService @Inject constructor(
    @ApplicationContext private val context: Context,
    private val settingsRepository: SettingsRepository
) {

    /** Printer resolution in dots per inch. Standard for 58mm and 80mm thermal heads. */
    private val printerDpi = 203

    suspend fun printReceipt(receipt: ReceiptDto): PrintResult = withContext(Dispatchers.IO) {
        try {
            val config = settingsRepository.getPrinterConfig()
            val connection = openConnection(config)
                ?: return@withContext PrintResult.Failure(connectionError(config))

            val charsPerLine = ReceiptRenderer.charsPerLine(config.paperWidthMm)
            val printer = EscPosPrinter(
                connection,
                printerDpi,
                config.paperWidthMm.toFloat(),
                charsPerLine
            )
            val rows = ReceiptRenderer.rows(receipt)
            printer.printFormattedTextAndCut(ReceiptRenderer.toEscPos(rows, charsPerLine))
            PrintResult.Success
        } catch (e: Exception) {
            PrintResult.Failure(e.message ?: "Print failed")
        }
    }

    /**
     * Renders the same receipt as monospaced plain text, for sharing outside the app.
     * Uses the configured paper width so shared text matches what the printer produces.
     */
    suspend fun renderShareText(receipt: ReceiptDto): String {
        val charsPerLine = try {
            ReceiptRenderer.charsPerLine(settingsRepository.getPrinterConfig().paperWidthMm)
        } catch (_: Exception) {
            ReceiptRenderer.CHARS_80MM
        }
        return ReceiptRenderer.toPlainText(ReceiptRenderer.rows(receipt), charsPerLine)
    }

    private fun openConnection(config: PrinterConfig): DeviceConnection? =
        when (config.connectionType) {
            PrinterConnectionType.BLUETOOTH -> {
                val mac = config.bluetoothAddress
                val adapter = bluetoothAdapter()
                if (mac == null || adapter == null) null
                else BluetoothConnection(adapter.getRemoteDevice(mac))
            }
            PrinterConnectionType.NETWORK -> {
                val host = config.networkHost?.takeIf { it.isNotBlank() }
                if (host == null) null else TcpConnection(host, config.networkPort, TCP_TIMEOUT_SECONDS)
            }
            PrinterConnectionType.USB -> null
        }

    private fun bluetoothAdapter(): BluetoothAdapter? =
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            context.getSystemService(BluetoothManager::class.java)?.adapter
        } else {
            @Suppress("DEPRECATION")
            BluetoothAdapter.getDefaultAdapter()
        }

    private fun connectionError(config: PrinterConfig): String = when (config.connectionType) {
        PrinterConnectionType.BLUETOOTH -> "No Bluetooth printer configured"
        PrinterConnectionType.NETWORK -> "No network printer host configured"
        PrinterConnectionType.USB -> "USB printing is not yet supported"
    }

    private companion object {
        const val TCP_TIMEOUT_SECONDS = 15
    }
}
