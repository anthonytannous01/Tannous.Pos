package com.tannous.pos.core.data.repository

/** Server numeric strings (1=Open, 6=Paid, 9=Void) plus local offline strings. */
fun String.isVoidableStatus(): Boolean =
    this in setOf("1", "6", "Paid", "Open", "PAID", "OPEN")

fun String.isAlreadyVoidedStatus(): Boolean =
    this in setOf("9", "Void", "VOID")
