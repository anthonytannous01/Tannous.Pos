package com.tannous.pos.feature.customers

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Send
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.model.TopCustomerDto
import com.tannous.pos.core.ui.LocalIsArabic

private const val SEGMENT_VIP = 0
private const val SEGMENT_ACTIVE = 1
private const val SEGMENT_AT_RISK = 2
private const val SEGMENT_LAPSED = 3
private const val SEGMENT_NEW = 4

private val ALL_SEGMENTS = listOf(
    SEGMENT_VIP, SEGMENT_ACTIVE, SEGMENT_AT_RISK, SEGMENT_LAPSED, SEGMENT_NEW
)

private fun segmentLabel(segment: Int, isArabic: Boolean): String = when (segment) {
    SEGMENT_VIP -> if (isArabic) "عميل مميز" else "VIP"
    SEGMENT_ACTIVE -> if (isArabic) "نشط" else "Active"
    SEGMENT_AT_RISK -> if (isArabic) "في خطر" else "At Risk"
    SEGMENT_LAPSED -> if (isArabic) "غائب" else "Lapsed"
    else -> if (isArabic) "جديد" else "New"
}

private fun segmentColor(segment: Int): Color = when (segment) {
    SEGMENT_VIP -> Color(0xFFFFC107)    // gold
    SEGMENT_ACTIVE -> Color(0xFF4CAF50) // green
    SEGMENT_AT_RISK -> Color(0xFFFF9800)// amber
    SEGMENT_LAPSED -> Color(0xFFF44336) // red
    else -> Color(0xFF2196F3)           // blue
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun LoyaltyCrmScreen(
    onNavigateBack: () -> Unit,
    viewModel: LoyaltyCrmViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val isArabic = LocalIsArabic.current
    var selectedTab by remember { mutableStateOf(0) }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(if (isArabic) "ولاء العملاء" else "Loyalty CRM") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = "Back")
                    }
                }
            )
        }
    ) { padding ->
        Column(modifier = Modifier.fillMaxSize().padding(padding)) {
            TabRow(selectedTabIndex = selectedTab) {
                Tab(
                    selected = selectedTab == 0,
                    onClick = { selectedTab = 0 },
                    text = { Text(if (isArabic) "تحليلات" else "Analytics") }
                )
                Tab(
                    selected = selectedTab == 1,
                    onClick = { selectedTab = 1 },
                    text = { Text(if (isArabic) "الحملات" else "Campaigns") }
                )
            }

            when (selectedTab) {
                0 -> AnalyticsTab(uiState, isArabic, onRetry = viewModel::loadAnalytics)
                else -> CampaignsTab(
                    uiState = uiState,
                    isArabic = isArabic,
                    onSend = viewModel::sendCampaign,
                    onClearResult = viewModel::clearSendResult
                )
            }
        }
    }
}

