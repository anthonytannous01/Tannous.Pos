package com.tannous.pos.feature.sell

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import com.tannous.pos.core.data.model.PaymentDto
import com.tannous.pos.core.ui.LocalIsArabic
import com.tannous.pos.core.util.currencyFormatterFor
import java.math.BigDecimal
import java.math.RoundingMode
import java.text.NumberFormat
import java.util.Locale

enum class PaymentMethod {
    CASH, LBP_CASH, CARD, OTHER
}

private fun lbpFormat(value: BigDecimal): String =
    NumberFormat.getNumberInstance(Locale.US)
        .format(value.setScale(0, RoundingMode.HALF_UP)) + " LBP"

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PaymentSelectionDialog(
    total: BigDecimal,
    currencyCode: String = "USD",
    /** LBP per USD from business settings. ZERO hides all LBP options. */
    exchangeRateLbpPerUsd: BigDecimal = BigDecimal.ZERO,
    onConfirm: (payments: List<PaymentDto>, changeCurrency: String) -> Unit,
    onDismiss: () -> Unit
) {
    var selectedMethod by remember { mutableStateOf<PaymentMethod?>(null) }
    var cashAmount by remember { mutableStateOf("") }
    var lbpAmount by remember { mutableStateOf("") }
    var cardAmount by remember { mutableStateOf("") }
    var otherAmount by remember { mutableStateOf("") }
    var otherMethodName by remember { mutableStateOf("") }
    var changeCurrency by remember { mutableStateOf("USD") }
    val isArabic = LocalIsArabic.current

    val currencyFormatter = remember(currencyCode) { currencyFormatterFor(currencyCode) }
    val lbpEnabled = exchangeRateLbpPerUsd > BigDecimal.ZERO

    // Calculate amounts
    val cashAmountDecimal = try {
        if (cashAmount.isNotEmpty()) BigDecimal(cashAmount) else BigDecimal.ZERO
    } catch (e: NumberFormatException) {
        BigDecimal.ZERO
    }

    // Raw LBP tendered; its USD equivalent participates in settlement math.
    val lbpAmountDecimal = try {
        if (lbpAmount.isNotEmpty()) BigDecimal(lbpAmount) else BigDecimal.ZERO
    } catch (e: NumberFormatException) {
        BigDecimal.ZERO
    }
    val lbpAmountUsd = if (lbpEnabled && lbpAmountDecimal > BigDecimal.ZERO) {
        // Same rounding as the backend (4 dp, away from zero) so both sides agree.
        lbpAmountDecimal.divide(exchangeRateLbpPerUsd, 4, RoundingMode.HALF_UP)
    } else {
        BigDecimal.ZERO
    }

    val cardAmountDecimal = try {
        if (cardAmount.isNotEmpty()) BigDecimal(cardAmount) else BigDecimal.ZERO
    } catch (e: NumberFormatException) {
        BigDecimal.ZERO
    }

    val otherAmountDecimal = try {
        if (otherAmount.isNotEmpty()) BigDecimal(otherAmount) else BigDecimal.ZERO
    } catch (e: NumberFormatException) {
        BigDecimal.ZERO
    }

    val anyCashTendered = cashAmountDecimal > BigDecimal.ZERO || lbpAmountUsd > BigDecimal.ZERO
    val totalPaid = cashAmountDecimal + lbpAmountUsd + cardAmountDecimal + otherAmountDecimal
    val remaining = total - totalPaid
    val change = if (anyCashTendered && totalPaid > total) {
        totalPaid - total
    } else {
        BigDecimal.ZERO
    }
    val changeInLbp = if (lbpEnabled) {
        change.multiply(exchangeRateLbpPerUsd).setScale(0, RoundingMode.HALF_UP)
    } else {
        BigDecimal.ZERO
    }

    Dialog(onDismissRequest = onDismiss) {
        Card(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp)
        ) {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(16.dp)
                    .verticalScroll(rememberScrollState())
            ) {
                Text(
                    text = if (isArabic) "اختر طريقة الدفع" else "Select Payment Method",
                    style = MaterialTheme.typography.headlineSmall,
                    fontWeight = FontWeight.Bold,
                    modifier = Modifier.padding(bottom = 16.dp)
                )

                // Total display
                Card(
                    colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.primaryContainer),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Column(
                        modifier = Modifier.padding(16.dp)
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Text(
                                text = if (isArabic) "المبلغ الإجمالي" else "Total Amount",
                                style = MaterialTheme.typography.titleMedium,
                                fontWeight = FontWeight.Bold
                            )
                            Text(
                                text = currencyFormatter.format(total),
                                style = MaterialTheme.typography.headlineMedium,
                                fontWeight = FontWeight.Bold
                            )
                        }
                        if (lbpEnabled) {
                            Text(
                                text = "≈ " + lbpFormat(total.multiply(exchangeRateLbpPerUsd)),
                                style = MaterialTheme.typography.bodyMedium,
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                                modifier = Modifier.align(Alignment.End)
                            )
                        }
                    }
                }

                Spacer(modifier = Modifier.height(16.dp))

                // Payment method selection
                Text(
                    text = if (isArabic) "طريقة الدفع" else "Payment Method",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    modifier = Modifier.padding(bottom = 8.dp)
                )

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    FilterChip(
                        selected = selectedMethod == PaymentMethod.CASH,
                        onClick = { selectedMethod = PaymentMethod.CASH },
                        label = { Text(if (isArabic) "نقداً $" else "Cash $") },
                        modifier = Modifier.weight(1f)
                    )
                    if (lbpEnabled) {
                        FilterChip(
                            selected = selectedMethod == PaymentMethod.LBP_CASH,
                            onClick = { selectedMethod = PaymentMethod.LBP_CASH },
                            label = { Text(if (isArabic) "نقد ل.ل" else "LBP") },
                            modifier = Modifier.weight(1f)
                        )
                    }
                    FilterChip(
                        selected = selectedMethod == PaymentMethod.CARD,
                        onClick = { selectedMethod = PaymentMethod.CARD },
                        label = { Text(if (isArabic) "بطاقة" else "Card") },
                        modifier = Modifier.weight(1f)
                    )
                    FilterChip(
                        selected = selectedMethod == PaymentMethod.OTHER,
                        onClick = { selectedMethod = PaymentMethod.OTHER },
                        label = { Text(if (isArabic) "أخرى" else "Other") },
                        modifier = Modifier.weight(1f)
                    )
                }

                Spacer(modifier = Modifier.height(16.dp))

                // Payment amount inputs
                when (selectedMethod) {
                    PaymentMethod.CASH -> {
                        OutlinedTextField(
                            value = cashAmount,
                            onValueChange = { cashAmount = it },
                            label = { Text(if (isArabic) "مبلغ نقدي (دولار)" else "Cash Amount (USD)") },
                            modifier = Modifier.fillMaxWidth(),
                            singleLine = true
                        )
                    }
                    PaymentMethod.LBP_CASH -> {
                        OutlinedTextField(
                            value = lbpAmount,
                            onValueChange = { lbpAmount = it },
                            label = { Text(if (isArabic) "المبلغ بالليرة" else "Amount in LBP") },
                            supportingText = {
                                if (lbpAmountUsd > BigDecimal.ZERO) {
                                    Text("= ${currencyFormatter.format(lbpAmountUsd)}")
                                } else {
                                    Text(
                                        if (isArabic) "سعر الصرف: ${lbpFormat(exchangeRateLbpPerUsd)} / $1"
                                        else "Rate: ${lbpFormat(exchangeRateLbpPerUsd)} / $1"
                                    )
                                }
                            },
                            modifier = Modifier.fillMaxWidth(),
                            singleLine = true
                        )
                    }
                    PaymentMethod.CARD -> {
                        OutlinedTextField(
                            value = cardAmount,
                            onValueChange = { cardAmount = it },
                            label = { Text(if (isArabic) "مبلغ البطاقة" else "Card Amount") },
                            modifier = Modifier.fillMaxWidth(),
                            singleLine = true
                        )
                    }
                    PaymentMethod.OTHER -> {
                        OutlinedTextField(
                            value = otherMethodName,
                            onValueChange = { otherMethodName = it },
                            label = { Text(if (isArabic) "اسم طريقة الدفع" else "Payment Method Name") },
                            modifier = Modifier.fillMaxWidth(),
                            singleLine = true
                        )
                        Spacer(modifier = Modifier.height(8.dp))
                        OutlinedTextField(
                            value = otherAmount,
                            onValueChange = { otherAmount = it },
                            label = { Text(if (isArabic) "المبلغ" else "Amount") },
                            modifier = Modifier.fillMaxWidth(),
                            singleLine = true
                        )
                    }
                    null -> {
                        Text(
                            text = if (isArabic) "يرجى اختيار طريقة دفع" else "Please select a payment method",
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }

                // Change due + change currency choice (cashier decides per sale)
                if (change > BigDecimal.ZERO) {
                    Spacer(modifier = Modifier.height(12.dp))
                    Text(
                        text = if (changeCurrency == "LBP" && lbpEnabled) {
                            if (isArabic) "الباقي: ${lbpFormat(changeInLbp)}"
                            else "Change: ${lbpFormat(changeInLbp)}"
                        } else {
                            if (isArabic) "الباقي: ${currencyFormatter.format(change)}"
                            else "Change: ${currencyFormatter.format(change)}"
                        },
                        style = MaterialTheme.typography.titleMedium,
                        color = MaterialTheme.colorScheme.primary,
                        fontWeight = FontWeight.Bold
                    )
                    if (lbpEnabled) {
                        Spacer(modifier = Modifier.height(8.dp))
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.spacedBy(8.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Text(
                                text = if (isArabic) "عملة الباقي:" else "Change in:",
                                style = MaterialTheme.typography.bodyMedium
                            )
                            FilterChip(
                                selected = changeCurrency == "USD",
                                onClick = { changeCurrency = "USD" },
                                label = { Text("USD") }
                            )
                            FilterChip(
                                selected = changeCurrency == "LBP",
                                onClick = { changeCurrency = "LBP" },
                                label = { Text("LBP") }
                            )
                        }
                    }
                }

                // Remaining amount display
                if (totalPaid < total && totalPaid > BigDecimal.ZERO) {
                    Spacer(modifier = Modifier.height(16.dp))
                    Text(
                        text = if (isArabic) "المتبقي: ${currencyFormatter.format(remaining)}" else "Remaining: ${currencyFormatter.format(remaining)}",
                        style = MaterialTheme.typography.titleMedium,
                        color = MaterialTheme.colorScheme.error,
                        fontWeight = FontWeight.Bold
                    )
                }

                Spacer(modifier = Modifier.height(24.dp))

                // Action buttons
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    OutlinedButton(
                        onClick = onDismiss,
                        modifier = Modifier.weight(1f)
                    ) {
                        Text(if (isArabic) "إلغاء" else "Cancel")
                    }

                    Button(
                        onClick = {
                            val payments = mutableListOf<PaymentDto>()

                            if (cashAmountDecimal > BigDecimal.ZERO) {
                                payments.add(
                                    PaymentDto(
                                        paymentMethod = "CASH",
                                        amount = cashAmountDecimal.toDouble()
                                    )
                                )
                            }

                            if (lbpAmountDecimal > BigDecimal.ZERO) {
                                payments.add(
                                    PaymentDto(
                                        paymentMethod = "CASH",
                                        amount = lbpAmountDecimal.toDouble(), // raw LBP
                                        tenderedCurrency = "LBP",
                                        notes = "= ${currencyFormatter.format(lbpAmountUsd)}"
                                    )
                                )
                            }

                            if (cardAmountDecimal > BigDecimal.ZERO) {
                                payments.add(
                                    PaymentDto(
                                        paymentMethod = "CARD",
                                        amount = cardAmountDecimal.toDouble(),
                                        transactionId = null,
                                        notes = null
                                    )
                                )
                            }

                            if (otherAmountDecimal > BigDecimal.ZERO) {
                                payments.add(
                                    PaymentDto(
                                        paymentMethod = otherMethodName.ifEmpty { "OTHER" },
                                        amount = otherAmountDecimal.toDouble(),
                                        notes = null
                                    )
                                )
                            }

                            if (payments.isNotEmpty() && totalPaid >= total) {
                                // LBP change only makes sense when change exists and LBP is enabled
                                val effectiveChangeCurrency =
                                    if (change > BigDecimal.ZERO && changeCurrency == "LBP" && lbpEnabled) "LBP" else "USD"
                                onConfirm(payments, effectiveChangeCurrency)
                            }
                        },
                        modifier = Modifier.weight(1f),
                        enabled = selectedMethod != null && totalPaid >= total && totalPaid > BigDecimal.ZERO
                    ) {
                        Text(if (isArabic) "إتمام الطلب" else "Finalize Order")
                    }
                }
            }
        }
    }
}
