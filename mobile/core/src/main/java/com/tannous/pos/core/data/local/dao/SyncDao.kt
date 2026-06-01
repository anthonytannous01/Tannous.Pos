package com.tannous.pos.core.data.local.dao

import androidx.room.*
import com.tannous.pos.core.data.local.entity.KeyValueEntity
import com.tannous.pos.core.data.local.entity.OutboxOperationEntity
import com.tannous.pos.core.data.local.entity.OutboxStatus
import kotlinx.coroutines.flow.Flow

data class StatusCount(
    val status: OutboxStatus,
    val count: Int
)

@Dao
interface KeyValueDao {
    
    @Query("SELECT value FROM key_value WHERE `key` = :key")
    suspend fun get(key: String): String?
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun set(keyValue: KeyValueEntity)
    
    @Query("DELETE FROM key_value WHERE `key` = :key")
    suspend fun delete(key: String)
    
    @Query("SELECT * FROM key_value WHERE `key` LIKE :prefix || '%'")
    fun getAllWithPrefix(prefix: String): Flow<List<KeyValueEntity>>
}

@Dao
interface OutboxDao {
    
    @Query("SELECT * FROM outbox_operations WHERE status = :status ORDER BY createdAt ASC LIMIT :limit")
    suspend fun getPendingOperations(status: OutboxStatus, limit: Int = 20): List<OutboxOperationEntity>
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(operation: OutboxOperationEntity)
    
    @Update
    suspend fun update(operation: OutboxOperationEntity)
    
    @Query("UPDATE outbox_operations SET status = :status, lastError = :error, attempt = attempt + 1 WHERE operationId = :operationId")
    suspend fun updateStatus(operationId: String, status: OutboxStatus, error: String? = null)
    
    @Query("DELETE FROM outbox_operations WHERE status IN ('SENT', 'FAILED', 'FAILED_CONFLICT') AND createdAt < :before")
    suspend fun cleanupOldOperations(before: java.time.Instant)
    
    @Query("SELECT COUNT(*) FROM outbox_operations WHERE status = 'PENDING'")
    suspend fun getPendingCount(): Int
    
    @Query("UPDATE outbox_operations SET lastError = :error WHERE operationId = :operationId")
    suspend fun updateError(operationId: String, error: String?)
    
    @Query("UPDATE outbox_operations SET attempt = attempt + 1 WHERE operationId = :operationId")
    suspend fun incrementAttempt(operationId: String)
    
    @Query("SELECT status, COUNT(*) as count FROM outbox_operations GROUP BY status")
    suspend fun getOperationCounts(): List<StatusCount>
}