@Composable
private fun AnalyticsTab(
    uiState: LoyaltyCrmUiState,
    isArabic: Boolean,
    onRetry: () -> Unit
) {
    when {
        uiState.isLoadingAnalytics -> {
            Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                CircularProgressIndicator()
            }
        }
        uiState.analyticsError != null -> {
            Box(Modifier.fillMaxSize().padding(24.dp), contentAlignment = Alignment.Center) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Text(uiState.analyticsError, color = MaterialTheme.colorScheme.error)
                    Spacer(Modifier.height(12.dp))
                    Button(onClick = onRetry) { Text(if (isArabic) "إعادة المحاولة" else "Retry") }
                }
            }
        }
        else -> {
            val a = uiState.analytics
            LazyColumn(
                modifier = Modifier.fillMaxSize(),
                contentPadding = PaddingValues(16.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                item {
                    Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                        SummaryCard(
                            label = if (isArabic) "إجمالي العملاء" else "Total Customers",
                            value = (a?.totalCustomers ?: 0).toString(),
                            modifier = Modifier.weight(1f)
                        )
                        SummaryCard(
                            label = if (isArabic) "نشط (30 يوم)" else "Active (30d)",
                            value = (a?.activeLast30Days ?: 0).toString(),
                            accent = segmentColor(SEGMENT_ACTIVE),
                            modifier = Modifier.weight(1f)
                        )
                    }
                }
                item {
                    Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                        SummaryCard(
                            label = if (isArabic) "في خطر" else "At-Risk",
                            value = (a?.atRiskCount ?: 0).toString(),
                            accent = segmentColor(SEGMENT_AT_RISK),
                            modifier = Modifier.weight(1f)
                        )
                        SummaryCard(
                            label = if (isArabic) "غائب" else "Lapsed",
                            value = (a?.lapsedCount ?: 0).toString(),
                            accent = segmentColor(SEGMENT_LAPSED),
                            modifier = Modifier.weight(1f)
                        )
                        SummaryCard(
                            label = if (isArabic) "مميز" else "VIP",
                            value = (a?.vipCount ?: 0).toString(),
                            accent = segmentColor(SEGMENT_VIP),
                            modifier = Modifier.weight(1f)
                        )
                    }
                }
                item {
                    Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                        SummaryCard(
                            label = if (isArabic) "متوسط قيمة الطلب" else "Avg Order Value",
                            value = (a?.averageOrderValue?.toPlainString() ?: "0"),
                            modifier = Modifier.weight(1f)
                        )
                        SummaryCard(
                            label = if (isArabic) "متوسط النقاط" else "Avg Points",
                            value = (a?.averagePointBalance?.toPlainString() ?: "0"),
                            modifier = Modifier.weight(1f)
                        )
                    }
                }
                item {
                    Text(
                        text = if (isArabic) "أفضل 10 عملاء" else "Top 10 Customers",
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold
                    )
                }
                val top = a?.topCustomers ?: emptyList()
                if (top.isEmpty()) {
                    item {
                        Text(
                            text = if (isArabic) "لا يوجد عملاء بعد" else "No customers yet",
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                } else {
                    items(top, key = { it.customerId }) { customer ->
                        TopCustomerRow(customer, isArabic)
                    }
                }
            }
        }
    }
}

