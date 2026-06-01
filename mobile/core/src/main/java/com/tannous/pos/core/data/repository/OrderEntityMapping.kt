package com.tannous.pos.core.data.repository

import com.tannous.pos.core.data.local.entity.OrderEntity
import com.tannous.pos.core.data.model.OrderDto
import java.time.Instant

fun OrderDto.toEntityForHistory(): OrderEntity {
    val created = runCatching { Instant.parse(createdAt) }.getOrElse { Instant.now() }
    return OrderEntity(
        id = id,
        orderNumber = orderNumber,
        orderType = orderType,
        status = status,
        customerId = customerId,
        shiftId = shiftId,
        subTotal = subTotal,
        discount = discount,
        tax = tax,
        total = total,
        notes = notes,
        createdAt = created,
        receiptNumber = receiptNumber,
        syncedAt = Instant.now()
    )
}

fun OrderEntity.toOrderDto(): OrderDto = OrderDto(
    id = id,
    orderNumber = orderNumber,
    orderType = orderType,
    status = status,
    customerId = customerId,
    shiftId = shiftId,
    subTotal = subTotal,
    discount = discount,
    tax = tax,
    total = total,
    notes = notes,
    createdAt = createdAt.toString(),
    receiptNumber = receiptNumber,
    syncedAt = syncedAt?.toString()
)
