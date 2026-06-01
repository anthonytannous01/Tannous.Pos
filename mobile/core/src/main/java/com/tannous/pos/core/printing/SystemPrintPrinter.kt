package com.tannous.pos.core.printing

import android.content.Context
import android.print.PrintAttributes
import android.print.PrintDocumentAdapter
import android.print.PrintJob
import android.print.PrintManager
import android.webkit.WebView
import android.webkit.WebViewClient
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlinx.coroutines.withContext
import timber.log.Timber
import javax.inject.Inject
import javax.inject.Singleton
import kotlin.coroutines.resume

/**
 * Printer implementation using Android Print Framework (PrintManager).
 * This is emulator-friendly as it uses the system print preview dialog.
 * 
 * Creates a PDF document and opens the system print dialog, allowing users to:
 * - Save as PDF
 * - Print to physical printers
 * - Share the receipt
 */
@Singleton
class SystemPrintPrinter @Inject constructor(
    private val context: Context
) : Printer {
    
    override suspend fun printReceipt(receipt: ReceiptToPrint): PrintResult {
        return withContext(Dispatchers.Main) {
            try {
                val printManager = context.getSystemService(Context.PRINT_SERVICE) as? PrintManager
                if (printManager == null) {
                    Timber.e("PrintManager not available")
                    return@withContext PrintResult.Failed("Print service not available")
                }
                
                // Create HTML content for the receipt
                val htmlContent = generateReceiptHtml(receipt)
                
                // Create a WebView to render HTML for printing
                val webView = WebView(context.applicationContext)
                webView.settings.javaScriptEnabled = true
                
                // Use suspendCancellableCoroutine to wait for page load
                suspendCancellableCoroutine<PrintResult> { continuation ->
                    webView.webViewClient = object : WebViewClient() {
                        override fun onPageFinished(view: WebView, url: String) {
                            super.onPageFinished(view, url)
                            
                            try {
                                // Create print document adapter
                                val printAdapter = view.createPrintDocumentAdapter("Receipt_${receipt.receiptNumber ?: receipt.orderNumber ?: "Unknown"}")
                                
                                // Build print attributes
                                val printAttributes = PrintAttributes.Builder()
                                    .setMediaSize(PrintAttributes.MediaSize.ISO_A4)
                                    .setResolution(PrintAttributes.Resolution("receipt", "receipt-printer", 300, 300))
                                    .setMinMargins(PrintAttributes.Margins.NO_MARGINS)
                                    .build()
                                
                                // Create print job
                                val printJob = printManager.print(
                                    "Receipt ${receipt.receiptNumber ?: receipt.orderNumber ?: ""}",
                                    printAdapter,
                                    printAttributes
                                )
                                
                                Timber.d("Print job created: ${printJob.id}")
                                continuation.resume(PrintResult.Success)
                            } catch (e: Exception) {
                                Timber.e(e, "Error creating print job")
                                continuation.resume(PrintResult.Failed(e.message ?: "Failed to create print job"))
                            }
                        }
                        
                        override fun onReceivedError(
                            view: WebView?,
                            errorCode: Int,
                            description: String?,
                            failingUrl: String?
                        ) {
                            super.onReceivedError(view, errorCode, description, failingUrl)
                            continuation.resume(PrintResult.Failed("Failed to load receipt: $description"))
                        }
                    }
                    
                    // Load HTML content
                    webView.loadDataWithBaseURL(null, htmlContent, "text/html", "UTF-8", null)
                }
                
            } catch (e: Exception) {
                Timber.e(e, "Error printing receipt")
                PrintResult.Failed(e.message ?: "Unknown error occurred")
            }
        }
    }
    
    /**
     * Generates HTML content for the receipt.
     * Uses simple, printable styling that works well with PrintManager.
     */
    private fun generateReceiptHtml(receipt: ReceiptToPrint): String {
        val html = StringBuilder()
        
        html.append("<!DOCTYPE html>")
        html.append("<html><head>")
        html.append("<meta charset='UTF-8'>")
        html.append("<style>")
        html.append("""
            @media print {
                @page {
                    size: 80mm auto;
                    margin: 0;
                }
            }
            body {
                font-family: monospace;
                font-size: 12px;
                margin: 0;
                padding: 10px;
                max-width: 300px;
            }
            .header {
                text-align: center;
                font-weight: bold;
                font-size: 16px;
                margin-bottom: 10px;
                border-bottom: 1px solid #000;
                padding-bottom: 5px;
            }
            .info-row {
                display: flex;
                justify-content: space-between;
                margin: 3px 0;
            }
            .divider {
                border-top: 1px dashed #000;
                margin: 8px 0;
            }
            .items {
                margin: 10px 0;
            }
            .item-row {
                margin: 5px 0;
            }
            .item-name {
                font-weight: bold;
            }
            .item-details {
                font-size: 10px;
                color: #666;
                margin-left: 15px;
            }
            .totals {
                margin: 10px 0;
            }
            .total-row {
                display: flex;
                justify-content: space-between;
                margin: 3px 0;
            }
            .total-final {
                font-weight: bold;
                font-size: 14px;
                border-top: 1px solid #000;
                padding-top: 5px;
                margin-top: 5px;
            }
            .payment {
                margin: 10px 0;
            }
            .footer {
                text-align: center;
                margin-top: 15px;
                padding-top: 10px;
                border-top: 1px solid #000;
                font-size: 10px;
            }
            .offline-notice {
                background-color: #fff3cd;
                border: 1px solid #ffc107;
                padding: 5px;
                margin: 10px 0;
                font-size: 10px;
                text-align: center;
            }
        """.trimIndent())
        html.append("</style></head><body>")
        
        // Header
        html.append("<div class='header'>TANNOUS POS</div>")
        
        // Order/Receipt info
        if (receipt.receiptNumber != null) {
            html.append("<div class='info-row'><span>Receipt #:</span><span>${receipt.receiptNumber}</span></div>")
        }
        if (receipt.orderNumber != null) {
            html.append("<div class='info-row'><span>Order #:</span><span>${receipt.orderNumber}</span></div>")
        }
        html.append("<div class='info-row'><span>Date:</span><span>${receipt.dateTime}</span></div>")
        
        html.append("<div class='divider'></div>")
        
        // Items (if available)
        if (receipt.items != null && receipt.items.isNotEmpty()) {
            html.append("<div class='items'>")
            receipt.items.forEach { item ->
                html.append("<div class='item-row'>")
                html.append("<div class='item-name'>${item.quantity}x ${escapeHtml(item.name)}</div>")
                html.append("<div class='item-details'>")
                html.append("${item.unitPrice} each = ${item.totalPrice}")
                html.append("</div>")
                html.append("</div>")
            }
            html.append("</div>")
            html.append("<div class='divider'></div>")
        } else {
            // Offline scenario - show notice
            html.append("<div class='offline-notice'>")
            html.append("⚠️ Minimal receipt - Full details available after sync")
            html.append("</div>")
        }
        
        // Totals
        html.append("<div class='totals'>")
        html.append("<div class='total-row'><span>Subtotal:</span><span>${receipt.subtotal}</span></div>")
        html.append("<div class='total-row'><span>Tax:</span><span>${receipt.tax}</span></div>")
        html.append("<div class='total-row total-final'><span>TOTAL:</span><span>${receipt.total}</span></div>")
        html.append("</div>")
        
        html.append("<div class='divider'></div>")
        
        // Payments
        html.append("<div class='payment'>")
        html.append("<div style='font-weight: bold; margin-bottom: 5px;'>PAYMENT:</div>")
        receipt.payments.forEach { payment ->
            html.append("<div class='total-row'>")
            html.append("<span>${escapeHtml(payment.method)}:</span>")
            html.append("<span>${payment.amount}</span>")
            html.append("</div>")
            if (payment.change != null) {
                html.append("<div class='total-row' style='font-size: 10px; margin-left: 15px;'>")
                html.append("<span>Change:</span>")
                html.append("<span>${payment.change}</span>")
                html.append("</div>")
            }
        }
        html.append("</div>")
        
        // Footer
        html.append("<div class='footer'>")
        if (receipt.footerText != null) {
            html.append("<div>${escapeHtml(receipt.footerText)}</div>")
        } else {
            html.append("<div>Thank you for your business!</div>")
            html.append("<div>Please come again!</div>")
        }
        html.append("</div>")
        
        html.append("</body></html>")
        
        return html.toString()
    }
    
    /**
     * Escapes HTML special characters to prevent XSS and formatting issues.
     */
    private fun escapeHtml(text: String): String {
        return text
            .replace("&", "&amp;")
            .replace("<", "&lt;")
            .replace(">", "&gt;")
            .replace("\"", "&quot;")
            .replace("'", "&#39;")
    }
}

