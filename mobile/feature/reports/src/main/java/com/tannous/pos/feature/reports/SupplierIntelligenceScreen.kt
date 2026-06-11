package com.tannous.pos.feature.reports

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import kotlinx.coroutines.launch
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.model.LowStockAlertDto
import com.tannous.pos.core.data.model.OrderLineSuggestionDto
import com.tannous.pos.core.data.model.SupplierIntelligenceDto
import com.tannous.pos.core.data.model.SupplierOrderSuggestionDto
import com.tannous.pos.core.ui.LocalIsArabic

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SupplierIntelligenceScreen(
    onNavigateBack: () -> Unit,
    viewModel: SupplierIntelligenceViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val isArabic = LocalIsArabic.current
    val snackbarHostState = remember { SnackbarHostState() }
    val scope = rememberCoroutineScope()

    LaunchedEffect(uiState.error) {
        uiState.error?.let {
            snackbarHostState.showSnackbar(it, duration = SnackbarDuration.Short)
            viewModel.clearError()
        }
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHostState) },
        topBar = {
            TopAppBar(
                title = { Text(if (isArabic) "طلب ذكي" else "Smart Ordering") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = "Back")
                    }
                },
                actions = {
                    IconButton(onClick = { viewModel.load() }) {
                        Icon(Icons.Default.Refresh, contentDescription = "Refresh")
                    }
                }
            )
        }
    ) { padding ->
        when {
            uiState.isLoading && uiState.data == null -> Box(
                Modifier.fillMaxSize().padding(padding),
                contentAlignment = Alignment.Center
            ) { CircularProgressIndicator() }

            uiState.data == null -> Box(
                Modifier.fillMaxSize().padding(padding),
                contentAlignment = Alignment.Center
            ) { Text(uiState.error ?: "No data") }

            else -> SupplierIntelligenceContent(
                data = uiState.data!!,
                isArabic = isArabic,
                creatingOrder = uiState.creatingOrder,
                onCreateOrder = { supplierId ->
                    viewModel.createOrderForSupplier(supplierId) { message ->
                        scope.launch { snackbarHostState.showSnackbar(message) }
                    }
                },
                modifier = Modifier.fillMaxSize().padding(padding)
            )
        }
    }
}

