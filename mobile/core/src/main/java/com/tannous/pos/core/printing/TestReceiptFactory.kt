package com.tannous.pos.core.printing

import com.tannous.pos.core.data.model.ReceiptDto
import com.tannous.pos.core.data.model.ReceiptLineDto
import com.tannous.pos.core.data.model.ReceiptPaymentDto
import java.math.BigDecimal
import java.time.Instant

/**
 * A representative receipt used by the "Print Test Receipt" action in printer settings.
 *
 * Exercises the full layout — long item name, discount, VAT, dual currency, change due —
 * so a test print surfaces alignment problems that a minimal receipt would hide.
 */
object TestReceiptFactory {

    fun sample(): ReceiptDto = ReceiptDto(
        orderId = "00000000-0000-0000-0000-000000000001",
        orderNumber = "TEST-001",
        orderType = "Dine-In",
        printedAt = Instant.now().toString(),
        businessName = "Tannous Test Kitchen",
        businessPhone = "+961 1 234 567",
        businessAddress = "Beirut, Lebanon",
        tableLabel = "T3",
        lines = listOf(
            ReceiptLineDto(
                name = "Shawarma Plate",
                qty = 2,
                unitPrice = BigDecimal("12.50"),
                lineTotal = BigDecimal("25.00")
            ),
            ReceiptLineDto(
                name = "Fresh Orange Juice - Large Glass",
                qty = 1,
                unitPrice = BigDecimal("4.00"),
                lineTotal = BigDecimal("4.00")
            )
        ),
        subTotal = BigDecimal("29.00"),
        discountAmount = BigDecimal("1.00"),
        taxAmount = BigDecimal("3.08"),
        totalUsd = BigDecimal("31.08"),
        totalLbp = BigDecimal("2780000"),
        stampDutyEnabled = false,
        payments = listOf(
            ReceiptPaymentDto(method = "Cash", amount = BigDecimal("35.00"))
        ),
        amountTendered = BigDecimal("35.00"),
        changeDue = BigDecimal("3.92")
    )
}
