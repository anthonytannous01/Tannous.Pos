package com.tannous.pos.core.data.local.dao

import androidx.room.*
import com.tannous.pos.core.data.local.entity.OrderEntity
import com.tannous.pos.core.data.local.entity.OrderLineAddOnEntity
import com.tannous.pos.core.data.local.entity.OrderLineEntity
import kotlinx.coroutines.flow.Flow
import java.math.BigDecimal
import java.time.Instant

@Dao
interface OrderDao {
    
    @Query("SELECT * FROM orders ORDER BY createdAt DESC")
    fun getAll(): Flow<List<OrderEntity>>
    
    @Query("SELECT * FROM orders WHERE id = :id")
    suspend fun getById(id: String): OrderEntity?
    
    @Query("SELECT * FROM orders WHERE shiftId = :shiftId ORDER BY createdAt DESC")
    fun getByShift(shiftId: String): Flow<List<OrderEntity>>
    
    @Query("SELECT * FROM orders WHERE status = :status ORDER BY createdAt DESC")
    fun getByStatus(status: String): Flow<List<OrderEntity>>
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(order: OrderEntity)
    
    @Update
    suspend fun update(order: OrderEntity)
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertAll(orders: List<OrderEntity>)
    
    @Query("UPDATE orders SET syncedAt = :syncedAt WHERE id = :orderId")
    suspend fun markSynced(orderId: String, syncedAt: Instant)
    
    @Query("UPDATE orders SET subTotal = :subTotal, tax = :tax, total = :total WHERE id = :orderId")
    suspend fun updateTotals(orderId: String, subTotal: BigDecimal, tax: BigDecimal, total: BigDecimal)
    
    @Query("UPDATE orders SET status = :status WHERE id = :orderId")
    suspend fun updateStatus(orderId: String, status: String)
    
    @Query("UPDATE orders SET receiptNumber = :receiptNumber WHERE id = :orderId")
    suspend fun updateReceiptNumber(orderId: String, receiptNumber: String)
    
    @Query("UPDATE orders SET orderNumber = :orderNumber WHERE id = :orderId")
    suspend fun updateOrderNumber(orderId: String, orderNumber: String?)

    @Query("DELETE FROM orders WHERE id = :orderId")
    suspend fun delete(orderId: String)
}

@Dao
interface OrderLineDao {
    
    @Query("SELECT * FROM order_lines WHERE orderId = :orderId")
    suspend fun getByOrderId(orderId: String): List<OrderLineEntity>
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(orderLine: OrderLineEntity)
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertAll(orderLines: List<OrderLineEntity>)
    
    @Query("DELETE FROM order_lines WHERE orderId = :orderId")
    suspend fun deleteByOrder(orderId: String)

    @Query("UPDATE order_lines SET orderId = :newOrderId WHERE orderId = :oldOrderId")
    suspend fun updateOrderId(oldOrderId: String, newOrderId: String)
}

@Dao
interface OrderLineAddOnDao {
    
    @Query("SELECT * FROM order_line_addons WHERE orderLineId = :orderLineId")
    suspend fun getByOrderLineId(orderLineId: String): List<OrderLineAddOnEntity>
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(addOn: OrderLineAddOnEntity)
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertAll(addOns: List<OrderLineAddOnEntity>)
    
    @Query("DELETE FROM order_line_addons WHERE orderLineId = :orderLineId")
    suspend fun deleteByOrderLine(orderLineId: String)
}
