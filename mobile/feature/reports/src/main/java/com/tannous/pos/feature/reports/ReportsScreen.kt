package com.tannous.pos.feature.reports

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.KeyboardArrowLeft
import androidx.compose.material.icons.filled.KeyboardArrowRight
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.filled.Share
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.model.KdsHourlyDto
import com.tannous.pos.core.data.model.KdsItemPerformanceDto
import com.tannous.pos.core.data.model.KdsPerformanceDto
import com.tannous.pos.core.data.model.SectionHourlyDto
import com.tannous.pos.core.data.model.SectionSalesDto
import com.tannous.pos.core.data.model.SectionSalesReportDto
import com.tannous.pos.core.data.model.SectionTopItemDto
import com.tannous.pos.core.ui.LocalIsArabic
import com.tannous.pos.core.util.currencyFormatterFor
import java.text.NumberFormat
import java.time.LocalDate
import java.time.format.DateTimeFormatter
import java.time.format.TextStyle
import java.util.Locale

private const val KITCHEN_TAB = 2
private const val EXPORT_TAB = 3
private const val SECTIONS_TAB = 4

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ReportsScreen(
    onNavigateBack: () -> Unit,
    viewModel: ReportsViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val context = LocalContext.current
    val isArabic = LocalIsArabic.current
    val snackbarHostState = remember { SnackbarHostState() }
    val scrollState = rememberScrollState()
    val currencyFormatter = remember(uiState.currencyCode) {
        currencyFormatterFor(uiState.currencyCode)
    }

    LaunchedEffect(uiState.exportError) {
        uiState.exportError?.let { error ->
            snackbarHostState.showSnackbar(error)
            viewModel.clearExportError()
        }
    }

    LaunchedEffect(uiState.kdsPerformance.error) {
        uiState.kdsPerformance.error?.let { error ->
            snackbarHostState.showSnackbar(error)
            viewModel.clearKdsError()
        }
    }

    LaunchedEffect(uiState.sectionSales.error) {
        uiState.sectionSales.error?.let { error ->
            snackbarHostState.showSnackbar(error)
            viewModel.clearSectionSalesError()
        }
    }

    LaunchedEffect(uiState.selectedTab) {
        if (uiState.selectedTab == KITCHEN_TAB &&
            uiState.kdsPerformance.data == null &&
            !uiState.kdsPerformance.isLoading
        ) {
            viewModel.loadKdsPerformance()
        }
        if (uiState.selectedTab == SECTIONS_TAB &&
            uiState.sectionSales.data == null &&
            !uiState.sectionSales.isLoading
        ) {
            viewModel.loadSectionSales()
        }
    }

    val dateLabel = if (uiState.selectedDate == LocalDate.now()) {
        if (isArabic) "اليوم" else "Today"
    } else {
        uiState.selectedDate.format(DateTimeFormatter.ofPattern("MMM d, yyyy"))
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHostState) },
        topBar = {
            TopAppBar(
                title = { Text(if (isArabic) "التقارير" else "Reports") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = if (isArabic) "رجوع" else "Back")
                    }
                }
            )
        }
    ) { paddingValues ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
        ) {
            TabRow(selectedTabIndex = uiState.selectedTab) {
                Tab(selected = uiState.selectedTab == 0, onClick = { viewModel.selectTab(0) },
                    text = { Text(if (isArabic) "نهاية اليوم" else "End of Day") })
                Tab(selected = uiState.selectedTab == 1, onClick = { viewModel.selectTab(1) },
                    text = { Text("COGS") })
                Tab(selected = uiState.selectedTab == KITCHEN_TAB, onClick = { viewModel.selectTab(KITCHEN_TAB) },
                    text = { Text(if (isArabic) "المطبخ" else "Kitchen") })
                Tab(selected = uiState.selectedTab == EXPORT_TAB, onClick = { viewModel.selectTab(EXPORT_TAB) },
                    text = { Text(if (isArabic) "تصدير" else "Export") })
                Tab(selected = uiState.selectedTab == SECTIONS_TAB, onClick = { viewModel.selectTab(SECTIONS_TAB) },
                    text = { Text(if (isArabic) "الأقسام" else "Sections") })
            }

            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .verticalScroll(scrollState)
                    .padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                if (uiState.selectedTab == 0) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        IconButton(onClick = {
                            viewModel.selectDate(uiState.selectedDate.minusDays(1))
                        }) {
                            Icon(
                                Icons.Default.KeyboardArrowLeft,
                                contentDescription = if (isArabic) "اليوم السابق" else "Previous day"
                            )
                        }
                        Text(
                            text = dateLabel,
                            style = MaterialTheme.typography.titleMedium,
                            modifier = Modifier.weight(1f),
                            textAlign = TextAlign.Center
                        )
                        IconButton(
                            onClick = {
                                viewModel.selectDate(uiState.selectedDate.plusDays(1))
                            },
                            enabled = uiState.selectedDate.isBefore(LocalDate.now())
                        ) {
                            Icon(
                                Icons.Default.KeyboardArrowRight,
                                contentDescription = if (isArabic) "اليوم التالي" else "Next day"
                            )
                        }
                    }

                    when {
                        uiState.isLoading -> {
                            Box(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(32.dp),
                                contentAlignment = Alignment.Center
                            ) {
                                CircularProgressIndicator()
                            }
                        }
                        uiState.error != null -> {
                            Text(
                                text = uiState.error!!,
                                color = MaterialTheme.colorScheme.error,
                                modifier = Modifier.padding(8.dp)
                            )
                            Button(
                                onClick = { viewModel.loadReport() },
                                modifier = Modifier.align(Alignment.CenterHorizontally)
                            ) {
                                Text(if (isArabic) "إعادة المحاولة" else "Retry")
                            }
                        }
                        uiState.report != null -> {
                            val report = uiState.report!!
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                horizontalArrangement = Arrangement.spacedBy(8.dp)
                            ) {
                                SummaryCard(
                                    label = if (isArabic) "صافي المبيعات" else "Net Sales",
                                    value = currencyFormatter.format(report.netSales),
                                    modifier = Modifier.weight(1f)
                                )
                                SummaryCard(
                                    label = if (isArabic) "الطلبات" else "Orders",
                                    value = report.ordersCount.toString(),
                                    modifier = Modifier.weight(1f)
                                )
                                SummaryCard(
                                    label = if (isArabic) "متوسط الفاتورة" else "Avg Ticket",
                                    value = currencyFormatter.format(report.avgTicket),
                                    modifier = Modifier.weight(1f)
                                )
                            }
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                horizontalArrangement = Arrangement.spacedBy(8.dp)
                            ) {
                                SummaryCard(
                                    label = if (isArabic) "إيداعات النقد" else "Cash Drops",
                                    value = currencyFormatter.format(report.cashDrops),
                                    modifier = Modifier.weight(1f)
                                )
                                SummaryCard(
                                    label = if (isArabic) "الفارق" else "Variance",
                                    value = report.variance?.let { currencyFormatter.format(it) }
                                        ?: "—",
                                    modifier = Modifier.weight(1f)
                                )
                            }

                            if (report.topItems.isNotEmpty()) {
                                Text(
                                    text = if (isArabic) "أعلى العناصر" else "Top Items",
                                    style = MaterialTheme.typography.titleSmall,
                                    modifier = Modifier.padding(vertical = 8.dp)
                                )
                                report.topItems.forEach { item ->
                                    Row(
                                        modifier = Modifier
                                            .fillMaxWidth()
                                            .padding(vertical = 4.dp),
                                        verticalAlignment = Alignment.CenterVertically
                                    ) {
                                        Text(
                                            text = item.name,
                                            modifier = Modifier.weight(1f)
                                        )
                                        Text("×${item.qty}")
                                        Spacer(modifier = Modifier.width(16.dp))
                                        Text(currencyFormatter.format(item.sales))
                                    }
                                }
                            }

                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                TextButton(
                                    onClick = { viewModel.exportCsv(context) },
                                    enabled = uiState.report != null && !uiState.isExporting
                                ) {
                                    if (uiState.isExporting) {
                                        CircularProgressIndicator(
                                            modifier = Modifier.size(16.dp),
                                            strokeWidth = 2.dp
                                        )
                                        Spacer(modifier = Modifier.width(4.dp))
                                        Text(if (isArabic) "جارٍ التصدير…" else "Exporting…")
                                    } else {
                                        Icon(Icons.Default.Share, contentDescription = null)
                                        Spacer(modifier = Modifier.width(4.dp))
                                        Text(if (isArabic) "تصدير CSV" else "Export CSV")
                                    }
                                }
                                TextButton(onClick = { viewModel.loadReport() }) {
                                    Icon(Icons.Default.Refresh, contentDescription = null)
                                    Spacer(modifier = Modifier.width(4.dp))
                                    Text(if (isArabic) "تحديث" else "Refresh")
                                }
                            }
                        }
                        else -> {
                            Text(
                                text = if (isArabic) "لا توجد بيانات لهذا التاريخ." else "No data for this date.",
                                modifier = Modifier.padding(8.dp)
                            )
                        }
                    }
                }

                if (uiState.selectedTab == 1) {
                    CogsDateRangeRow(
                        fromDate = uiState.cogsFromDate,
                        onRangeSelected = { from, to -> viewModel.selectCogsRange(from, to) }
                    )

                    when {
                        uiState.isCogsLoading -> {
                            Box(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(32.dp),
                                contentAlignment = Alignment.Center
                            ) {
                                CircularProgressIndicator()
                            }
                        }
                        uiState.cogsError != null -> {
                            Text(
                                text = uiState.cogsError!!,
                                color = MaterialTheme.colorScheme.error,
                                modifier = Modifier.padding(8.dp)
                            )
                            Button(
                                onClick = { viewModel.loadCogsReport() },
                                modifier = Modifier.align(Alignment.CenterHorizontally)
                            ) {
                                Text(if (isArabic) "إعادة المحاولة" else "Retry")
                            }
                        }
                        uiState.cogsReport != null -> {
                            val report = uiState.cogsReport!!
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                horizontalArrangement = Arrangement.spacedBy(8.dp)
                            ) {
                                SummaryCard(
                                    label = if (isArabic) "المبيعات" else "Sales",
                                    value = currencyFormatter.format(report.salesTotal),
                                    modifier = Modifier.weight(1f)
                                )
                                SummaryCard(
                                    label = "COGS",
                                    value = currencyFormatter.format(report.cogsTotal),
                                    modifier = Modifier.weight(1f)
                                )
                                SummaryCard(
                                    label = if (isArabic) "الهامش" else "Margin",
                                    value = currencyFormatter.format(report.grossMargin),
                                    modifier = Modifier.weight(1f)
                                )
                            }

                            if (report.ingredientUsage.isNotEmpty()) {
                                Text(
                                    text = if (isArabic) "استهلاك المكونات" else "Ingredient Usage",
                                    style = MaterialTheme.typography.titleSmall,
                                    modifier = Modifier.padding(vertical = 8.dp)
                                )
                                Row(modifier = Modifier.fillMaxWidth()) {
                                    Text(
                                        text = if (isArabic) "المكون" else "Ingredient",
                                        modifier = Modifier.weight(1f),
                                        style = MaterialTheme.typography.labelSmall
                                    )
                                    Text(
                                        text = if (isArabic) "الكمية المستخدمة" else "Qty Used",
                                        style = MaterialTheme.typography.labelSmall
                                    )
                                    Spacer(modifier = Modifier.width(16.dp))
                                    Text(
                                        text = if (isArabic) "التكلفة" else "Cost",
                                        style = MaterialTheme.typography.labelSmall
                                    )
                                }
                                Divider()
                                report.ingredientUsage.forEach { item ->
                                    Row(
                                        modifier = Modifier
                                            .fillMaxWidth()
                                            .padding(vertical = 4.dp),
                                        verticalAlignment = Alignment.CenterVertically
                                    ) {
                                        Text(
                                            text = item.name,
                                            modifier = Modifier.weight(1f)
                                        )
                                        Text(item.qtyUsed.stripTrailingZeros().toPlainString())
                                        Spacer(modifier = Modifier.width(16.dp))
                                        Text(currencyFormatter.format(item.cost))
                                    }
                                }
                            } else {
                                Text(
                                    text = if (isArabic) "لا توجد بيانات استهلاك لهذه الفترة." else "No ingredient usage data for this period.",
                                    modifier = Modifier.padding(8.dp)
                                )
                            }

                            TextButton(
                                onClick = { viewModel.loadCogsReport() },
                                modifier = Modifier.align(Alignment.End)
                            ) {
                                Icon(Icons.Default.Refresh, contentDescription = null)
                                Spacer(modifier = Modifier.width(4.dp))
                                Text(if (isArabic) "تحديث" else "Refresh")
                            }
                        }
                        else -> {
                            Text(
                                text = if (isArabic) "اختر نطاقاً زمنياً ثم اضغط تحديث." else "Select a date range and tap Refresh.",
                                modifier = Modifier.padding(8.dp)
                            )
                        }
                    }
                }

                // ── Kitchen tab ───────────────────────────────────────────────
                if (uiState.selectedTab == KITCHEN_TAB) {
                    KitchenTabContent(
                        state = uiState.kdsPerformance,
                        isArabic = isArabic,
                        onSelectPreset = { viewModel.selectKdsPreset(it) },
                        onRetry = { viewModel.loadKdsPerformance() }
                    )
                }

                // ── Export tab ────────────────────────────────────────────────
                if (uiState.selectedTab == EXPORT_TAB) {
                    var exportFrom by remember { mutableStateOf(LocalDate.now().withDayOfMonth(1)) }
                    var exportTo   by remember { mutableStateOf(LocalDate.now()) }

                    Text(if (isArabic) "التصدير المحاسبي" else "Accounting Export",
                        style = MaterialTheme.typography.titleMedium,
                        modifier = Modifier.padding(bottom = 4.dp))
                    Text(
                        if (isArabic) "نزّل ملفات CSV لمحاسبك. افتحها في Excel أو أي برنامج محاسبة."
                        else "Download CSV files for your accountant. Open in Excel or any accounting software.",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )

                    Spacer(modifier = Modifier.height(8.dp))

                    // Date range picker (reuse COGS row)
                    CogsDateRangeRow(
                        fromDate = exportFrom,
                        onRangeSelected = { from, to -> exportFrom = from; exportTo = to }
                    )

                    Spacer(modifier = Modifier.height(8.dp))

                    // Sales CSV
                    Card(modifier = Modifier.fillMaxWidth()) {
                        Column(modifier = Modifier.padding(16.dp),
                            verticalArrangement = Arrangement.spacedBy(4.dp)) {
                            Text(if (isArabic) "تصدير المبيعات" else "Sales Export",
                                style = MaterialTheme.typography.bodyLarge,
                                fontWeight = androidx.compose.ui.text.font.FontWeight.SemiBold)
                            Text(if (isArabic) "صف واحد لكل طلب مدفوع — التاريخ، الفاتورة، النوع، الإجماليات"
                                 else "One row per paid order — date, receipt, type, subtotal, tax, total, payments",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant)
                            Button(
                                onClick = { viewModel.exportSalesCsv(context, exportFrom, exportTo) },
                                enabled = !uiState.isExporting,
                                modifier = Modifier.fillMaxWidth()
                            ) {
                                if (uiState.isExporting) {
                                    CircularProgressIndicator(modifier = Modifier.size(16.dp),
                                        strokeWidth = 2.dp,
                                        color = MaterialTheme.colorScheme.onPrimary)
                                } else {
                                    Text(if (isArabic) "تصدير CSV المبيعات ($exportFrom → $exportTo)"
                                         else "Export Sales CSV ($exportFrom → $exportTo)")
                                }
                            }
                        }
                    }

                    // Purchases CSV
                    Card(modifier = Modifier.fillMaxWidth()) {
                        Column(modifier = Modifier.padding(16.dp),
                            verticalArrangement = Arrangement.spacedBy(4.dp)) {
                            Text(if (isArabic) "تصدير المشتريات" else "Purchases Export",
                                style = MaterialTheme.typography.bodyLarge,
                                fontWeight = androidx.compose.ui.text.font.FontWeight.SemiBold)
                            Text(if (isArabic) "أوامر الشراء — المورد، الحالة، المبالغ"
                                 else "Purchase orders — supplier, status, amounts",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant)
                            Button(
                                onClick = { viewModel.exportPurchasesCsv(context, exportFrom, exportTo) },
                                enabled = !uiState.isExporting,
                                modifier = Modifier.fillMaxWidth()
                            ) {
                                if (uiState.isExporting) {
                                    CircularProgressIndicator(modifier = Modifier.size(16.dp),
                                        strokeWidth = 2.dp,
                                        color = MaterialTheme.colorScheme.onPrimary)
                                } else {
                                    Text(if (isArabic) "تصدير CSV المشتريات ($exportFrom → $exportTo)"
                                         else "Export Purchases CSV ($exportFrom → $exportTo)")
                                }
                            }
                        }
                    }
                }

                // ── Sections tab ──────────────────────────────────────────────
                if (uiState.selectedTab == SECTIONS_TAB) {
                    SectionsTabContent(
                        state = uiState.sectionSales,
                        isArabic = isArabic,
                        currencyFormatter = currencyFormatter,
                        onSelectPreset = { viewModel.selectSectionPreset(it) },
                        onRetry = { viewModel.loadSectionSales() }
                    )
                }
            }
        }
    }
}

