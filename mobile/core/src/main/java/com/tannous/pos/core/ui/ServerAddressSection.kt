package com.tannous.pos.core.ui

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Card
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.Button
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp

/**
 * Lets someone point this tablet at a different server without rebuilding the app.
 *
 * Lives in core because it is shown in two places: Settings, and the login screen. The login
 * screen is the one that matters. A wrong address stops the app reaching the server, and if the
 * only way to correct it is a screen behind login, the setting is a trap rather than a fix.
 *
 * The compiled-in address comes from API_BASE_URL at build time. On a restaurant LAN that is a
 * DHCP lease, and when it moves every tablet stops at once. Before this existed the only fix was
 * rebuilding and reinstalling the APK, which is not something anyone can do during service.
 */
@Composable
fun ServerAddressSection(
    isArabic: Boolean,
    address: String,
    isOverridden: Boolean,
    defaultAddress: String,
    error: String?,
    onAddressChange: (String) -> Unit,
    onSave: () -> Unit,
    onReset: () -> Unit
) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(
            modifier = Modifier.fillMaxWidth().padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            Text(
                text = if (isArabic) "عنوان الخادم" else "Server address",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.SemiBold
            )

            Text(
                text = if (isArabic) {
                    "غيّره فقط إذا توقف الجهاز عن الاتصال بالخادم."
                } else {
                    "Only change this if the tablet has stopped reaching the server."
                },
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )

            OutlinedTextField(
                value = address,
                onValueChange = onAddressChange,
                label = { Text(if (isArabic) "العنوان" else "Address") },
                placeholder = { Text("192.168.10.231:7000") },
                singleLine = true,
                isError = error != null,
                supportingText = error?.let { { Text(it, color = MaterialTheme.colorScheme.error) } },
                modifier = Modifier.fillMaxWidth()
            )

            Text(
                text = if (isOverridden) {
                    if (isArabic) "مضبوط على هذا الجهاز. الأصلي: $defaultAddress"
                    else "Set on this tablet. Built-in address: $defaultAddress"
                } else {
                    if (isArabic) "يستخدم العنوان الأصلي للتطبيق."
                    else "Using the address this app was built with."
                },
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )

            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                Button(onClick = onSave) {
                    Text(if (isArabic) "حفظ" else "Save")
                }
                if (isOverridden) {
                    TextButton(onClick = onReset) {
                        Text(if (isArabic) "استعادة الأصلي" else "Reset to built-in")
                    }
                }
            }
        }
    }
}
