package com.tannous.pos.core.data.repository

import com.tannous.pos.core.data.local.dao.*
import com.tannous.pos.core.data.local.entity.*
import com.tannous.pos.core.data.remote.OrderService
import com.tannous.pos.core.data.model.FinalizeOrderRequest
import com.tannous.pos.core.data.model.PaymentDto
import com.tannous.pos.core.data.model.OrderDto
import com.tannous.pos.core.data.model.VoidOrderRequest
import java.io.IOException
import com.tannous.pos.core.sync.OutboxManager
import kotlinx.coroutines.flow.Flow
import retrofit2.HttpException
import timber.log.Timber
import java.math.BigDecimal
import java.time.Instant
import java.time.LocalDate
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.*
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class OrderRepository @Inject constructor(
    private val orderDao: OrderDao,
    private val orderLineDao: OrderLineDao,
    private val orderLineAddOnDao: OrderLineAddOnDao,
    private val shiftDao: ShiftDao,
    private val orderService: OrderService,
    private val outboxManager: OutboxManager,
    private val settingsRepository: SettingsRepository
) {
    
    suspend fun startOrder(
        shiftId: String,
        customerId: String? = null,
        orderType: String = "DINE_IN"
    ): String {
        val orderId = UUID.randomUUID().toString()
        val order = OrderEntity(
            id = orderId,
            orderNumber = null,
            orderType = orderType,
            status = "PENDING",
            subTotal = BigDecimal.ZERO,
            discount = BigDecimal.ZERO,
            tax = BigDecimal.ZERO,
            total = BigDecimal.ZERO,
            createdAt = Instant.now(),
            receiptNumber = null,
            shiftId = shiftId,
            syncedAt = null,
            customerId = customerId,
            notes = null
        )
        
        orderDao.insert(order)
        Timber.d("Started new order: $orderId")
        return orderId
    }
    
    suspend fun addLine(
        orderId: String,
        menuItem: MenuItemEntity,
        quantity: Int,
        addOns: List<CartAddOn>
    ) {
        val lineId = UUID.randomUUID().toString()
        val line = OrderLineEntity(
            id = lineId,
            orderId = orderId,
            menuItemId = menuItem.id,
            quantity = quantity,
            unitPrice = menuItem.price,
            totalPrice = menuItem.price * BigDecimal.valueOf(quantity.toLong()),
            notes = null
        )
        
        orderLineDao.insert(line)
        
        // Add add-ons
        addOns.forEach { addOn ->
            val addOnLineId = UUID.randomUUID().toString()
            val addOnLine = OrderLineAddOnEntity(
                id = addOnLineId,
                orderLineId = lineId,
                addOnId = addOn.id,
                price = addOn.price
            )
            orderLineAddOnDao.insert(addOnLine)
        }
        
        updateOrderTotals(orderId)
        Timber.d("Added line to order $orderId: ${menuItem.name} x$quantity")
    }
    
    /**
     * Finalizes an order with the given payments.
     * Attempts to call the API directly. If offline or network fails, enqueues to outbox.
     * Returns Result with order ID on success, or failure with error message.
     */
    suspend fun finalizeOrder(
        orderId: String,
        payments: List<PaymentDto>,
        changeCurrency: String = "USD"
    ): Result<OrderDto> {
        return try {
            // orderId is the server UUID after createOrderFromCart re-keys the local row
            // Local totals for display only; the server total is authoritative online
            val lines = orderLineDao.getByOrderId(orderId)
            val subTotal = lines.sumOf { it.totalPrice }
            val tax = subTotal * settingsRepository.getTaxRate()
            val total = subTotal + tax
            
            // Update order with final totals locally
            orderDao.updateTotals(orderId, subTotal, tax, total)
            
            // Try to finalize via API first
            try {
                val finalizeRequest = FinalizeOrderRequest(payments = payments, changeCurrency = changeCurrency)
                val finalizedOrder = orderService.finalizeOrder(orderId, finalizeRequest)
                
                // Update local order with server response
                orderDao.updateStatus(orderId, finalizedOrder.status)
                finalizedOrder.receiptNumber?.let { receiptNumber ->
                    orderDao.updateReceiptNumber(orderId, receiptNumber)
                }
                orderDao.markSynced(orderId, Instant.now())
                
                Timber.d("Order $orderId finalized successfully via API. Receipt: ${finalizedOrder.receiptNumber}")
                Result.success(finalizedOrder)
                
            } catch (e: IOException) {
                // Network error - enqueue to outbox for offline sync
                Timber.w(e, "Network error finalizing order $orderId. Enqueuing to outbox.")
                
                // Mark order as PAID locally (will sync later)
                orderDao.updateStatus(orderId, "PAID")
                orderDao.updateReceiptNumber(orderId, "PENDING#${orderId.take(8)}")
                
                // Enqueue to outbox (changeCurrency rides in the payload; SyncController reads it)
                val finalizeRequest = FinalizeOrderRequest(payments = payments, changeCurrency = changeCurrency)
                outboxManager.enqueueOperation(
                    type = "FinalizeOrder",
                    payload = finalizeRequest,
                    orderId = orderId
                )
                
                // Trigger immediate push (will retry when network is available)
                outboxManager.triggerImmediatePush()
                
                // Return success with local order (marked as queued)
                val localOrder = orderDao.getById(orderId)!!
                val orderDto = OrderDto(
                    id = localOrder.id,
                    orderNumber = localOrder.orderNumber,
                    orderType = localOrder.orderType,
                    status = localOrder.status,
                    customerId = localOrder.customerId,
                    shiftId = localOrder.shiftId,
                    subTotal = localOrder.subTotal,
                    discount = localOrder.discount,
                    tax = localOrder.tax,
                    total = localOrder.total,
                    notes = localOrder.notes,
                    createdAt = localOrder.createdAt.toString(),
                    receiptNumber = localOrder.receiptNumber,
                    syncedAt = null
                )
                
                Result.success(orderDto)
                
            } catch (e: Exception) {
                // Other errors (validation, etc.) - don't enqueue, return failure
                Timber.e(e, "Error finalizing order $orderId via API")
                Result.failure(e)
            }
            
        } catch (e: Exception) {
            Timber.e(e, "Error finalizing order $orderId")
            Result.failure(e)
        }
    }
    
    /**
     * Creates an order from cart items and returns the order ID.
     * If order already exists on server, returns existing order ID.
     *
     * @param orderType Backend OrderType int: 1=DineIn, 2=Takeaway, 3=Delivery
     */
    suspend fun createOrderFromCart(
        shiftId: String,
        cartItems: List<CartItem>,
        customerId: String? = null,
        notes: String? = null,
        orderType: Int = 1
    ): Result<String> {
        val orderTypeStr = when (orderType) {
            2 -> "TAKEAWAY"
            3 -> "DELIVERY"
            else -> "DINE_IN"
        }
        return try {
            // Create order locally first
            val orderId = startOrder(shiftId, customerId, orderTypeStr)
            
            // Add all cart items as order lines
            for (cartItem in cartItems) {
                val addOns = cartItem.addOns.map { addOn: CartAddOn ->
                    CartAddOn(
                        id = addOn.id,
                        name = addOn.name,
                        price = addOn.price,
                        quantity = addOn.quantity
                    )
                }
                addLine(orderId, cartItem.menuItem, cartItem.quantity, addOns)
            }
            
            // Try to sync to server
            try {
                val createRequest = com.tannous.pos.core.data.model.CreateOrderRequest(
                    orderType = orderType,
                    customerId = customerId,
                    orderLines = cartItems.map { item: CartItem ->
                        com.tannous.pos.core.data.model.CreateOrderLineRequest(
                            menuItemId = item.menuItem.id,
                            quantity = item.quantity,
                            unitPrice = item.menuItem.price.toDouble(),
                            addOns = item.addOns.map { addOn: CartAddOn ->
                                com.tannous.pos.core.data.model.CreateOrderLineAddOnRequest(
                                    addOnId = addOn.id
                                )
                            }
                        )
                    },
                    notes = notes
                )
                
                val serverOrder = orderService.createOrder(createRequest)
                
                // Re-key local Room row (and lines) from local UUID to server UUID
                rekeyOrderToServerId(localOrderId = orderId, serverOrder = serverOrder)
                
                Timber.d("Order ${serverOrder.id} created and synced to server (re-keyed from $orderId)")
                Result.success(serverOrder.id)
                
            } catch (e: IOException) {
                // Network error - order is created locally, will sync later
                Timber.w(e, "Network error creating order. Order $orderId created locally.")
                Result.success(orderId)
                
            } catch (e: Exception) {
                Timber.e(e, "Error creating order on server")
                Result.failure(e)
            }
            
        } catch (e: Exception) {
            Timber.e(e, "Error creating order from cart")
            Result.failure(e)
        }
    }
    
    private suspend fun updateOrderTotals(orderId: String) {
        val lines = orderLineDao.getByOrderId(orderId)
        val subTotal = lines.sumOf { it.totalPrice }
        val tax = subTotal * settingsRepository.getTaxRate()
        val total = subTotal + tax
        
        orderDao.updateTotals(orderId, subTotal, tax, total)
    }
    
    suspend fun getOrderById(orderId: String): OrderEntity? {
        return orderDao.getById(orderId)
    }

    fun getShiftOrders(shiftId: String): Flow<List<OrderEntity>> =
        orderDao.getByShift(shiftId)

    fun observeAllOrders(): Flow<List<OrderEntity>> = orderDao.getAll()

    suspend fun refreshOrders(): Result<Int> {
        return try {
            val startDate = LocalDate.now()
                .minusDays(7)
                .atStartOfDay(ZoneId.systemDefault())
                .format(DateTimeFormatter.ISO_LOCAL_DATE_TIME)
            val orders = orderService.getOrders(startDate = startDate)
            orders.forEach { dto ->
                orderDao.insert(dto.toEntityForHistory())
            }
            Timber.d("Refreshed ${orders.size} orders from server")
            Result.success(orders.size)
        } catch (e: HttpException) {
            Result.failure(RuntimeException("Server error: ${e.code()}"))
        } catch (e: IOException) {
            Result.failure(IOException("Offline — showing cached orders"))
        } catch (e: Exception) {
            Timber.e(e, "Error refreshing orders")
            Result.failure(e)
        }
    }

    suspend fun voidOrder(orderId: String, reason: String): Result<OrderDto> {
        val trimmed = reason.trim()
        if (trimmed.isBlank()) {
            return Result.failure(IllegalArgumentException("A reason is required to void an order"))
        }
        if (trimmed.length > 500) {
            return Result.failure(IllegalArgumentException("Reason must be 500 characters or fewer"))
        }

        val localOrder = orderDao.getById(orderId)

        if (localOrder != null &&
            (localOrder.syncedAt == null ||
                localOrder.receiptNumber?.startsWith("PENDING") == true)
        ) {
            return Result.failure(
                IllegalStateException("Order must sync before it can be voided")
            )
        }

        if (localOrder != null && localOrder.status.isAlreadyVoidedStatus()) {
            val dto = OrderDto(
                id = localOrder.id,
                orderNumber = localOrder.orderNumber,
                orderType = localOrder.orderType,
                status = localOrder.status,
                customerId = localOrder.customerId,
                shiftId = localOrder.shiftId,
                subTotal = localOrder.subTotal,
                discount = localOrder.discount,
                tax = localOrder.tax,
                total = localOrder.total,
                notes = localOrder.notes,
                createdAt = localOrder.createdAt.toString(),
                receiptNumber = localOrder.receiptNumber,
                syncedAt = localOrder.syncedAt?.toString()
            )
            return Result.success(dto)
        }

        return try {
            val voidedOrder = orderService.voidOrder(orderId, VoidOrderRequest(trimmed))
            orderDao.updateStatus(orderId, voidedOrder.status)
            Timber.d("Order $orderId voided. Server status: ${voidedOrder.status}")
            Result.success(voidedOrder)
        } catch (e: IOException) {
            Timber.w(e, "Network error voiding order $orderId")
            Result.failure(IOException("No connection. Please check network and try again."))
        } catch (e: Exception) {
            Timber.e(e, "Error voiding order $orderId")
            Result.failure(e)
        }
    }
    
    suspend fun getOrderLines(orderId: String): List<OrderLineEntity> {
        return orderLineDao.getByOrderId(orderId)
    }
    
    suspend fun getOrderLineAddOns(orderLineId: String): List<OrderLineAddOnEntity> {
        return orderLineAddOnDao.getByOrderLineId(orderLineId)
    }
    
    suspend fun markOrderSynced(orderId: String, serverReceiptNumber: String) {
        orderDao.updateReceiptNumber(orderId, serverReceiptNumber)
        orderDao.markSynced(orderId, Instant.now())
        Timber.d("Order $orderId synced with receipt: $serverReceiptNumber")
    }

    /**
     * After a successful POST /orders, replace the optimistic local-UUID row with one keyed by
     * [serverOrder.id]. Order lines are re-keyed in the same transaction sequence so finalize
     * and local totals lookups work under the server id. Only called on server success — on
     * IOException the local UUID row is left untouched for offline/outbox use.
     */
    private suspend fun rekeyOrderToServerId(localOrderId: String, serverOrder: OrderDto) {
        val localOrder = orderDao.getById(localOrderId)
            ?: throw IllegalStateException("Local order $localOrderId not found for re-key")

        if (localOrderId == serverOrder.id) {
            orderDao.updateOrderNumber(localOrderId, serverOrder.orderNumber)
            orderDao.markSynced(localOrderId, Instant.now())
            return
        }

        // Point line rows at the server id before removing the old order row
        orderLineDao.updateOrderId(localOrderId, serverOrder.id)
        orderDao.delete(localOrderId)
        orderDao.insert(serverOrder.toEntity(localOrder))
    }

    private fun OrderDto.toEntity(fallback: OrderEntity): OrderEntity {
        val created = try {
            Instant.parse(createdAt)
        } catch (e: Exception) {
            fallback.createdAt
        }
        return OrderEntity(
            id = id,
            orderNumber = orderNumber,
            orderType = orderType,
            status = status,
            customerId = customerId ?: fallback.customerId,
            shiftId = shiftId ?: fallback.shiftId,
            subTotal = subTotal,
            discount = discount,
            tax = tax,
            total = total,
            notes = notes ?: fallback.notes,
            createdAt = created,
            receiptNumber = receiptNumber,
            syncedAt = Instant.now()
        )
    }
}

data class CartItem(
    val menuItem: MenuItemEntity,
    val quantity: Int,
    val addOns: List<CartAddOn>
)

data class CartAddOn(
    val id: String,
    val name: String,
    val price: BigDecimal,
    val quantity: Int
)
