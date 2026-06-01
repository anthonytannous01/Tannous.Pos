package com.tannous.pos.core.printing

import android.bluetooth.BluetoothAdapter
import android.bluetooth.BluetoothDevice
import android.bluetooth.BluetoothSocket
import android.content.Context
import android.net.wifi.WifiManager
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import timber.log.Timber
import java.io.IOException
import java.io.OutputStream
import java.net.Socket
import java.util.*
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class PrintingManager @Inject constructor(
    private val context: Context,
    private val bluetoothAdapter: BluetoothAdapter?
) {
    
    private var bluetoothSocket: BluetoothSocket? = null
    private var lanSocket: Socket? = null
    private var outputStream: OutputStream? = null
    
    // ESC/POS Commands
    companion object {
        private const val ESC = 0x1B
        private const val GS = 0x1D
        private const val INIT = 0x40
        private const val ALIGN_CENTER = 0x61
        private const val ALIGN_LEFT = 0x61
        private const val FONT_BOLD = 0x45
        private const val FONT_NORMAL = 0x46
        private const val DOUBLE_HEIGHT = 0x21
        private const val DOUBLE_WIDTH = 0x21
        private const val CUT_PAPER = 0x6D
        private const val FEED_LINE = 0x0A
        private const val FEED_PAPER = 0x0C
    }
    
    // Bluetooth Printing
    suspend fun connectBluetoothPrinter(device: BluetoothDevice): Result<Unit> {
        return withContext(Dispatchers.IO) {
            try {
                val uuid = UUID.fromString("00001101-0000-1000-8000-00805F9B34FB") // Standard SPP UUID
                bluetoothSocket = device.createRfcommSocketToServiceRecord(uuid)
                bluetoothSocket?.connect()
                outputStream = bluetoothSocket?.outputStream
                
                // Initialize printer
                outputStream?.write(byteArrayOf(ESC.toByte(), INIT.toByte()))
                outputStream?.flush()
                
                Timber.d("Connected to Bluetooth printer: ${device.name}")
                Result.success(Unit)
            } catch (e: IOException) {
                Timber.e(e, "Failed to connect to Bluetooth printer")
                Result.failure(e)
            }
        }
    }
    
    suspend fun disconnectBluetoothPrinter() {
        withContext(Dispatchers.IO) {
            try {
                outputStream?.close()
                bluetoothSocket?.close()
                outputStream = null
                bluetoothSocket = null
                Timber.d("Disconnected from Bluetooth printer")
            } catch (e: IOException) {
                Timber.e(e, "Error disconnecting from Bluetooth printer")
            }
        }
    }
    
    // LAN Printing
    suspend fun connectLanPrinter(host: String, port: Int): Result<Unit> {
        return withContext(Dispatchers.IO) {
            try {
                lanSocket = Socket(host, port)
                outputStream = lanSocket?.getOutputStream()
                
                // Initialize printer
                outputStream?.write(byteArrayOf(ESC.toByte(), INIT.toByte()))
                outputStream?.flush()
                
                Timber.d("Connected to LAN printer: $host:$port")
                Result.success(Unit)
            } catch (e: IOException) {
                Timber.e(e, "Failed to connect to LAN printer")
                Result.failure(e)
            }
        }
    }
    
    suspend fun disconnectLanPrinter() {
        withContext(Dispatchers.IO) {
            try {
                outputStream?.close()
                lanSocket?.close()
                outputStream = null
                lanSocket = null
                Timber.d("Disconnected from LAN printer")
            } catch (e: IOException) {
                Timber.e(e, "Error disconnecting from LAN printer")
            }
        }
    }
    
    // Receipt Printing
    suspend fun printReceipt(receiptText: String): Result<Unit> {
        return withContext(Dispatchers.IO) {
            try {
                if (outputStream == null) {
                    return@withContext Result.failure(IllegalStateException("No printer connected"))
                }
                
                val commands = buildReceiptCommands(receiptText)
                outputStream?.write(commands)
                outputStream?.flush()
                
                // Feed and cut paper
                outputStream?.write(byteArrayOf(FEED_LINE.toByte(), FEED_LINE.toByte(), FEED_LINE.toByte()))
                outputStream?.write(byteArrayOf(GS.toByte(), CUT_PAPER.toByte(), 0x00.toByte()))
                outputStream?.flush()
                
                Timber.d("Receipt printed successfully")
                Result.success(Unit)
            } catch (e: IOException) {
                Timber.e(e, "Failed to print receipt")
                Result.failure(e)
            }
        }
    }
    
    private fun buildReceiptCommands(receiptText: String): ByteArray {
        val commands = mutableListOf<Byte>()
        
        // Initialize printer
        commands.addAll(byteArrayOf(ESC.toByte(), INIT.toByte()).toList())
        
        // Center align and bold for header
        commands.addAll(byteArrayOf(ESC.toByte(), ALIGN_CENTER.toByte(), 0x01.toByte()).toList())
        commands.addAll(byteArrayOf(ESC.toByte(), FONT_BOLD.toByte(), 0x01.toByte()).toList())
        commands.addAll(byteArrayOf(ESC.toByte(), DOUBLE_HEIGHT.toByte(), 0x11.toByte()).toList())
        
        // Add header
        commands.addAll("TANNOUS POS\n".toByteArray().toList())
        commands.addAll(byteArrayOf(ESC.toByte(), FONT_NORMAL.toByte(), 0x00.toByte()).toList())
        commands.addAll(byteArrayOf(ESC.toByte(), DOUBLE_HEIGHT.toByte(), 0x00.toByte()).toList())
        
        // Left align for content
        commands.addAll(byteArrayOf(ESC.toByte(), ALIGN_LEFT.toByte(), 0x00.toByte()).toList())
        
        // Add receipt content
        commands.addAll(receiptText.toByteArray().toList())
        
        // Add footer
        commands.addAll("\n\nThank you for your business!\n".toByteArray().toList())
        
        return commands.toByteArray()
    }
    
    // Utility methods
    fun isBluetoothConnected(): Boolean = bluetoothSocket?.isConnected == true
    
    fun isLanConnected(): Boolean = lanSocket?.isConnected == true
    
    fun isConnected(): Boolean = isBluetoothConnected() || isLanConnected()
    
    fun getConnectedPrinterInfo(): String? {
        return when {
            isBluetoothConnected() -> "Bluetooth: ${bluetoothSocket?.remoteDevice?.name ?: "Unknown"}"
            isLanConnected() -> "LAN: ${lanSocket?.inetAddress?.hostAddress ?: "Unknown"}"
            else -> null
        }
    }
    
    // Test print
    suspend fun testPrint(): Result<Unit> {
        val testText = """
            ================================
            TANNOUS POS - TEST PRINT
            ================================
            Date: ${java.time.LocalDateTime.now()}
            Time: ${java.time.LocalTime.now()}
            ================================
            This is a test print to verify
            that your printer is working
            correctly with the POS system.
            ================================
        """.trimIndent()
        
        return printReceipt(testText)
    }
}
