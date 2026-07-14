package com.tannous.pos.core.data.local.dao

import androidx.paging.PagingSource
import androidx.room.*
import com.tannous.pos.core.data.local.entity.CustomerEntity
import kotlinx.coroutines.flow.Flow

@Dao
interface CustomerDao {
    
    @Query("SELECT * FROM customers WHERE isDeleted = 0 ORDER BY firstName, lastName")
    fun getAllActive(): Flow<List<CustomerEntity>>
    
    @Query("SELECT * FROM customers WHERE isDeleted = 0 ORDER BY firstName, lastName")
    fun getPagedCustomers(): PagingSource<Int, CustomerEntity>
    
    @Query("SELECT * FROM customers WHERE isDeleted = 0 AND (firstName LIKE '%' || :query || '%' OR lastName LIKE '%' || :query || '%' OR phone LIKE '%' || :query || '%') ORDER BY firstName, lastName")
    fun searchCustomers(query: String): Flow<List<CustomerEntity>>
    
    @Query("SELECT * FROM customers WHERE id = :id")
    suspend fun getById(id: String): CustomerEntity?
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(customer: CustomerEntity)
    
    @Update
    suspend fun update(customer: CustomerEntity)
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertAll(customers: List<CustomerEntity>)
    
    @Query("DELETE FROM customers")
    suspend fun deleteAll()
    
    @Query("UPDATE customers SET isDeleted = 1 WHERE id = :id")
    suspend fun markDeleted(id: String)
    
    /** FULL-SNAPSHOT ONLY: wipes the table and inserts [customers]. Never call with an
     *  incremental delta — use [insertAll] (REPLACE-on-conflict upsert) for that. */
    @Transaction
    suspend fun replaceAll(customers: List<CustomerEntity>) {
        deleteAll()
        insertAll(customers)
    }
}
