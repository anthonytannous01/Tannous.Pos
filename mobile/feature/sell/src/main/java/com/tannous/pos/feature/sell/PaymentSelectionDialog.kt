package com.tannous.pos.feature.sell

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import com.tannous.pos.core.data.model.PaymentDto
import com.tannous.pos.core.util.currencyFormatterFor
import java.math.BigDecimal

enum class PaymentMethod {
    CASH, CARD, OTHER
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PaymentSelectionDialog(
    total: BigDecimal,
    currencyCode: String = "USD",
    onConfirm: (List<PaymentDto>) -> Unit,
    onDismiss: () -> Unit
) {
    var selectedMethod by remember { mutableStateOf<PaymentMethod?>(null) }
    var cashAmount by remember { mutableStateOf("") }
    var cardAmount by remember { mutableStateOf("") }
    var otherAmount by remember { mutableStateOf("") }
    var otherMethodName by remember { mutableStateOf("") }
    
    val currencyFormatter = remember(currencyCode) { currencyFormatterFor(currencyCode) }
    
    // Calculate amounts
    val cashAmountDecimal = try {
        if (cashAmount.isNotEmpty()) BigDecimal(cashAmount) else BigDecimal.ZERO
    } catch (e: NumberFormatException) {
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
    
    val totalPaid = cashAmountDecimal + cardAmountDecimal + otherAmountDecimal
    val remaining = total - totalPaid
    val change = if (cashAmountDecimal > BigDecimal.ZERO && totalPaid > total) {
        totalPaid - total
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
            ) {
                Text(
                    text = "Select Payment Method",
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
                }
                
                Spacer(modifier = Modifier.height(16.dp))
                
                // Payment method selection
                Text(
                    text = "Payment Method",
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
                        label = { Text("Cash") },
                        modifier = Modifier.weight(1f)
                    )
                    FilterChip(
                        selected = selectedMethod == PaymentMethod.CARD,
                        onClick = { selectedMethod = PaymentMethod.CARD },
                        label = { Text("Card") },
                        modifier = Modifier.weight(1f)
                    )
                    FilterChip(
                        selected = selectedMethod == PaymentMethod.OTHER,
                        onClick = { selectedMethod = PaymentMethod.OTHER },
                        label = { Text("Other") },
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
                            label = { Text("Cash Amount") },
                            modifier = Modifier.fillMaxWidth(),
                            singleLine = true
                        )
                        if (cashAmountDecimal > BigDecimal.ZERO && change > BigDecimal.ZERO) {
                            Spacer(modifier = Modifier.height(8.dp))
                            Text(
                                text = "Change: ${currencyFormatter.format(change)}",
                                style = MaterialTheme.typography.titleMedium,
                                color = MaterialTheme.colorScheme.primary,
                                fontWeight = FontWeight.Bold
                            )
                        }
                    }
                    PaymentMethod.CARD -> {
                        OutlinedTextField(
                            value = cardAmount,
                            onValueChange = { cardAmount = it },
                            label = { Text("Card Amount") },
                            modifier = Modifier.fillMaxWidth(),
                            singleLine = true
                        )
                    }
                    PaymentMethod.OTHER -> {
                        OutlinedTextField(
                            value = otherMethodName,
                            onValueChange = { otherMethodName = it },
                            label = { Text("Payment Method Name") },
                            modifier = Modifier.fillMaxWidth(),
                            singleLine = true
                        )
                        Spacer(modifier = Modifier.height(8.dp))
                        OutlinedTextField(
                            value = otherAmount,
                            onValueChange = { otherAmount = it },
                            label = { Text("Amount") },
                            modifier = Modifier.fillMaxWidth(),
                            singleLine = true
                        )
                    }
                    null -> {
                        Text(
                            text = "Please select a payment method",
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }
                
                // Remaining amount display
                if (totalPaid < total && (cashAmountDecimal > BigDecimal.ZERO || cardAmountDecimal > BigDecimal.ZERO || otherAmountDecimal > BigDecimal.ZERO)) {
                    Spacer(modifier = Modifier.height(16.dp))
                    Text(
                        text = "Remaining: ${currencyFormatter.format(remaining)}",
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
                        Text("Cancel")
                    }
                    
                    Button(
                        onClick = {
                            val payments = mutableListOf<PaymentDto>()
                            
                            if (cashAmountDecimal > BigDecimal.ZERO) {
                                payments.add(
                                    PaymentDto(
                                        paymentMethod = "CASH",
                                        amount = cashAmountDecimal,
                                        notes = if (change > BigDecimal.ZERO) "Change: ${currencyFormatter.format(change)}" else null
                                    )
                                )
                            }
                            
                            if (cardAmountDecimal > BigDecimal.ZERO) {
                                payments.add(
                                    PaymentDto(
                                        paymentMethod = "CARD",
                                        amount = cardAmountDecimal,
                                        transactionId = null,
                                        notes = null
                                    )
                                )
                            }
                            
                            if (otherAmountDecimal > BigDecimal.ZERO) {
                                payments.add(
                                    PaymentDto(
                                        paymentMethod = otherMethodName.ifEmpty { "OTHER" },
                                        amount = otherAmountDecimal,
                                        notes = null
                                    )
                                )
                            }
                            
                            if (payments.isNotEmpty() && totalPaid >= total) {
                                onConfirm(payments)
                            }
                        },
                        modifier = Modifier.weight(1f),
                        enabled = selectedMethod != null && totalPaid >= total && totalPaid > BigDecimal.ZERO
                    ) {
                        Text("Finalize Order")
                    }
                }
            }
        }
    }
}


