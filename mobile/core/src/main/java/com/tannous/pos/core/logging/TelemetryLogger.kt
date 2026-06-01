package com.tannous.pos.core.logging

import android.os.Bundle
import com.google.firebase.analytics.FirebaseAnalytics
import com.google.firebase.crashlytics.FirebaseCrashlytics
import timber.log.Timber
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class TelemetryLogger @Inject constructor(
    private val firebaseAnalytics: FirebaseAnalytics,
    private val crashlytics: FirebaseCrashlytics
) {
    
    // Business Metrics
    fun logOrderCreated(orderId: String, itemCount: Int, totalAmount: Double) {
        val bundle = Bundle().apply {
            putString("order_id", orderId)
            putInt("item_count", itemCount)
            putDouble("total_amount", totalAmount)
        }
        
        firebaseAnalytics.logEvent("order_created", bundle)
        // Note: Firebase Crashlytics doesn't support reading custom keys, only setting them
        // We'll use a simple increment approach
        crashlytics.setCustomKey("orders_created", 1L)
        
        Timber.d("Telemetry: Order created - ID: $orderId, Items: $itemCount, Total: $totalAmount")
    }
    
    fun logOrderFinalized(orderId: String, paymentMethod: String, cashTendered: Double, change: Double) {
        val bundle = Bundle().apply {
            putString("order_id", orderId)
            putString("payment_method", paymentMethod)
            putDouble("cash_tendered", cashTendered)
            putDouble("change", change)
        }
        
        firebaseAnalytics.logEvent("order_finalized", bundle)
        // Note: Firebase Crashlytics doesn't support reading custom keys, only setting them
        crashlytics.setCustomKey("orders_finalized", 1L)
        
        Timber.d("Telemetry: Order finalized - ID: $orderId, Method: $paymentMethod")
    }
    
    fun logShiftOpened(shiftId: String, openingBalance: Double) {
        val bundle = Bundle().apply {
            putString("shift_id", shiftId)
            putDouble("opening_balance", openingBalance)
        }
        
        firebaseAnalytics.logEvent("shift_opened", bundle)
        Timber.d("Telemetry: Shift opened - ID: $shiftId, Balance: $openingBalance")
    }
    
    fun logShiftClosed(shiftId: String, expectedCash: Double, actualCash: Double, variance: Double) {
        val bundle = Bundle().apply {
            putString("shift_id", shiftId)
            putDouble("expected_cash", expectedCash)
            putDouble("actual_cash", actualCash)
            putDouble("variance", variance)
        }
        
        firebaseAnalytics.logEvent("shift_closed", bundle)
        Timber.d("Telemetry: Shift closed - ID: $shiftId, Variance: $variance")
    }
    
    // Sync Metrics
    fun logSyncPushStarted(operationCount: Int) {
        val bundle = Bundle().apply {
            putInt("operation_count", operationCount)
        }
        
        firebaseAnalytics.logEvent("sync_push_started", bundle)
        Timber.d("Telemetry: Sync push started - Operations: $operationCount")
    }
    
    fun logSyncPushCompleted(successCount: Int, failureCount: Int, conflictCount: Int) {
        val bundle = Bundle().apply {
            putInt("success_count", successCount)
            putInt("failure_count", failureCount)
            putInt("conflict_count", conflictCount)
        }
        
        firebaseAnalytics.logEvent("sync_push_completed", bundle)
        // Note: Firebase Crashlytics doesn't support reading custom keys, only setting them
        crashlytics.setCustomKey("sync_push_success", successCount.toLong())
        crashlytics.setCustomKey("sync_push_failures", failureCount.toLong())
        
        Timber.d("Telemetry: Sync push completed - Success: $successCount, Failures: $failureCount, Conflicts: $conflictCount")
    }
    
    fun logSyncPullStarted() {
        firebaseAnalytics.logEvent("sync_pull_started", null)
        Timber.d("Telemetry: Sync pull started")
    }
    
    fun logSyncPullCompleted(entitiesUpdated: Int, entitiesDeleted: Int) {
        val bundle = Bundle().apply {
            putInt("entities_updated", entitiesUpdated)
            putInt("entities_deleted", entitiesDeleted)
        }
        
        firebaseAnalytics.logEvent("sync_pull_completed", bundle)
        Timber.d("Telemetry: Sync pull completed - Updated: $entitiesUpdated, Deleted: $entitiesDeleted")
    }
    
    // Error Metrics
    fun logNetworkError(endpoint: String, errorCode: Int, errorMessage: String) {
        val bundle = Bundle().apply {
            putString("endpoint", endpoint)
            putInt("error_code", errorCode)
            putString("error_message", errorMessage)
        }
        
        firebaseAnalytics.logEvent("network_error", bundle)
        // Note: Firebase Crashlytics doesn't support reading custom keys, only setting them
        crashlytics.setCustomKey("network_errors", 1L)
        
        Timber.e("Telemetry: Network error - Endpoint: $endpoint, Code: $errorCode, Message: $errorMessage")
    }
    
    fun logConflictResolved(entityType: String, resolution: String) {
        val bundle = Bundle().apply {
            putString("entity_type", entityType)
            putString("resolution", resolution)
        }
        
        firebaseAnalytics.logEvent("conflict_resolved", bundle)
        Timber.d("Telemetry: Conflict resolved - Type: $entityType, Resolution: $resolution")
    }
    
    // Performance Metrics
    fun logOperationDuration(operation: String, durationMs: Long) {
        val bundle = Bundle().apply {
            putString("operation", operation)
            putLong("duration_ms", durationMs)
        }
        
        firebaseAnalytics.logEvent("operation_duration", bundle)
        
        if (durationMs > 5000) { // Log slow operations
            crashlytics.setCustomKey("slow_operations", "$operation:${durationMs}ms")
        }
        
        Timber.d("Telemetry: Operation duration - $operation: ${durationMs}ms")
    }
    
    // Printing Metrics
    fun logReceiptPrinted(orderId: String, status: String) {
        val bundle = Bundle().apply {
            putString("order_id", orderId)
            putString("status", status)
        }
        
        firebaseAnalytics.logEvent("receipt_printed", bundle)
        // Note: Firebase Crashlytics doesn't support reading custom keys, only setting them
        crashlytics.setCustomKey("receipts_printed", 1L)
        
        Timber.d("Telemetry: Receipt printed - Order: $orderId, Status: $status")
    }
    
    fun logPrinterConnected(connectionType: String, status: String) {
        val bundle = Bundle().apply {
            putString("connection_type", connectionType)
            putString("status", status)
        }
        
        firebaseAnalytics.logEvent("printer_connected", bundle)
        Timber.d("Telemetry: Printer connected - Type: $connectionType, Status: $status")
    }
    
    fun logPrinterDisconnected() {
        firebaseAnalytics.logEvent("printer_disconnected", null)
        Timber.d("Telemetry: Printer disconnected")
    }
}
