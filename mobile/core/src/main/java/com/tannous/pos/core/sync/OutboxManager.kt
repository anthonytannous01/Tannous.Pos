package com.tannous.pos.core.sync

import com.tannous.pos.core.data.local.dao.OutboxDao
import com.tannous.pos.core.data.local.entity.OutboxOperationEntity
import com.tannous.pos.core.data.local.entity.OutboxStatus
import com.tannous.pos.core.sync.SyncManager
import kotlinx.coroutines.flow.Flow
import timber.log.Timber
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import java.time.Instant
import java.util.*
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class OutboxManager @Inject constructor(
    private val outboxDao: OutboxDao,
    private val syncManager: SyncManager
) {
    
    suspend fun enqueueOperation(
        type: String,
        payload: Any,
        orderId: String? = null,
        shiftId: String? = null
    ) {
        val operation = OutboxOperationEntity(
            operationId = UUID.randomUUID().toString(),
            type = type,
            payloadJson = Json.encodeToString(payload),
            createdAt = Instant.now(),
            attempt = 0,
            lastError = null,
            status = OutboxStatus.PENDING
        )
        
        outboxDao.insert(operation)
        Timber.d("Enqueued outbox operation: $type for ${orderId ?: shiftId}")
    }
    
    suspend fun getPendingOperations(limit: Int = 20): List<OutboxOperationEntity> {
        return outboxDao.getPendingOperations(OutboxStatus.PENDING, limit)
    }
    
    suspend fun markOperationSent(operationId: String) {
        outboxDao.updateStatus(operationId, OutboxStatus.SENT)
        Timber.d("Marked operation $operationId as SENT")
    }
    
    suspend fun markOperationFailed(operationId: String, error: String) {
        outboxDao.updateStatus(operationId, OutboxStatus.FAILED, error)
        Timber.d("Marked operation $operationId as FAILED: $error")
    }
    
    suspend fun markOperationConflict(operationId: String, serverEntity: String) {
        outboxDao.updateStatus(operationId, OutboxStatus.FAILED_CONFLICT)
        outboxDao.updateError(operationId, "Conflict with server: $serverEntity")
        Timber.d("Marked operation $operationId as FAILED_CONFLICT")
    }
    
    suspend fun incrementAttempt(operationId: String) {
        outboxDao.incrementAttempt(operationId)
    }
    
    suspend fun cleanupOldOperations() {
        val cutoff = Instant.now().minusSeconds(7 * 24 * 60 * 60) // 7 days
        outboxDao.cleanupOldOperations(cutoff)
        Timber.d("Cleaned up old outbox operations")
    }
    
    fun triggerImmediatePush() {
        syncManager.triggerImmediatePush()
    }
    
    fun triggerImmediatePull() {
        syncManager.triggerImmediatePull()
    }
    
    suspend fun getOperationCounts(): Map<OutboxStatus, Int> {
        val statusCounts = outboxDao.getOperationCounts()
        return statusCounts.associate { it.status to it.count }
    }
}
