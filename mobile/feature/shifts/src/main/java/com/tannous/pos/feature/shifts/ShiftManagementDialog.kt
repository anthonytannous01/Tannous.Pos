package com.tannous.pos.feature.shifts

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import com.tannous.pos.core.ui.LocalIsArabic
import java.math.BigDecimal
import java.math.RoundingMode
import java.text.NumberFormat
import java.util.Locale

/** Whole-pound LBP display with thousands grouping, e.g. "1,600,000 LBP". */
internal fun formatLbp(value: BigDecimal): String =
    NumberFormat.getNumberInstance(Locale.US)
        .format(value.setScale(0, RoundingMode.HALF_UP)) + " LBP"

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun OpenShiftDialog(
    onConfirm: (BigDecimal, BigDecimal, String?) -> Unit,
    onDismiss: () -> Unit
) {
    var openingBalance by remember { mutableStateOf("") }
    var openingBalanceLbp by remember { mutableStateOf("") }
    var notes by remember { mutableStateOf("") }
    var hasError by remember { mutableStateOf(false) }
    var hasLbpError by remember { mutableStateOf(false) }
    val isArabic = LocalIsArabic.current

    Dialog(onDismissRequest = onDismiss) {
        Card(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp)
        ) {
            Column(
                modifier = Modifier.padding(16.dp)
            ) {
                Text(
                    text = if (isArabic) "فتح وردية" else "Open Shift",
                    style = MaterialTheme.typography.headlineSmall,
                    fontWeight = FontWeight.Bold,
                    modifier = Modifier.padding(bottom = 16.dp)
                )

                OutlinedTextField(
                    value = openingBalance,
                    onValueChange = {
                        openingBalance = it
                        hasError = false
                    },
                    label = { Text(if (isArabic) "رصيد الافتتاح (دولار)" else "Opening Balance (USD)") },
                    isError = hasError,
                    supportingText = if (hasError) {
                        { Text(if (isArabic) "يرجى إدخال مبلغ صحيح" else "Please enter a valid amount") }
                    } else null,
                    modifier = Modifier.fillMaxWidth()
                )

                Spacer(modifier = Modifier.height(16.dp))

                OutlinedTextField(
                    value = openingBalanceLbp,
                    onValueChange = {
                        openingBalanceLbp = it
                        hasLbpError = false
                    },
                    label = { Text(if (isArabic) "رصيد الافتتاح ل.ل (اختياري)" else "Opening Balance LBP (Optional)") },
                    isError = hasLbpError,
                    supportingText = if (hasLbpError) {
                        { Text(if (isArabic) "يرجى إدخال مبلغ صحيح" else "Please enter a valid amount") }
                    } else null,
                    modifier = Modifier.fillMaxWidth()
                )

                Spacer(modifier = Modifier.height(16.dp))

                OutlinedTextField(
                    value = notes,
                    onValueChange = { notes = it },
                    label = { Text(if (isArabic) "ملاحظات (اختياري)" else "Notes (Optional)") },
                    modifier = Modifier.fillMaxWidth(),
                    maxLines = 3
                )

                Spacer(modifier = Modifier.height(24.dp))

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
                            val balance = try {
                                BigDecimal(openingBalance)
                            } catch (e: NumberFormatException) {
                                hasError = true; return@Button
                            }
                            if (balance <= BigDecimal.ZERO) {
                                hasError = true; return@Button
                            }
                            val balanceLbp = if (openingBalanceLbp.isBlank()) BigDecimal.ZERO else try {
                                BigDecimal(openingBalanceLbp).also {
                                    if (it < BigDecimal.ZERO) { hasLbpError = true; return@Button }
                                }
                            } catch (e: NumberFormatException) {
                                hasLbpError = true; return@Button
                            }
                            onConfirm(balance, balanceLbp, notes.ifEmpty { null })
                        },
                        modifier = Modifier.weight(1f)
                    ) {
                        Text(if (isArabic) "فتح وردية" else "Open Shift")
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CashDropDialog(
    onConfirm: (BigDecimal, String?) -> Unit,
    onDismiss: () -> Unit
) {
    var amount by remember { mutableStateOf("") }
    var note by remember { mutableStateOf("") }
    var hasError by remember { mutableStateOf(false) }
    val isArabic = LocalIsArabic.current

    Dialog(onDismissRequest = onDismiss) {
        Card(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp)
        ) {
            Column(
                modifier = Modifier.padding(16.dp)
            ) {
                Text(
                    text = if (isArabic) "إيداع نقدي" else "Cash Drop",
                    style = MaterialTheme.typography.headlineSmall,
                    fontWeight = FontWeight.Bold,
                    modifier = Modifier.padding(bottom = 16.dp)
                )

                OutlinedTextField(
                    value = amount,
                    onValueChange = {
                        amount = it
                        hasError = false
                    },
                    label = { Text(if (isArabic) "المبلغ" else "Amount") },
                    isError = hasError,
                    supportingText = if (hasError) {
                        { Text(if (isArabic) "يرجى إدخال مبلغ صحيح" else "Please enter a valid amount") }
                    } else null,
                    modifier = Modifier.fillMaxWidth()
                )

                Spacer(modifier = Modifier.height(16.dp))

                OutlinedTextField(
                    value = note,
                    onValueChange = { note = it },
                    label = { Text(if (isArabic) "ملاحظة (اختياري)" else "Note (Optional)") },
                    modifier = Modifier.fillMaxWidth()
                )

                Spacer(modifier = Modifier.height(24.dp))

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
                            try {
                                val cashAmount = BigDecimal(amount)
                                if (cashAmount > BigDecimal.ZERO) {
                                    onConfirm(cashAmount, note.ifEmpty { null })
                                } else {
                                    hasError = true
                                }
                            } catch (e: NumberFormatException) {
                                hasError = true
                            }
                        },
                        modifier = Modifier.weight(1f)
                    ) {
                        Text(if (isArabic) "تسجيل الإيداع" else "Record Drop")
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CloseShiftDialog(
    shiftId: String,
    expectedCash: BigDecimal,
    expectedCashLbp: BigDecimal,
    onConfirm: (BigDecimal, BigDecimal, String?) -> Unit,
    onDismiss: () -> Unit
) {
    var actualCash by remember { mutableStateOf("") }
    var actualCashLbp by remember { mutableStateOf("") }
    var note by remember { mutableStateOf("") }
    var hasError by remember { mutableStateOf(false) }
    var hasLbpError by remember { mutableStateOf(false) }
    val isArabic = LocalIsArabic.current
    // LBP side only participates when the drawer actually held/handled LBP this shift.
    val hasLbpDrawer = expectedCashLbp.signum() != 0 || actualCashLbp.isNotBlank()

    Dialog(onDismissRequest = onDismiss) {
        Card(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp)
        ) {
            Column(
                modifier = Modifier.padding(16.dp)
            ) {
                Text(
                    text = if (isArabic) "إغلاق الوردية" else "Close Shift",
                    style = MaterialTheme.typography.headlineSmall,
                    fontWeight = FontWeight.Bold,
                    modifier = Modifier.padding(bottom = 16.dp)
                )

                Card(
                    colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.secondaryContainer)
                ) {
                    Column(
                        modifier = Modifier.padding(16.dp)
                    ) {
                        Text(
                            text = if (isArabic) "النقد المتوقع (دولار): ${expectedCash}" else "Expected Cash (USD): ${expectedCash}",
                            style = MaterialTheme.typography.titleMedium,
                            fontWeight = FontWeight.Bold
                        )

                        if (actualCash.isNotEmpty()) {
                            Spacer(modifier = Modifier.height(8.dp))
                            val variance = try {
                                BigDecimal(actualCash) - expectedCash
                            } catch (e: NumberFormatException) {
                                BigDecimal.ZERO
                            }
                            Text(
                                text = if (isArabic) "فرق الدولار: ${variance}" else "USD Variance: ${variance}",
                                style = MaterialTheme.typography.bodyMedium,
                                color = if (variance.signum() == 0) {
                                    MaterialTheme.colorScheme.primary
                                } else {
                                    MaterialTheme.colorScheme.error
                                }
                            )
                        }

                        if (hasLbpDrawer) {
                            Spacer(modifier = Modifier.height(8.dp))
                            Text(
                                text = if (isArabic) "النقد المتوقع ل.ل: ${formatLbp(expectedCashLbp)}"
                                       else "Expected Cash LBP: ${formatLbp(expectedCashLbp)}",
                                style = MaterialTheme.typography.titleMedium,
                                fontWeight = FontWeight.Bold
                            )

                            if (actualCashLbp.isNotEmpty()) {
                                Spacer(modifier = Modifier.height(8.dp))
                                val varianceLbp = try {
                                    BigDecimal(actualCashLbp) - expectedCashLbp
                                } catch (e: NumberFormatException) {
                                    BigDecimal.ZERO
                                }
                                Text(
                                    text = if (isArabic) "فرق ل.ل: ${formatLbp(varianceLbp)}"
                                           else "LBP Variance: ${formatLbp(varianceLbp)}",
                                    style = MaterialTheme.typography.bodyMedium,
                                    color = if (varianceLbp.signum() == 0) {
                                        MaterialTheme.colorScheme.primary
                                    } else {
                                        MaterialTheme.colorScheme.error
                                    }
                                )
                            }
                        }
                    }
                }

                Spacer(modifier = Modifier.height(16.dp))

                OutlinedTextField(
                    value = actualCash,
                    onValueChange = {
                        actualCash = it
                        hasError = false
                    },
                    label = { Text(if (isArabic) "عدد النقد الفعلي (دولار)" else "Actual Cash Count (USD)") },
                    isError = hasError,
                    supportingText = if (hasError) {
                        { Text(if (isArabic) "يرجى إدخال مبلغ صحيح" else "Please enter a valid amount") }
                    } else null,
                    modifier = Modifier.fillMaxWidth()
                )

                Spacer(modifier = Modifier.height(16.dp))

                OutlinedTextField(
                    value = actualCashLbp,
                    onValueChange = {
                        actualCashLbp = it
                        hasLbpError = false
                    },
                    label = {
                        Text(
                            if (isArabic) "عدد النقد الفعلي ل.ل"
                            else "Actual Cash Count (LBP)"
                        )
                    },
                    isError = hasLbpError,
                    supportingText = if (hasLbpError) {
                        { Text(if (isArabic) "يرجى إدخال مبلغ صحيح" else "Please enter a valid amount") }
                    } else null,
                    modifier = Modifier.fillMaxWidth()
                )

                Spacer(modifier = Modifier.height(16.dp))

                OutlinedTextField(
                    value = note,
                    onValueChange = { note = it },
                    label = { Text(if (isArabic) "ملاحظة (اختياري)" else "Note (Optional)") },
                    modifier = Modifier.fillMaxWidth()
                )

                Spacer(modifier = Modifier.height(24.dp))

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
                            val closingCount = try {
                                BigDecimal(actualCash)
                            } catch (e: NumberFormatException) {
                                hasError = true; return@Button
                            }
                            if (closingCount < BigDecimal.ZERO) {
                                hasError = true; return@Button
                            }
                            val closingCountLbp = if (actualCashLbp.isBlank()) BigDecimal.ZERO else try {
                                BigDecimal(actualCashLbp).also {
                                    if (it < BigDecimal.ZERO) { hasLbpError = true; return@Button }
                                }
                            } catch (e: NumberFormatException) {
                                hasLbpError = true; return@Button
                            }
                            onConfirm(closingCount, closingCountLbp, note.ifEmpty { null })
                        },
                        modifier = Modifier.weight(1f)
                    ) {
                        Text(if (isArabic) "إغلاق الوردية" else "Close Shift")
                    }
                }
            }
        }
    }
}
