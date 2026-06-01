package com.tannous.pos.core.util

import java.text.NumberFormat
import java.util.Currency
import java.util.Locale

fun currencyFormatterFor(currencyCode: String): NumberFormat {
    return try {
        NumberFormat.getCurrencyInstance().apply {
            currency = Currency.getInstance(currencyCode)
        }
    } catch (_: Exception) {
        NumberFormat.getCurrencyInstance(Locale.US)
    }
}
