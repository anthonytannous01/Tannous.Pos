package com.tannous.pos.core.data.local.entity

import androidx.room.ColumnInfo
import androidx.room.Entity
import androidx.room.PrimaryKey
import androidx.room.TypeConverters
import com.tannous.pos.core.data.local.converter.Converters
import java.math.BigDecimal
import java.time.Instant

@Entity(tableName = "categories")
@TypeConverters(Converters::class)
data class CategoryEntity(
    @PrimaryKey
    val id: String,
    val name: String,
    val nameAr: String? = null,
    val description: String?,
    val displayOrder: Int,
    val isActive: Boolean,
    val updatedAt: Instant,
    val isDeleted: Boolean
)

@Entity(tableName = "menu_items")
@TypeConverters(Converters::class)
data class MenuItemEntity(
    @PrimaryKey
    val id: String,
    val categoryId: String,
    val name: String,
    val nameAr: String? = null,
    val description: String?,
    val descriptionAr: String? = null,
    val price: BigDecimal,
    val imageUrl: String?,
    val isActive: Boolean,
    val hasAddOns: Boolean = false,
    val updatedAt: Instant,
    val isDeleted: Boolean,
    val version: String?
)

@Entity(tableName = "addons")
@TypeConverters(Converters::class)
data class AddOnEntity(
    @PrimaryKey
    val id: String,
    val name: String,
    val price: BigDecimal,
    val isActive: Boolean,
    val updatedAt: Instant,
    val isDeleted: Boolean,
    val version: String?
)

@Entity(tableName = "customers")
@TypeConverters(Converters::class)
data class CustomerEntity(
    @PrimaryKey
    val id: String,
    val firstName: String,
    val lastName: String,
    val email: String?,
    val phone: String?,
    val address: String?,
    val notes: String?,
    val isActive: Boolean,
    val lastVisitDate: Instant?,
    val totalOrders: Int,
    val isDeleted: Boolean,
    val deletedAt: Instant?,
    val version: String?
)

@Entity(tableName = "orders")
@TypeConverters(Converters::class)
data class OrderEntity(
    @PrimaryKey
    val id: String,
    val orderNumber: String?,
    val orderType: String,
    val status: String,
    val customerId: String?,
    val shiftId: String?,
    val subTotal: BigDecimal,
    val discount: BigDecimal,
    val tax: BigDecimal,
    val total: BigDecimal,
    val notes: String?,
    val createdAt: Instant,
    val receiptNumber: String?,
    val syncedAt: Instant?,
    /**
     * True once the server has acknowledged this order (POST /orders succeeded, or the row came
     * down from the server). False for an order started on this device that has not reached the
     * server yet.
     *
     * Finalize refuses to queue payment for an order with this false: the outbox would carry a
     * FinalizeOrder for an id the server has never seen, which fails on replay and loses the sale
     * after the money was taken. See OfflineFinalizeGuard.
     *
     * The Kotlin default is false because a freshly started order has not reached the server. The
     * SQL default is 1 for the opposite reason: rows that already existed when this column was
     * added are historical and were server-known.
     */
    @ColumnInfo(defaultValue = "1")
    val existsOnServer: Boolean = false
)

@Entity(tableName = "order_lines")
@TypeConverters(Converters::class)
data class OrderLineEntity(
    @PrimaryKey
    val id: String,
    val orderId: String,
    val menuItemId: String,
    val quantity: Int,
    val unitPrice: BigDecimal,
    val totalPrice: BigDecimal,
    val notes: String?
)

@Entity(tableName = "order_line_addons")
@TypeConverters(Converters::class)
data class OrderLineAddOnEntity(
    @PrimaryKey
    val id: String,
    val orderLineId: String,
    val addOnId: String,
    val price: BigDecimal
)

@Entity(tableName = "shifts")
@TypeConverters(Converters::class)
data class ShiftEntity(
    @PrimaryKey
    val id: String,
    val shiftNumber: String,
    val startTime: Instant,
    val endTime: Instant?,
    val openedAt: Instant,
    val closedAt: Instant?,
    val status: String,
    val openingBalance: BigDecimal,
    val closingBalance: BigDecimal?,
    val expectedCash: BigDecimal?,
    val actualCash: BigDecimal?,
    val variance: BigDecimal?,
    val isDeleted: Boolean = false,
    val deletedAt: Instant? = null,
    val syncedAt: Instant? = null
)

@Entity(tableName = "key_value")
data class KeyValueEntity(
    @PrimaryKey
    val key: String,
    val value: String
)

@Entity(tableName = "outbox_operations")
@TypeConverters(Converters::class)
data class OutboxOperationEntity(
    @PrimaryKey
    val operationId: String,
    val type: String,
    val payloadJson: String,
    val createdAt: Instant,
    val attempt: Int,
    val lastError: String?,
    val status: OutboxStatus
)

enum class OutboxStatus {
    PENDING,
    SENT,
    FAILED,
    FAILED_CONFLICT
}
