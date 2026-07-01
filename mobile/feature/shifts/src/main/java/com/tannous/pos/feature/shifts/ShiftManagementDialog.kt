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

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun OpenShiftDialog(
    onConfirm: (BigDecimal, String?) -> Unit,
    onDismiss: () -> Unit
) {
    var openingBalance by remember { mutableStateOf("") }
    var notes by remember { mutableStateOf("") }
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
                    label = { Text(if (isArabic) "رصيد الافتتاح" else "Opening Balance") },
                    isError = hasError,
                    supportingText = if (hasError) {
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
                            try {
                                val balance = BigDecimal(openingBalance)
                                if (balance > BigDecimal.ZERO) {
                                    onConfirm(balance, notes.ifEmpty { null })
                                } else {
                                    hasError = true
                                }
                            } catch (e: NumberFormatException) {
                                hasError = true
                            }
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
    onConfirm: (BigDecimal, String?) -> Unit,
    onDismiss: () -> Unit
) {
    var actualCash by remember { mutableStateOf("") }
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
                            text = if (isArabic) "النقد المتوقع: ${expectedCash}" else "Expected Cash: ${expectedCash}",
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
                                text = if (isArabic) "الفرق: ${variance}" else "Variance: ${variance}",
                                style = MaterialTheme.typography.bodyMedium,
                                color = if (variance == BigDecimal.ZERO) {
                                    MaterialTheme.colorScheme.primary
                                } else {
                                    MaterialTheme.colorScheme.error
                                }
                            )
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
                    label = { Text(if (isArabic) "عدد النقد الفعلي" else "Actual Cash Count") },
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
                                val closingCount = BigDecimal(actualCash)
                                if (closingCount >= BigDecimal.ZERO) {
                                    onConfirm(closingCount, note.ifEmpty { null })
                                } else {
                                    hasError = true
                                }
                            } catch (e: NumberFormatException) {
                                hasError = true
                            }
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
