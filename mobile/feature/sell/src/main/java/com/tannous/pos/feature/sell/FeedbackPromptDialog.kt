package com.tannous.pos.feature.sell

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Star
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import com.tannous.pos.core.ui.LocalIsArabic

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun FeedbackPromptDialog(
    orderId: String?,
    orderNumber: String?,
    branchId: String?,
    onDismiss: () -> Unit,
    viewModel: FeedbackViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    val isArabic = LocalIsArabic.current

    // Auto-dismiss after successful submission
    LaunchedEffect(uiState.submitted) {
        if (uiState.submitted) {
            kotlinx.coroutines.delay(1200)
            onDismiss()
        }
    }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = {
            Text(
                if (uiState.submitted) (if (isArabic) "شكرًا! 🎉" else "Thank you! 🎉")
                else (if (isArabic) "كيف كانت تجربتك؟" else "How was your experience?")
            )
        },
        text = {
            if (uiState.submitted) {
                Text(if (isArabic) "تم تسجيل ملاحظاتك." else "Your feedback has been recorded.")
            } else {
                Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                    // Star rating row
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.Center
                    ) {
                        (1..5).forEach { star ->
                            Icon(
                                imageVector = Icons.Filled.Star,
                                contentDescription = "$star star",
                                tint = if (star <= uiState.selectedRating)
                                    MaterialTheme.colorScheme.primary
                                else
                                    MaterialTheme.colorScheme.surfaceVariant,
                                modifier = Modifier
                                    .size(40.dp)
                                    .clickable { viewModel.setRating(star) }
                                    .padding(4.dp)
                            )
                        }
                    }

                    // Category chips
                    val visibleCategories = feedbackCategories.take(4) // General/Food/Service/Complaint
                    val categoryIndices = listOf(0, 1, 2, 5)
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(6.dp)
                    ) {
                        categoryIndices.zip(visibleCategories + listOf("Complaint")).forEach { (idx, label) ->
                            FilterChip(
                                selected = uiState.selectedCategory == idx,
                                onClick = { viewModel.setCategory(idx) },
                                label = { Text(label, style = MaterialTheme.typography.labelSmall) },
                                modifier = Modifier.weight(1f)
                            )
                        }
                    }

                    // Optional comment
                    OutlinedTextField(
                        value = uiState.comment,
                        onValueChange = { if (it.length <= 500) viewModel.setComment(it) },
                        label = { Text(if (isArabic) "تعليق (اختياري)" else "Comment (optional)") },
                        modifier = Modifier.fillMaxWidth(),
                        minLines = 2,
                        supportingText = { Text("${uiState.comment.length}/500") }
                    )

                    uiState.error?.let {
                        Text(it, color = MaterialTheme.colorScheme.error,
                            style = MaterialTheme.typography.bodySmall)
                    }
                }
            }
        },
        confirmButton = {
            if (!uiState.submitted) {
                TextButton(
                    onClick = { viewModel.submit(orderId, orderNumber, branchId) },
                    enabled = uiState.selectedRating > 0 && !uiState.isSubmitting
                ) {
                    if (uiState.isSubmitting) {
                        CircularProgressIndicator(modifier = Modifier.size(16.dp), strokeWidth = 2.dp)
                    } else {
                        Text(if (isArabic) "إرسال" else "Submit")
                    }
                }
            }
        },
        dismissButton = {
            if (!uiState.submitted) {
                TextButton(onClick = onDismiss) { Text(if (isArabic) "تخطي" else "Skip") }
            }
        }
    )
}
