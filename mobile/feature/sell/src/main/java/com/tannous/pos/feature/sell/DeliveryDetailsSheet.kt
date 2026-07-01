package com.tannous.pos.feature.sell

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import java.math.BigDecimal

/**
 * Bottom sheet that collects delivery details (address, phone, fee, ETA, notes)
 * before the cashier proceeds to the payment step.
 *
 * [onConfirm] is called with the filled-in details when the user taps
 * "Continue to Payment". The caller is responsible for then showing the
 * payment dialog and calling ViewModel.finalizeOrder().
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DeliveryDetailsSheet(
    isArabic: Boolean,
    onConfirm: (PendingDeliveryDetails) -> Unit,
    onDismiss: () -> Unit
) {
    val sheetState = rememberModalBottomSheetState(skipPartiallyExpanded = true)

    var address by remember { mutableStateOf("") }
    var phone   by remember { mutableStateOf("") }
    var feeText by remember { mutableStateOf("") }
    var etaText by remember { mutableStateOf("") }
    var notes   by remember { mutableStateOf("") }

    ModalBottomSheet(
        onDismissRequest = onDismiss,
        sheetState = sheetState
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 16.dp)
                .padding(bottom = 32.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Text(
                text = if (isArabic) "تفاصيل التوصيل" else "Delivery Details",
                style = MaterialTheme.typography.titleLarge,
                modifier = Modifier.padding(vertical = 4.dp)
            )

            // Address — required
            OutlinedTextField(
                value = address,
                onValueChange = { address = it },
                label = { Text(if (isArabic) "عنوان التوصيل *" else "Delivery Address *") },
                placeholder = { Text(if (isArabic) "الشارع، المبنى، الطابق..." else "Street, building, floor...") },
                modifier = Modifier.fillMaxWidth(),
                minLines = 2,
                isError = address.isBlank(),
                supportingText = if (address.isBlank()) {
                    { Text(if (isArabic) "العنوان مطلوب" else "Address is required") }
                } else null
            )

            // Customer phone
            OutlinedTextField(
                value = phone,
                onValueChange = { phone = it },
                label = { Text(if (isArabic) "هاتف العميل" else "Customer Phone") },
                placeholder = { Text(if (isArabic) "اختياري" else "Optional") },
                modifier = Modifier.fillMaxWidth(),
                singleLine = true,
                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Phone)
            )

            // Fee + ETA side by side
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                OutlinedTextField(
                    value = feeText,
                    onValueChange = { if (it.matches(Regex("\\d*\\.?\\d*"))) feeText = it },
                    label = { Text(if (isArabic) "رسوم التوصيل" else "Delivery Fee") },
                    placeholder = { Text("0.00") },
                    modifier = Modifier.weight(1f),
                    singleLine = true,
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal)
                )
                OutlinedTextField(
                    value = etaText,
                    onValueChange = { if (it.all(Char::isDigit)) etaText = it },
                    label = { Text(if (isArabic) "الوقت (دقائق)" else "ETA (mins)") },
                    placeholder = { Text(if (isArabic) "اختياري" else "Optional") },
                    modifier = Modifier.weight(1f),
                    singleLine = true,
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number)
                )
            }

            // Notes / special instructions
            OutlinedTextField(
                value = notes,
                onValueChange = { notes = it },
                label = { Text(if (isArabic) "ملاحظات" else "Notes") },
                placeholder = { Text(if (isArabic) "تعليمات خاصة..." else "Special instructions...") },
                modifier = Modifier.fillMaxWidth(),
                minLines = 2
            )

            // Continue to payment
            Button(
                onClick = {
                    onConfirm(
                        PendingDeliveryDetails(
                            address          = address.trim(),
                            phone            = phone.trim().ifBlank { null },
                            fee              = feeText.toBigDecimalOrNull() ?: BigDecimal.ZERO,
                            estimatedMinutes = etaText.toIntOrNull(),
                            notes            = notes.trim().ifBlank { null }
                        )
                    )
                },
                enabled   = address.isNotBlank(),
                modifier  = Modifier.fillMaxWidth()
            ) {
                Text(if (isArabic) "متابعة إلى الدفع" else "Continue to Payment")
            }

            OutlinedButton(
                onClick  = onDismiss,
                modifier = Modifier.fillMaxWidth()
            ) {
                Text(if (isArabic) "إلغاء" else "Cancel")
            }
        }
    }
}