@Composable
private fun SupplierIntelligenceContent(
    data: SupplierIntelligenceDto,
    isArabic: Boolean,
    creatingOrder: String?,
    onCreateOrder: (String) -> Unit,
    modifier: Modifier = Modifier
) {
    val showZeroState = data.totalSuggestedLines == 0 && data.lowStockAlerts.isEmpty()

    LazyColumn(
        modifier = modifier.padding(horizontal = 12.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
        contentPadding = PaddingValues(vertical = 12.dp)
    ) {
        item {
            Row(
                Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    "\uD83D\uDCE6 " + (if (isArabic) "طلب ذكي — الـ 7 أيام القادمة"
                    else "Smart Ordering — Next 7 days"),
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold
                )
                Row(
                    horizontalArrangement = Arrangement.spacedBy(6.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    IntelligenceConfidenceDots(data.confidence)
                    Text(
                        data.confidence,
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }
        }

        if (showZeroState) {
            item {
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.primaryContainer
                    )
                ) {
                    Text(
                        if (isArabic) {
                            "\u2705 المخزون يبدو جيداً! لا حاجة لطلبات خلال الـ 7 أيام القادمة."
                        } else {
                            "\u2705 Stock looks good! No orders needed for the next 7 days."
                        },
                        modifier = Modifier.padding(24.dp),
                        style = MaterialTheme.typography.bodyLarge
                    )
                }
            }
            return@LazyColumn
        }

        if (data.lowStockAlerts.isNotEmpty()) {
            item {
                Text(
                    "\u26A0\uFE0F " + (if (isArabic) "مخزون منخفض" else "Low Stock Now"),
                    style = MaterialTheme.typography.titleSmall,
                    fontWeight = FontWeight.SemiBold,
                    color = MaterialTheme.colorScheme.error
                )
            }
            items(data.lowStockAlerts) { alert ->
                LowStockAlertCard(alert, isArabic)
            }
        }

        if (data.orderSuggestions.isNotEmpty()) {
            item {
                Text(
                    if (isArabic) "اقتراحات الطلب" else "Order Suggestions",
                    style = MaterialTheme.typography.titleSmall,
                    fontWeight = FontWeight.SemiBold
                )
            }
            items(data.orderSuggestions) { suggestion ->
                SupplierSuggestionCard(
                    suggestion = suggestion,
                    isArabic = isArabic,
                    isCreating = creatingOrder == suggestion.supplierId,
                    onCreateOrder = onCreateOrder
                )
            }
        }
    }
}

@Composable
private fun LowStockAlertCard(alert: LowStockAlertDto, isArabic: Boolean) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.errorContainer.copy(alpha = 0.5f)
        )
    ) {
        Column(Modifier.padding(12.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
            Text(alert.name, fontWeight = FontWeight.SemiBold)
            Text(
                "${if (isArabic) "الحالي" else "Current"}: ${alert.currentStock} ${alert.unit}  |  " +
                    "${if (isArabic) "الحد الأدنى" else "Min"}: ${alert.minimumStock} ${alert.unit}",
                style = MaterialTheme.typography.bodySmall
            )
            Text(
                "${if (isArabic) "النقص" else "Deficit"}: ${alert.deficit} ${alert.unit}",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.error,
                fontWeight = FontWeight.Medium
            )
            alert.supplierName?.let {
                Text(
                    "${if (isArabic) "المورد" else "Supplier"}: $it",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        }
    }
}

@Composable
private fun SupplierSuggestionCard(
    suggestion: SupplierOrderSuggestionDto,
    isArabic: Boolean,
    isCreating: Boolean,
    onCreateOrder: (String) -> Unit
) {
    val isUnassigned = suggestion.supplierId == null

    Card(modifier = Modifier.fillMaxWidth()) {
        Column(Modifier.padding(12.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
            Row(
                Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(suggestion.supplierName, fontWeight = FontWeight.Bold)
                Text(
                    "$${"%.2f".format(suggestion.totalEstimatedCost)}",
                    fontWeight = FontWeight.SemiBold,
                    color = MaterialTheme.colorScheme.primary
                )
            }

            if (isUnassigned) {
                Text(
                    if (isArabic) {
                        "\u26A0\uFE0F لم يُعيَّن مورد — عيِّن مورداً مفضلاً لإنشاء الطلبات تلقائياً"
                    } else {
                        "\u26A0\uFE0F No supplier assigned — assign a preferred supplier to create orders automatically"
                    },
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.tertiary
                )
            }

            suggestion.lines.forEach { line ->
                OrderLineRow(line, isArabic)
            }

            if (!isUnassigned) {
                val supplierId = suggestion.supplierId
                if (supplierId != null) {
                    Button(
                        onClick = { onCreateOrder(supplierId) },
                        enabled = !isCreating,
                        modifier = Modifier.align(Alignment.End)
                    ) {
                        if (isCreating) {
                            CircularProgressIndicator(
                                modifier = Modifier.size(16.dp),
                                strokeWidth = 2.dp
                            )
                            Spacer(Modifier.width(8.dp))
                        }
                        Text(if (isArabic) "إنشاء طلب شراء مسودة" else "Create Draft PO")
                    }
                }
            }
        }
    }
}

@Composable
private fun OrderLineRow(line: OrderLineSuggestionDto, isArabic: Boolean) {
    val containerColor = if (line.isLowStock) {
        Color(0xFFFFF3E0)
    } else {
        MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.4f)
    }

    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(containerColor = containerColor)
    ) {
        Column(Modifier.padding(10.dp), verticalArrangement = Arrangement.spacedBy(2.dp)) {
            Text(line.name, fontWeight = FontWeight.Medium)
            Text(
                "${if (isArabic) "الحالي" else "current"}: ${line.currentStock} ${line.unit}  |  " +
                    "${if (isArabic) "الحاجة" else "need"}: ${line.projectedUsage} ${line.unit}",
                style = MaterialTheme.typography.bodySmall
            )
            Text(
                "${if (isArabic) "اقتراح الطلب" else "suggest ordering"}: ${line.suggestedQty} ${line.unit}  " +
                    "($${"%.2f".format(line.estimatedCost)})",
                style = MaterialTheme.typography.bodySmall,
                fontWeight = FontWeight.Medium
            )
        }
    }
}

@Composable
private fun IntelligenceConfidenceDots(confidence: String) {
    val filled = when (confidence) {
        "High"   -> 3
        "Medium" -> 2
        else     -> 1
    }
    Text(
        buildString {
            repeat(filled) { append('●') }
            repeat(3 - filled) { append('○') }
        },
        style = MaterialTheme.typography.bodySmall,
        color = MaterialTheme.colorScheme.primary
    )
}
