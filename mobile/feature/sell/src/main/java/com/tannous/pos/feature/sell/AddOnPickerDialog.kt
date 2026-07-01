package com.tannous.pos.feature.sell

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import com.tannous.pos.core.data.local.entity.AddOnEntity
import com.tannous.pos.core.data.local.entity.MenuItemEntity
import com.tannous.pos.core.ui.LocalIsArabic
import java.text.NumberFormat

@Composable
fun AddOnPickerDialog(
    menuItem: MenuItemEntity,
    availableAddOns: List<AddOnEntity>,
    currencyFormatter: NumberFormat,
    onConfirm: (List<CartAddOn>) -> Unit,
    onDismiss: () -> Unit
) {
    var selectedIds by remember { mutableStateOf(setOf<String>()) }
    val isArabic = LocalIsArabic.current

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
                    text = if (isArabic) menuItem.nameAr?.takeIf { it.isNotBlank() } ?: menuItem.name else menuItem.name,
                    style = MaterialTheme.typography.headlineSmall,
                    fontWeight = FontWeight.Bold
                )
                Text(
                    text = if (isArabic) "اختر الإضافات (اختياري)" else "Select add-ons (optional)",
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.padding(bottom = 12.dp)
                )

                if (availableAddOns.isEmpty()) {
                    Text(
                        text = if (isArabic) "لا توجد إضافات متاحة" else "No add-ons available",
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        modifier = Modifier.padding(vertical = 8.dp)
                    )
                } else {
                    LazyColumn(
                        modifier = Modifier
                            .fillMaxWidth()
                            .heightIn(max = 280.dp),
                        verticalArrangement = Arrangement.spacedBy(4.dp)
                    ) {
                        items(availableAddOns, key = { it.id }) { addOn ->
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Checkbox(
                                    checked = addOn.id in selectedIds,
                                    onCheckedChange = { checked ->
                                        selectedIds = if (checked) {
                                            selectedIds + addOn.id
                                        } else {
                                            selectedIds - addOn.id
                                        }
                                    }
                                )
                                Text(
                                    text = addOn.name,
                                    style = MaterialTheme.typography.bodyLarge,
                                    modifier = Modifier.weight(1f)
                                )
                                Text(
                                    text = currencyFormatter.format(addOn.price),
                                    style = MaterialTheme.typography.bodyMedium,
                                    fontWeight = FontWeight.SemiBold
                                )
                            }
                        }
                    }
                }

                Spacer(modifier = Modifier.height(16.dp))

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
                            val selected = availableAddOns
                                .filter { it.id in selectedIds }
                                .map { addOn ->
                                    CartAddOn(
                                        id = addOn.id,
                                        name = addOn.name,
                                        price = addOn.price.toDouble(),
                                        quantity = 1
                                    )
                                }
                            onConfirm(selected)
                        },
                        modifier = Modifier.weight(1f)
                    ) {
                        Text(if (isArabic) "أضف إلى السلة" else "Add to Cart")
                    }
                }
            }
        }
    }
}
