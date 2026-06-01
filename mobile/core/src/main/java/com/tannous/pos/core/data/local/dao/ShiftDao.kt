package com.tannous.pos.core.data.local.dao

import androidx.room.*
import com.tannous.pos.core.data.local.entity.ShiftEntity
import kotlinx.coroutines.flow.Flow

@Dao
interface ShiftDao {
    
    @Query("SELECT * FROM shifts WHERE isDeleted = 0 ORDER BY openedAt DESC")
    fun getAllActive(): Flow<List<ShiftEntity>>
    
    @Query("SELECT * FROM shifts WHERE id = :shiftId AND isDeleted = 0")
    suspend fun getById(shiftId: String): ShiftEntity?
    
    @Query("SELECT * FROM shifts WHERE isDeleted = 0 AND closedAt IS NULL ORDER BY openedAt DESC LIMIT 1")
    suspend fun getActiveShift(): ShiftEntity?
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(shift: ShiftEntity)
    
    @Update
    suspend fun update(shift: ShiftEntity)
    
    @Query("UPDATE shifts SET isDeleted = 1 WHERE id = :shiftId")
    suspend fun delete(shiftId: String)
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertAll(shifts: List<ShiftEntity>)
    
    @Query("DELETE FROM shifts")
    suspend fun deleteAll()
    
    @Transaction
    suspend fun upsertAll(shifts: List<ShiftEntity>) {
        insertAll(shifts)
    }
}
