package com.tannous.pos.feature.sell

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import com.tannous.pos.core.util.currencyFormatterFor
import java.math.BigDecimal

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CashPaymentDialog(
    total: BigDecimal,
    currencyCode: String = "USD",
    onConfirm: (BigDecimal) -> Unit,
    onDismiss: () -> Unit
) {
    var cashTendered by remember { mutableStateOf("") }
    var hasError by remember { mutableStateOf(false) }
    
    val currencyFormatter = remember(currencyCode) {
        currencyFormatterFor(currencyCode)
    }
    val change = try {
        val tendered = BigDecimal(cashTendered)
        if (tendered >= total) tendered - total else BigDecimal.ZERO
    } catch (e: NumberFormatException) {
        BigDecimal.ZERO
    }
    
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
                    text = "Cash Payment",
                    style = MaterialTheme.typography.headlineSmall,
                    fontWeight = FontWeight.Bold,
                    modifier = Modifier.padding(bottom = 16.dp)
                )
                
                // Total display
                Card(
                    colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.primaryContainer)
                ) {
                    Column(
                        modifier = Modifier.padding(16.dp)
                    ) {
                        Text(
                            text = "Total Amount",
                            style = MaterialTheme.typography.titleMedium,
                            fontWeight = FontWeight.Bold
                        )
                        Text(
                            text = currencyFormatter.format(total),
                            style = MaterialTheme.typography.headlineMedium,
                            fontWeight = FontWeight.Bold
                        )
                    }
                }
                
                Spacer(modifier = Modifier.height(16.dp))
                
                OutlinedTextField(
                    value = cashTendered,
                    onValueChange = { 
                        cashTendered = it
                        hasError = false
                    },
                    label = { Text("Cash Received") },
                    isError = hasError,
                    supportingText = if (hasError) {
                        { Text("Amount must be greater than or equal to total") }
                    } else null,
                    modifier = Modifier.fillMaxWidth()
                )
                
                // Change calculation
                if (cashTendered.isNotEmpty() && change >= BigDecimal.ZERO) {
                    Spacer(modifier = Modifier.height(16.dp))
                    
                    Card(
                        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.secondaryContainer)
                    ) {
                        Column(
                            modifier = Modifier.padding(16.dp)
                        ) {
                            Text(
                                text = "Change Due",
                                style = MaterialTheme.typography.titleMedium,
                                fontWeight = FontWeight.Bold
                            )
                            Text(
                                text = currencyFormatter.format(change),
                                style = MaterialTheme.typography.headlineSmall,
                                fontWeight = FontWeight.Bold,
                                color = MaterialTheme.colorScheme.primary
                            )
                        }
                    }
                }
                
                Spacer(modifier = Modifier.height(24.dp))
                
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    OutlinedButton(
                        onClick = onDismiss,
                        modifier = Modifier.weight(1f)
                    ) {
                        Text("Cancel")
                    }
                    
                    Button(
                        onClick = {
                            try {
                                val tendered = BigDecimal(cashTendered)
                                if (tendered >= total) {
                                    onConfirm(tendered)
                                } else {
                                    hasError = true
                                }
                            } catch (e: NumberFormatException) {
                                hasError = true
                            }
                        },
                        modifier = Modifier.weight(1f),
                        enabled = cashTendered.isNotEmpty() && change >= BigDecimal.ZERO
                    ) {
                        Text("Finalize Order")
                    }
                }
            }
        }
    }
}