@Composable
private fun CogsDateRangeRow(
    fromDate: LocalDate,
    onRangeSelected: (LocalDate, LocalDate) -> Unit
) {
    val currentMonth = LocalDate.now().withDayOfMonth(1)
    Row(
        modifier = Modifier.fillMaxWidth(),
        verticalAlignment = Alignment.CenterVertically
    ) {
        IconButton(onClick = {
            val prevFirst = fromDate.minusMonths(1).withDayOfMonth(1)
            val prevLast = prevFirst.plusMonths(1).minusDays(1)
            onRangeSelected(prevFirst, prevLast)
        }) {
            Icon(Icons.Default.KeyboardArrowLeft, contentDescription = "Previous month")
        }
        Text(
            text = "${fromDate.month.getDisplayName(TextStyle.FULL, Locale.getDefault())} ${fromDate.year}",
            modifier = Modifier.weight(1f),
            textAlign = TextAlign.Center,
            style = MaterialTheme.typography.titleMedium
        )
        IconButton(
            onClick = {
                val nextFirst = fromDate.plusMonths(1).withDayOfMonth(1)
                val nextLast = minOf(nextFirst.plusMonths(1).minusDays(1), LocalDate.now())
                onRangeSelected(nextFirst, nextLast)
            },
            enabled = fromDate.withDayOfMonth(1).isBefore(currentMonth)
        ) {
            Icon(Icons.Default.KeyboardArrowRight, contentDescription = "Next month")
        }
    }
}

