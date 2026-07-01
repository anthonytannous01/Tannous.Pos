package com.tannous.pos.feature.reports

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.model.BranchDto
import com.tannous.pos.core.data.model.DemandForecastDto
import com.tannous.pos.core.data.model.HourlySalesDto
import com.tannous.pos.core.data.model.SalesSummaryDto
import com.tannous.pos.core.ui.LocalIsArabic
import java.math.BigDecimal

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DashboardScreen(
    onNavigateBack: () -> Unit,
    onNavigateToForecast: () -> Unit = {},
    onNavigateToSupplierIntelligence: () -> Unit = {},
    viewModel: DashboardViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val isArabic = LocalIsArabic.current
    val snackbarHostState = remember { SnackbarHostState() }

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
                title = { Text(if (isArabic) "لوحة المبيعات" else "Sales Dashboard") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = if (isArabic) "رجوع" else "Back")
                    }
                },
                actions = {
                    IconButton(onClick = { viewModel.refresh() }) {
                        Icon(Icons.Default.Refresh, contentDescription = if (isArabic) "تحديث" else "Refresh")
                    }
                }
            )
        }
    ) { padding ->
        Column(Modifier.padding(padding)) {
            // Branch selector — only shown when there are 2+ branches
            if (uiState.branches.size > 1) {
                BranchSelectorRow(
                    branches = uiState.branches,
                    selectedBranch = uiState.selectedBranch,
                    onSelect = { viewModel.selectBranch(it) }
                )
            }

            when {
                uiState.isLoading -> Box(
                    Modifier.fillMaxSize(),
                    contentAlignment = Alignment.Center
                ) { CircularProgressIndicator() }

                uiState.summary == null -> Box(
                    Modifier.fillMaxSize(),
                    contentAlignment = Alignment.Center
                ) {
                    Column(
                        horizontalAlignment = Alignment.CenterHorizontally,
                        verticalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        Text(if (isArabic) "لا توجد بيانات بعد" else "No data yet", style = MaterialTheme.typography.titleMedium)
                        Text(if (isArabic) "لا توجد طلبات مدفوعة اليوم." else "No paid orders today.",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant)
                    }
                }

                else -> DashboardContent(
                    summary = uiState.summary!!,
                    forecast = uiState.forecast,
                    isForecastLoading = uiState.isForecastLoading,
                    onSeeFullForecast = onNavigateToForecast,
                    onNavigateToSupplierIntelligence = onNavigateToSupplierIntelligence,
                    modifier = Modifier.fillMaxSize()
                )
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun BranchSelectorRow(
    branches: List<BranchDto>,
    selectedBranch: BranchDto?,
    onSelect: (BranchDto?) -> Unit
) {
    val isArabic = LocalIsArabic.current
    LazyRow(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 12.dp, vertical = 6.dp),
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        // "All" chip
        item {
            FilterChip(
                selected = selectedBranch == null,
                onClick = { onSelect(null) },
                label = { Text(if (isArabic) "جميع الفروع" else "All Branches") }
            )
        }
        items(branches) { branch ->
            FilterChip(
                selected = selectedBranch?.id == branch.id,
                onClick = { onSelect(branch) },
                label = { Text(branch.name) }
            )
        }
    }
}

@Composable
private fun DashboardContent(
    summary: SalesSummaryDto,
    forecast: DemandForecastDto?,
    isForecastLoading: Boolean,
    onSeeFullForecast: () -> Unit,
    onNavigateToSupplierIntelligence: () -> Unit = {},
    modifier: Modifier = Modifier
) {
    val isArabic = LocalIsArabic.current
    LazyColumn(
        modifier = modifier
            .fillMaxSize()
            .padding(horizontal = 12.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
        contentPadding = PaddingValues(vertical = 12.dp)
    ) {
        // ── Core KPI cards ───────────────────────────────────────────────────
        item {
            Text(if (isArabic) "مبيعات اليوم" else "Today's Sales",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold)
        }
        item {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                KpiCard(if (isArabic) "صافي المبيعات" else "Net Sales",
                    "$${summary.netSales.toPlainString()}",
                    MaterialTheme.colorScheme.primaryContainer, Modifier.weight(1f))
                KpiCard(if (isArabic) "الطلبات" else "Orders",
                    summary.ordersCount.toString(),
                    MaterialTheme.colorScheme.secondaryContainer, Modifier.weight(1f))
            }
        }
        item {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                KpiCard(if (isArabic) "متوسط الفاتورة" else "Avg Ticket",
                    "$${summary.avgTicket.toPlainString()}",
                    MaterialTheme.colorScheme.tertiaryContainer, Modifier.weight(1f))
                KpiCard(if (isArabic) "نسبة الإلغاء" else "Void Rate",
                    "${summary.voidRate}%",
                    if (summary.voidRate > BigDecimal("5"))
                        MaterialTheme.colorScheme.errorContainer
                    else
                        MaterialTheme.colorScheme.surfaceVariant,
                    Modifier.weight(1f))
            }
        }

        // ── VAT + stamp duty ─────────────────────────────────────────────────
        if (summary.taxCollected > BigDecimal.ZERO || summary.stampDutyCollected > BigDecimal.ZERO) {
            item {
                Text(if (isArabic) "الضرائب والرسوم" else "Tax & Duties",
                    style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
            item {
                Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    if (summary.taxCollected > BigDecimal.ZERO)
                        KpiCard(if (isArabic) "ضريبة القيمة المضافة" else "VAT Collected",
                            "$${summary.taxCollected.toPlainString()}",
                            MaterialTheme.colorScheme.surfaceVariant, Modifier.weight(1f))
                    if (summary.stampDutyCollected > BigDecimal.ZERO)
                        KpiCard(if (isArabic) "رسوم الطابع" else "Stamp Duty",
                            "$${summary.stampDutyCollected.toPlainString()}",
                            MaterialTheme.colorScheme.surfaceVariant, Modifier.weight(1f))
                }
            }
        }

        // ── Order type split ─────────────────────────────────────────────────
        item {
            Text(if (isArabic) "أنواع الطلبات" else "Order Types",
                style = MaterialTheme.typography.titleSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant)
        }
        item {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                KpiCard(if (isArabic) "داخل المطعم" else "Dine-In",
                    summary.dineInCount.toString(),
                    MaterialTheme.colorScheme.surfaceVariant, Modifier.weight(1f))
                KpiCard(if (isArabic) "للمنزل" else "Takeaway",
                    summary.takeawayCount.toString(),
                    MaterialTheme.colorScheme.surfaceVariant, Modifier.weight(1f))
                KpiCard(if (isArabic) "توصيل" else "Delivery",
                    summary.deliveryCount.toString(),
                    MaterialTheme.colorScheme.surfaceVariant, Modifier.weight(1f))
            }
        }

        // ── Hourly sparkline ─────────────────────────────────────────────────
        if (summary.hourlySales.isNotEmpty()) {
            item {
                Text(if (isArabic) "المبيعات بالساعة" else "Sales by Hour",
                    style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
            item { HourlyChart(summary.hourlySales) }
        }

        // ── Smart Suggestions (demand forecast) ──────────────────────────────
        if (forecast != null || isForecastLoading) {
            item {
                SmartSuggestionsCard(
                    forecast = forecast,
                    isLoading = isForecastLoading,
                    onSeeFullForecast = onSeeFullForecast
                )
            }
        }

        // ── Smart Ordering (supplier intelligence) ───────────────────────────
        item {
            SmartOrderingCard(onNavigate = onNavigateToSupplierIntelligence)
        }

        // ── Payment methods ──────────────────────────────────────────────────
        if (summary.paymentMethods.isNotEmpty()) {
            item {
                Text(if (isArabic) "طرق الدفع" else "Payment Methods",
                    style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
            items(summary.paymentMethods) { pm ->
                Card(Modifier.fillMaxWidth()) {
                    Row(
                        Modifier.fillMaxWidth().padding(12.dp),
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Column {
                            Text("${pm.method} (${pm.currency})",
                                style = MaterialTheme.typography.bodyMedium,
                                fontWeight = FontWeight.Medium)
                            Text(if (isArabic) "${pm.count} معاملة" else "${pm.count} transaction(s)",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant)
                        }
                        Text("$${pm.amount.toPlainString()}",
                            style = MaterialTheme.typography.bodyLarge,
                            fontWeight = FontWeight.Bold,
                            color = MaterialTheme.colorScheme.primary)
                    }
                }
            }
        }

        // ── Top items ────────────────────────────────────────────────────────
        if (summary.topItems.isNotEmpty()) {
            item {
                Text(if (isArabic) "أعلى العناصر" else "Top Items",
                    style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
            items(summary.topItems.take(8)) { topItem ->
                Card(Modifier.fillMaxWidth()) {
                    Row(
                        Modifier.fillMaxWidth().padding(12.dp),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column(Modifier.weight(1f)) {
                            Text(topItem.name, style = MaterialTheme.typography.bodyMedium,
                                fontWeight = FontWeight.Medium)
                            Text("${if (isArabic) "الكمية:" else "Qty:"} ${topItem.qty}",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant)
                        }
                        Text("$${topItem.sales.toPlainString()}",
                            style = MaterialTheme.typography.bodyLarge,
                            fontWeight = FontWeight.Bold)
                    }
                }
            }
        }
    }
}

@Composable
private fun KpiCard(label: String, value: String, color: Color, modifier: Modifier = Modifier) {
    Card(
        modifier = modifier,
        colors = CardDefaults.cardColors(containerColor = color)
    ) {
        Column(
            modifier = Modifier.padding(12.dp),
            verticalArrangement = Arrangement.spacedBy(4.dp)
        ) {
            Text(label, style = MaterialTheme.typography.labelSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant)
            Text(value, style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Bold)
        }
    }
}

@Composable
private fun SmartSuggestionsCard(
    forecast: DemandForecastDto?,
    isLoading: Boolean,
    onSeeFullForecast: () -> Unit
) {
    val isArabic = LocalIsArabic.current

    // Loading placeholder — same height as the populated card.
    if (forecast == null) {
        if (isLoading) {
            Card(Modifier.fillMaxWidth()) {
                Box(
                    Modifier.fillMaxWidth().height(220.dp),
                    contentAlignment = Alignment.Center
                ) { CircularProgressIndicator() }
            }
        }
        return
    }

    // Not enough history — small informational card, no numbers.
    if (forecast.insufficientDataMessage != null) {
        Card(
            modifier = Modifier.fillMaxWidth(),
            colors = CardDefaults.cardColors(
                containerColor = MaterialTheme.colorScheme.surfaceVariant)
        ) {
            Row(
                Modifier.padding(12.dp),
                horizontalArrangement = Arrangement.spacedBy(8.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text("\uD83D\uDCA1", style = MaterialTheme.typography.titleLarge)
                Text(
                    forecast.insufficientDataMessage!!,
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        }
        return
    }

    val peak = forecast.timeBlocks.firstOrNull { it.isPeakBlock }

    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.secondaryContainer)
    ) {
        Column(
            Modifier.fillMaxWidth().padding(12.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            Text(
                "\uD83D\uDD2E " + (if (isArabic) "اقتراحات ذكية" else "Smart Suggestions") +
                    " — ${forecast.dayOfWeekName}",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold
            )

            Row(
                horizontalArrangement = Arrangement.spacedBy(6.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                ConfidenceDots(forecast.confidence)
                Text(
                    "${forecast.confidence}  (${forecast.weeksOfDataUsed} " +
                        (if (isArabic) "أسابيع من البيانات" else "weeks of data") + ")",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            Text(
                "~${forecast.estimatedOrders} " +
                    (if (isArabic) "طلبات" else "orders") +
                    "  |  ~$${forecast.estimatedRevenue.toPlainString()} " +
                    (if (isArabic) "متوقع" else "estimated"),
                style = MaterialTheme.typography.titleSmall,
                fontWeight = FontWeight.SemiBold
            )

            if (peak != null) {
                Text(
                    (if (isArabic) "وقت الذروة: " else "Peak time: ") +
                        "${peak.label} (~${peak.estimatedOrders} " +
                        (if (isArabic) "طلبات" else "orders") + ")",
                    style = MaterialTheme.typography.bodyMedium
                )
            }

            if (forecast.topItems.isNotEmpty()) {
                Text(
                    if (isArabic) "أصناف للتحضير:" else "Top items to prep:",
                    style = MaterialTheme.typography.labelMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                forecast.topItems.take(3).forEach { item ->
                    val name = if (isArabic)
                        item.nameAr?.takeIf { it.isNotBlank() } ?: item.name
                    else item.name
                    Text(
                        "• $name ×${item.estimatedQty.stripTrailingZeros().toPlainString()}",
                        style = MaterialTheme.typography.bodySmall
                    )
                }
            }

            if (forecast.ingredientDemands.isNotEmpty()) {
                Text(
                    if (isArabic) "مكونات للتخزين:" else "Ingredients to stock:",
                    style = MaterialTheme.typography.labelMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                forecast.ingredientDemands.take(3).forEach { ing ->
                    val name = if (isArabic)
                        ing.nameAr?.takeIf { it.isNotBlank() } ?: ing.name
                    else ing.name
                    Text(
                        "• $name  ${ing.estimatedQty.stripTrailingZeros().toPlainString()} ${ing.unit}",
                        style = MaterialTheme.typography.bodySmall
                    )
                }
            }

            TextButton(
                onClick = onSeeFullForecast,
                modifier = Modifier.align(Alignment.End)
            ) {
                Text(if (isArabic) "عرض التوقعات الكاملة ›" else "See full forecast ›")
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun SmartOrderingCard(onNavigate: () -> Unit) {
    val isArabic = LocalIsArabic.current
    Card(
        modifier = Modifier.fillMaxWidth(),
        onClick = onNavigate,
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.tertiaryContainer
        )
    ) {
        Row(
            Modifier.fillMaxWidth().padding(12.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(Modifier.weight(1f)) {
                Text(
                    "\uD83D\uDCE6 " + (if (isArabic) "طلب ذكي" else "Smart Ordering"),
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold
                )
                Text(
                    if (isArabic) "اقتراحات شراء مبنية على الطلب المتوقع"
                    else "Demand-driven purchase order suggestions",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
            Text("\u2192", style = MaterialTheme.typography.titleLarge)
        }
    }
}

/** 3-level confidence indicator: Low = 1 filled dot, Medium = 2, High = 3 (out of 3). */
@Composable
private fun ConfidenceDots(confidence: String) {
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

@Composable
private fun HourlyChart(hours: List<HourlySalesDto>) {
    val maxSales = hours.maxOfOrNull { it.sales } ?: BigDecimal.ONE
    Card(Modifier.fillMaxWidth()) {
        LazyRow(
            modifier = Modifier.padding(12.dp),
            horizontalArrangement = Arrangement.spacedBy(4.dp)
        ) {
            items(hours) { h ->
                val heightFraction = if (maxSales > BigDecimal.ZERO)
                    (h.sales.toFloat() / maxSales.toFloat()).coerceIn(0.05f, 1f)
                else 0.05f
                Column(
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.Bottom,
                    modifier = Modifier.height(80.dp)
                ) {
                    Box(
                        Modifier
                            .width(20.dp)
                            .fillMaxHeight(heightFraction)
                            .background(
                                MaterialTheme.colorScheme.primary,
                                MaterialTheme.shapes.small
                            )
                    )
                    Spacer(Modifier.height(2.dp))
                    Text(
                        "${h.hour}h",
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }
        }
    }
}