@Composable
private fun SummaryCard(
    label: String,
    value: String,
    modifier: Modifier = Modifier,
    accent: Color? = null
) {
    Card(modifier = modifier) {
        Column(Modifier.padding(12.dp)) {
            Text(
                text = value,
                style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Bold,
                color = accent ?: MaterialTheme.colorScheme.onSurface
            )
            Text(
                text = label,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
    }
}

@Composable
private fun TopCustomerRow(customer: TopCustomerDto, isArabic: Boolean) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier.fillMaxWidth().padding(12.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(Modifier.weight(1f)) {
                Text(
                    text = customer.name,
                    style = MaterialTheme.typography.bodyLarge,
                    fontWeight = FontWeight.Medium
                )
                Text(
                    text = customer.phone ?: (if (isArabic) "لا يوجد هاتف" else "No phone"),
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Text(
                    text = (if (isArabic) "النقاط: " else "Points: ") + customer.lifetimePointsEarned,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
            SegmentChip(customer.segment, isArabic)
        }
    }
}

@Composable
private fun SegmentChip(segment: Int, isArabic: Boolean) {
    val color = segmentColor(segment)
    Surface(
        color = color.copy(alpha = 0.18f),
        contentColor = color,
        shape = MaterialTheme.shapes.small
    ) {
        Text(
            text = segmentLabel(segment, isArabic),
            style = MaterialTheme.typography.labelMedium,
            fontWeight = FontWeight.Bold,
            modifier = Modifier.padding(horizontal = 10.dp, vertical = 4.dp)
        )
    }
}

@Composable
private fun CampaignsTab(
    uiState: LoyaltyCrmUiState,
    isArabic: Boolean,
    onSend: (String, String, Int) -> Unit,
    onClearResult: () -> Unit
) {
    var showDialog by remember { mutableStateOf(false) }

    Column(
        modifier = Modifier.fillMaxSize().padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Button(
            onClick = { showDialog = true },
            modifier = Modifier.fillMaxWidth()
        ) {
            Icon(Icons.Default.Send, contentDescription = null)
            Spacer(Modifier.width(8.dp))
            Text(if (isArabic) "حملة جديدة" else "New Campaign")
        }

        uiState.lastCampaign?.let { campaign ->
            val statusText = when (campaign.status) {
                2 -> if (isArabic) "اكتملت" else "Completed"
                3 -> if (isArabic) "فشلت" else "Failed"
                1 -> if (isArabic) "جارٍ الإرسال" else "Sending"
                else -> if (isArabic) "معلّقة" else "Pending"
            }
            Card(modifier = Modifier.fillMaxWidth()) {
                Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                    Text(
                        text = campaign.name,
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold
                    )
                    Text(
                        text = (if (isArabic) "الحالة: " else "Status: ") + statusText,
                        style = MaterialTheme.typography.bodyMedium
                    )
                    Text(
                        text = (if (isArabic) "تم الإرسال: " else "Sent: ") +
                            "${campaign.sentCount} / ${campaign.recipientCount}",
                        style = MaterialTheme.typography.bodyMedium
                    )
                    campaign.sentAt?.let {
                        Text(
                            text = (if (isArabic) "الوقت: " else "At: ") + it,
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                    campaign.errorMessage?.let {
                        Text(text = it, color = MaterialTheme.colorScheme.error,
                            style = MaterialTheme.typography.bodySmall)
                    }
                    TextButton(onClick = onClearResult) {
                        Text(if (isArabic) "إغلاق" else "Dismiss")
                    }
                }
            }
        }

        uiState.sendError?.let { error ->
            Text(text = error, color = MaterialTheme.colorScheme.error)
        }
    }

    if (showDialog) {
        NewCampaignDialog(
            uiState = uiState,
            isArabic = isArabic,
            onDismiss = { showDialog = false },
            onSend = { name, message, segment ->
                onSend(name, message, segment)
                showDialog = false
            }
        )
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun NewCampaignDialog(
    uiState: LoyaltyCrmUiState,
    isArabic: Boolean,
    onDismiss: () -> Unit,
    onSend: (String, String, Int) -> Unit
) {
    var name by remember { mutableStateOf("") }
    var message by remember { mutableStateOf("") }
    var segment by remember { mutableStateOf(SEGMENT_VIP) }
    var segmentMenuExpanded by remember { mutableStateOf(false) }

    val previewCount = when (segment) {
        SEGMENT_VIP -> uiState.analytics?.vipCount ?: 0
        SEGMENT_ACTIVE -> uiState.analytics?.activeLast30Days ?: 0
        SEGMENT_AT_RISK -> uiState.analytics?.atRiskCount ?: 0
        SEGMENT_LAPSED -> uiState.analytics?.lapsedCount ?: 0
        else -> uiState.analytics?.newCount ?: 0
    }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(if (isArabic) "حملة جديدة" else "New Campaign") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                OutlinedTextField(
                    value = name,
                    onValueChange = { if (it.length <= 100) name = it },
                    label = { Text(if (isArabic) "الاسم" else "Name") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )

                ExposedDropdownMenuBox(
                    expanded = segmentMenuExpanded,
                    onExpandedChange = { segmentMenuExpanded = !segmentMenuExpanded }
                ) {
                    OutlinedTextField(
                        value = segmentLabel(segment, isArabic),
                        onValueChange = {},
                        readOnly = true,
                        label = { Text(if (isArabic) "الفئة المستهدفة" else "Target Segment") },
                        trailingIcon = {
                            ExposedDropdownMenuDefaults.TrailingIcon(expanded = segmentMenuExpanded)
                        },
                        modifier = Modifier.menuAnchor().fillMaxWidth()
                    )
                    ExposedDropdownMenu(
                        expanded = segmentMenuExpanded,
                        onDismissRequest = { segmentMenuExpanded = false }
                    ) {
                        ALL_SEGMENTS.forEach { seg ->
                            DropdownMenuItem(
                                text = { Text(segmentLabel(seg, isArabic)) },
                                onClick = {
                                    segment = seg
                                    segmentMenuExpanded = false
                                }
                            )
                        }
                    }
                }

                Text(
                    text = (if (isArabic) "المستلمون المتوقعون: " else "Preview recipients: ") + previewCount,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )

                OutlinedTextField(
                    value = message,
                    onValueChange = { if (it.length <= 500) message = it },
                    label = { Text(if (isArabic) "الرسالة" else "Message") },
                    minLines = 3,
                    modifier = Modifier.fillMaxWidth()
                )
                Text(
                    text = "${message.length} / 500",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.align(Alignment.End)
                )
            }
        },
        confirmButton = {
            TextButton(
                onClick = { onSend(name, message, segment) },
                enabled = name.isNotBlank() && message.isNotBlank() && !uiState.isSending
            ) {
                if (uiState.isSending) {
                    CircularProgressIndicator(modifier = Modifier.size(16.dp))
                } else {
                    Text(if (isArabic) "إرسال عبر واتساب" else "Send via WhatsApp")
                }
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) {
                Text(if (isArabic) "إلغاء" else "Cancel")
            }
        }
    )
}