@Composable
private fun SummaryCard(
    label: String,
    value: String,
    modifier: Modifier = Modifier
) {
    Card(modifier = modifier) {
        Column(
            modifier = Modifier.padding(12.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Text(
                text = label,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Spacer(modifier = Modifier.height(4.dp))
            Text(
                text = value,
                style = MaterialTheme.typography.titleMedium
            )
        }
    }
}

/** Formats seconds as "Xm Ys" (e.g. 4m 32s). */
internal fun formatSeconds(seconds: Double): String {
    val total = seconds.toLong().coerceAtLeast(0)
    val m = total / 60
    val s = total % 60
    return if (m > 0) "${m}m ${s}s" else "${s}s"
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun KitchenTabContent(
    state: KdsPerformanceState,
    isArabic: Boolean,
    onSelectPreset: (String) -> Unit,
    onRetry: () -> Unit
) {
    val presets = listOf(
        Triple("today", if (isArabic) "اليوم" else "Today", "today"),
        Triple("7d", if (isArabic) "آخر 7 أيام" else "Last 7 days", "7d"),
        Triple("30d", if (isArabic) "آخر 30 يوماً" else "Last 30 days", "30d")
    )

    Row(
        modifier = Modifier
            .fillMaxWidth()
            .horizontalScroll(rememberScrollState()),
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        presets.forEach { (_, label, id) ->
            FilterChip(
                selected = state.rangePreset == id,
                onClick = { onSelectPreset(id) },
                label = { Text(label) }
            )
        }
    }

    when {
        state.isLoading && state.data == null -> {
            Box(
                modifier = Modifier.fillMaxWidth().padding(32.dp),
                contentAlignment = Alignment.Center
            ) { CircularProgressIndicator() }
        }

        state.error != null && state.data == null -> {
            Column(
                modifier = Modifier.fillMaxWidth(),
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Text(state.error!!, color = MaterialTheme.colorScheme.error)
                Button(onClick = onRetry) {
                    Text(if (isArabic) "إعادة المحاولة" else "Retry")
                }
            }
        }

        state.data != null && state.data.totalTickets == 0 -> {
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(
                    containerColor = MaterialTheme.colorScheme.surfaceVariant)
            ) {
                Text(
                    if (isArabic)
                        "📊 لا توجد تذاكر KDS مكتملة في هذه الفترة. ستظهر بيانات الأداء عندما يبدأ المطبخ بمعالجة الطلبات."
                    else
                        "📊 No completed KDS tickets in this period. Performance data will appear once your kitchen starts processing orders.",
                    style = MaterialTheme.typography.bodyMedium,
                    modifier = Modifier.padding(16.dp),
                    textAlign = TextAlign.Center
                )
            }
        }

        state.data != null -> {
            val data = state.data
            KdsKpiRow(data = data, isArabic = isArabic)

            Text(
                if (isArabic) "الإنتاجية بالساعة" else "Hourly throughput",
                style = MaterialTheme.typography.titleSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            KdsHourlyChart(hourly = data.hourlyBreakdown)

            Text(
                if (isArabic) "أبطأ الأصناف (وقت التحضير)" else "Slowest Items (by Prep Time)",
                style = MaterialTheme.typography.titleSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(top = 8.dp)
            )
            if (data.itemBreakdown.isEmpty()) {
                Text(
                    if (isArabic) "لا توجد بيانات تحضير للأصناف." else "No item prep data available.",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            } else {
                data.itemBreakdown.forEach { item ->
                    KdsItemRow(item = item, isArabic = isArabic)
                }
            }
        }
    }
}

@Composable
private fun KdsKpiRow(data: KdsPerformanceDto, isArabic: Boolean) {
    val peakLabel = data.peakThroughputHour?.let { hour ->
        val count = data.peakThroughputCount ?: 0
        String.format("%02d:00 (%d %s)", hour, count, if (isArabic) "تذكرة" else "tkts")
    } ?: "—"

    val cards = listOf(
        Triple(if (isArabic) "متوسط وقت التذكرة" else "Avg Ticket Time",
               formatSeconds(data.avgTotalTicketSeconds),
               if (isArabic) "طلب → انتهاء" else "order → done"),
        Triple(if (isArabic) "P90 وقت التذكرة" else "P90 Ticket Time",
               formatSeconds(data.p90TotalTicketSeconds),
               if (isArabic) "المئين 90" else "90th percentile"),
        Triple(if (isArabic) "متوسط التحضير" else "Avg Prep Time",
               formatSeconds(data.avgPrepSeconds),
               if (isArabic) "استلام → انتهاء" else "ack → done"),
        Triple(if (isArabic) "متوسط الإنتاجية" else "Avg Throughput",
               String.format("%.1f / hr", data.avgThroughputPerHour),
               ""),
        Triple(if (isArabic) "ساعة الذروة" else "Peak Hour", peakLabel, "")
    )

    LazyRow(
        horizontalArrangement = Arrangement.spacedBy(8.dp),
        contentPadding = PaddingValues(vertical = 4.dp)
    ) {
        items(cards) { (label, value, subtitle) ->
            Card(modifier = Modifier.width(140.dp)) {
                Column(Modifier.padding(12.dp), verticalArrangement = Arrangement.spacedBy(2.dp)) {
                    Text(label, style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant)
                    Text(value, style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold)
                    if (subtitle.isNotBlank()) {
                        Text(subtitle, style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant)
                    }
                }
            }
        }
    }
}

@Composable
private fun KdsHourlyChart(hourly: List<KdsHourlyDto>) {
    val maxCount = hourly.maxOfOrNull { it.ticketsCompleted }?.coerceAtLeast(1) ?: 1
    val barColor = MaterialTheme.colorScheme.primary
    val labelHours = listOf(0, 3, 6, 9, 12, 15, 18, 21)

    Card(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(12.dp)) {
            Canvas(modifier = Modifier.fillMaxWidth().height(160.dp)) {
                val barWidth = size.width / 24f
                val chartHeight = size.height - 24f

                hourly.forEach { h ->
                    val fraction = h.ticketsCompleted.toFloat() / maxCount.toFloat()
                    val barHeight = (chartHeight * fraction).coerceAtLeast(2f)
                    drawRect(
                        color = barColor,
                        topLeft = Offset(h.hour * barWidth + barWidth * 0.15f, chartHeight - barHeight),
                        size = Size(barWidth * 0.7f, barHeight)
                    )
                }
            }
            Row(
                Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                labelHours.forEach { hour ->
                    Text(
                        "${hour}h",
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }
        }
    }
}

@Composable
private fun KdsItemRow(item: KdsItemPerformanceDto, isArabic: Boolean) {
    val name = if (isArabic) item.nameAr?.takeIf { it.isNotBlank() } ?: item.name else item.name
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 4.dp)
    ) {
        Text(
            text = "$name  •  ${item.ticketCount} " +
                (if (isArabic) "تذاكر" else "tickets") +
                "  •  avg ${formatSeconds(item.avgPrepSeconds)}" +
                "  •  P90 ${formatSeconds(item.p90PrepSeconds)}",
            style = MaterialTheme.typography.bodyMedium,
            modifier = Modifier.padding(12.dp)
        )
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun SectionsTabContent(
    state: SectionSalesState,
    isArabic: Boolean,
    currencyFormatter: NumberFormat,
    onSelectPreset: (String) -> Unit,
    onRetry: () -> Unit
) {
    val presets = listOf(
        Triple("today", if (isArabic) "اليوم" else "Today", "today"),
        Triple("7d", if (isArabic) "آخر 7 أيام" else "Last 7 days", "7d"),
        Triple("30d", if (isArabic) "آخر 30 يوماً" else "Last 30 days", "30d")
    )

    Row(
        modifier = Modifier
            .fillMaxWidth()
            .horizontalScroll(rememberScrollState()),
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        presets.forEach { (_, label, id) ->
            FilterChip(
                selected = state.rangePreset == id,
                onClick = { onSelectPreset(id) },
                label = { Text(label) }
            )
        }
    }

    when {
        state.isLoading && state.data == null -> {
            Box(
                modifier = Modifier.fillMaxWidth().padding(32.dp),
                contentAlignment = Alignment.Center
            ) { CircularProgressIndicator() }
        }

        state.error != null && state.data == null -> {
            Column(
                modifier = Modifier.fillMaxWidth(),
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Text(state.error!!, color = MaterialTheme.colorScheme.error)
                Button(onClick = onRetry) {
                    Text(if (isArabic) "إعادة المحاولة" else "Retry")
                }
            }
        }

        state.data != null && (state.data.sections.isEmpty() || state.data.totalOrders == 0) -> {
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(
                    containerColor = MaterialTheme.colorScheme.surfaceVariant)
            ) {
                Text(
                    if (isArabic)
                        "🪑 لا توجد بيانات أقسام لهذه الفترة. عيِّن الطاولات لمخططات الطابق لرؤية الإيرادات حسب القسم."
                    else
                        "🪑 No section data for this period. Assign tables to floor plans to see revenue by section.",
                    style = MaterialTheme.typography.bodyMedium,
                    modifier = Modifier.padding(16.dp),
                    textAlign = TextAlign.Center
                )
            }
        }

        state.data != null -> {
            val data = state.data
            val namedSections = data.sections.filter { !it.isUnassigned }
            val topSection = namedSections.firstOrNull()

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                SummaryCard(
                    label = if (isArabic) "إجمالي الأقسام" else "Total Sections",
                    value = namedSections.size.toString(),
                    modifier = Modifier.weight(1f)
                )
                SummaryCard(
                    label = if (isArabic) "أعلى قسم" else "Top Section",
                    value = topSection?.let {
                        "${it.sectionName}\n${currencyFormatter.format(it.netSales)}"
                    } ?: "—",
                    modifier = Modifier.weight(1f)
                )
            }

            data.sections.forEachIndexed { index, section ->
                SectionSalesCard(
                    section = section,
                    isArabic = isArabic,
                    currencyFormatter = currencyFormatter,
                    isTopSection = index == 0 && !section.isUnassigned
                )
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun SectionSalesCard(
    section: SectionSalesDto,
    isArabic: Boolean,
    currencyFormatter: NumberFormat,
    isTopSection: Boolean
) {
    var hourlyExpanded by remember { mutableStateOf(false) }
    val icon = if (section.isUnassigned) "📦" else "🪑"
    val barColor = if (isTopSection) {
        MaterialTheme.colorScheme.primary
    } else {
        MaterialTheme.colorScheme.onSurfaceVariant
    }

    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = if (section.isUnassigned) {
            CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant)
        } else {
            CardDefaults.cardColors()
        }
    ) {
        Column(
            modifier = Modifier.padding(12.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    "$icon ${section.sectionName}",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    modifier = Modifier.weight(1f)
                )
                Text(
                    "${"%.1f".format(section.sharePercent)}% " +
                        (if (isArabic) "من المبيعات" else "of sales"),
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            Text(
                "${section.orderCount} " + (if (isArabic) "طلبات" else "orders") +
                    "  ·  " + (if (isArabic) "متوسط" else "Avg") +
                    " ${currencyFormatter.format(section.avgTicket)}" +
                    "  ·  " + (if (isArabic) "صافي" else "Net") +
                    " ${currencyFormatter.format(section.netSales)}",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )

            LinearProgressIndicator(
                progress = (section.sharePercent / 100.0).toFloat().coerceIn(0f, 1f),
                modifier = Modifier.fillMaxWidth(),
                color = barColor,
                trackColor = MaterialTheme.colorScheme.surfaceVariant
            )

            if (section.topItems.isNotEmpty()) {
                Text(
                    if (isArabic) "أفضل الأصناف:" else "Top items:",
                    style = MaterialTheme.typography.labelMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                section.topItems.forEach { item ->
                    val name = if (isArabic) item.nameAr?.takeIf { it.isNotBlank() } ?: item.name else item.name
                    Text(
                        "• $name ×${item.qty}  ${currencyFormatter.format(item.sales)}",
                        style = MaterialTheme.typography.bodySmall
                    )
                }
            }

            if (section.hourlySales.isNotEmpty()) {
                TextButton(onClick = { hourlyExpanded = !hourlyExpanded }) {
                    Text(
                        if (hourlyExpanded) {
                            if (isArabic) "▾ إخفاء التوزيع بالساعة" else "▾ Hide hourly breakdown"
                        } else {
                            if (isArabic) "▸ عرض التوزيع بالساعة" else "▸ See hourly breakdown"
                        }
                    )
                }
                if (hourlyExpanded) {
                    SectionHourlyChart(hourly = section.hourlySales, barColor = barColor)
                }
            }
        }
    }
}

@Composable
private fun SectionHourlyChart(
    hourly: List<SectionHourlyDto>,
    barColor: androidx.compose.ui.graphics.Color
) {
    val maxSales = hourly.maxOfOrNull { it.sales }?.coerceAtLeast(1.0) ?: 1.0
    val labelHours = listOf(0, 6, 12, 18)

    Card(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(8.dp)) {
            Canvas(modifier = Modifier.fillMaxWidth().height(80.dp)) {
                val barWidth = size.width / 24f
                val chartHeight = size.height - 16f

                hourly.forEach { h ->
                    val fraction = (h.sales / maxSales).toFloat()
                    val barHeight = (chartHeight * fraction).coerceAtLeast(2f)
                    drawRect(
                        color = barColor,
                        topLeft = Offset(h.hour * barWidth + barWidth * 0.15f, chartHeight - barHeight),
                        size = Size(barWidth * 0.7f, barHeight)
                    )
                }
            }
            Row(
                Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                labelHours.forEach { hour ->
                    Text(
                        "${hour}h",
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }
        }
    }
}
