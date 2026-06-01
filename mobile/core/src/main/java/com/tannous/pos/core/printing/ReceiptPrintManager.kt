package com.tannous.pos.core.printing

import com.tannous.pos.core.data.local.entity.OrderEntity
import com.tannous.pos.core.data.local.entity.OrderLineEntity
import com.tannous.pos.core.data.local.entity.MenuItemEntity
import com.tannous.pos.core.logging.TelemetryLogger
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.launch
import timber.log.Timber
import java.math.BigDecimal
import java.text.NumberFormat
import java.time.format.DateTimeFormatter
import java.util.*
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class ReceiptPrintManager @Inject constructor(
    private val printingManager: PrintingManager,
    private val telemetryLogger: TelemetryLogger,
    private val coroutineScope: CoroutineScope
) {
    
    private val currencyFormatter = NumberFormat.getCurrencyInstance(Locale.US)
    private val dateFormatter = DateTimeFormatter.ofPattern("MM/dd/yyyy")
    private val timeFormatter = DateTimeFormatter.ofPattern("HH:mm:ss")
    
    fun printReceipt(
        order: OrderEntity,
        orderLines: List<OrderLineEntity>,
        menuItems: Map<String, MenuItemEntity>,
        cashTendered: BigDecimal,
        change: BigDecimal
    ) {
        coroutineScope.launch {
            try {
                val receiptText = formatReceipt(order, orderLines, menuItems, cashTendered, change)
                
                if (printingManager.isConnected()) {
                    val result = printingManager.printReceipt(receiptText)
                    if (result.isSuccess) {
                        telemetryLogger.logReceiptPrinted(order.id, "success")
                        Timber.d("Receipt printed successfully for order: ${order.id}")
                    } else {
                        telemetryLogger.logReceiptPrinted(order.id, "failure")
                        Timber.e("Failed to print receipt: ${result.exceptionOrNull()?.message}")
                    }
                } else {
                    Timber.w("No printer connected, receipt preview only")
                    telemetryLogger.logReceiptPrinted(order.id, "no_printer")
                }
            } catch (e: Exception) {
                Timber.e(e, "Error printing receipt for order: ${order.id}")
                telemetryLogger.logReceiptPrinted(order.id, "error")
            }
        }
    }
    
    private fun formatReceipt(
        order: OrderEntity,
        orderLines: List<OrderLineEntity>,
        menuItems: Map<String, MenuItemEntity>,
        cashTendered: BigDecimal,
        change: BigDecimal
    ): String {
        val receipt = StringBuilder()
        
        // Header
        receipt.append("=".repeat(32)).append("\n")
        receipt.append("TANNOUS POS SYSTEM\n")
        receipt.append("=".repeat(32)).append("\n")
        
        // Order Info
        receipt.append("Order #: ${order.receiptNumber ?: order.id.take(8)}\n")
        receipt.append("Date: ${dateFormatter.format(order.createdAt)}\n")
        receipt.append("Time: ${timeFormatter.format(order.createdAt)}\n")
        receipt.append("Shift: ${order.shiftId?.take(8) ?: "N/A"}\n")
        receipt.append("-".repeat(32)).append("\n")
        
        // Items
        receipt.append("ITEMS:\n")
        orderLines.forEach { line ->
            val menuItem = menuItems[line.menuItemId]
            val itemName = menuItem?.name ?: "Unknown Item"
            val lineTotal = line.totalPrice
            
            receipt.append("${line.quantity}x ${itemName}\n")
            receipt.append("  ${currencyFormatter.format(line.unitPrice)} each\n")
            receipt.append("  ${currencyFormatter.format(lineTotal)}\n")
        }
        
        receipt.append("-".repeat(32)).append("\n")
        
        // Totals
        receipt.append("Subtotal: ${currencyFormatter.format(order.subTotal)}\n")
        receipt.append("Tax (11%): ${currencyFormatter.format(order.tax)}\n")
        receipt.append("TOTAL: ${currencyFormatter.format(order.total)}\n")
        receipt.append("=".repeat(32)).append("\n")
        
        // Payment
        receipt.append("PAYMENT:\n")
        receipt.append("Method: CASH\n")
        receipt.append("Tendered: ${currencyFormatter.format(cashTendered)}\n")
        receipt.append("Change: ${currencyFormatter.format(change)}\n")
        receipt.append("=".repeat(32)).append("\n")
        
        // Footer
        receipt.append("Thank you for your business!\n")
        receipt.append("Please come again!\n")
        receipt.append("=".repeat(32)).append("\n")
        
        return receipt.toString()
    }
    
    // Test print functionality
    fun testPrint() {
        coroutineScope.launch {
            try {
                if (printingManager.isConnected()) {
                    val result = printingManager.testPrint()
                    if (result.isSuccess) {
                        Timber.d("Test print successful")
                    } else {
                        Timber.e("Test print failed: ${result.exceptionOrNull()?.message}")
                    }
                } else {
                    Timber.w("No printer connected for test print")
                }
            } catch (e: Exception) {
                Timber.e(e, "Error during test print")
            }
        }
    }
    
    // Get printer status
    fun getPrinterStatus(): PrinterStatus {
        return when {
            printingManager.isBluetoothConnected() -> PrinterStatus.BluetoothConnected(
                printingManager.getConnectedPrinterInfo() ?: "Unknown Bluetooth Printer"
            )
            printingManager.isLanConnected() -> PrinterStatus.LanConnected(
                printingManager.getConnectedPrinterInfo() ?: "Unknown LAN Printer"
            )
            else -> PrinterStatus.Disconnected
        }
    }
    
    // Connect to Bluetooth printer
    suspend fun connectBluetoothPrinter(device: android.bluetooth.BluetoothDevice): Result<Unit> {
        return try {
            val result = printingManager.connectBluetoothPrinter(device)
            if (result.isSuccess) {
                telemetryLogger.logPrinterConnected("bluetooth", "success")
            } else {
                telemetryLogger.logPrinterConnected("bluetooth", "failure")
            }
            result
        } catch (e: Exception) {
            telemetryLogger.logPrinterConnected("bluetooth", "error")
            Result.failure(e)
        }
    }
    
    // Connect to LAN printer
    suspend fun connectLanPrinter(host: String, port: Int): Result<Unit> {
        return try {
            val result = printingManager.connectLanPrinter(host, port)
            if (result.isSuccess) {
                telemetryLogger.logPrinterConnected("lan", "success")
            } else {
                telemetryLogger.logPrinterConnected("lan", "failure")
            }
            result
        } catch (e: Exception) {
            telemetryLogger.logPrinterConnected("lan", "error")
            Result.failure(e)
        }
    }
    
    // Disconnect printer
    suspend fun disconnectPrinter() {
        try {
            printingManager.disconnectBluetoothPrinter()
            printingManager.disconnectLanPrinter()
            telemetryLogger.logPrinterDisconnected()
            Timber.d("Printer disconnected")
        } catch (e: Exception) {
            Timber.e(e, "Error disconnecting printer")
        }
    }
}

sealed class PrinterStatus {
    object Disconnected : PrinterStatus()
    data class BluetoothConnected(val deviceName: String) : PrinterStatus()
    data class LanConnected(val connectionInfo: String) : PrinterStatus()
}
