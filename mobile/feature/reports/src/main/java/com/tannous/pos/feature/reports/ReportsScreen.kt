package com.tannous.pos.feature.reports

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.KeyboardArrowLeft
import androidx.compose.material.icons.filled.KeyboardArrowRight
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import java.time.LocalDate
import java.time.format.DateTimeFormatter

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ReportsScreen(
    onNavigateBack: () -> Unit,
    viewModel: ReportsViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val scrollState = rememberScrollState()
    val currencyFormatter = remember(uiState.currencyCode) {
        currencyFormatterFor(uiState.currencyCode)
    }
    val dateLabel = if (uiState.selectedDate == LocalDate.now()) {
        "Today"
    } else {
        uiState.selectedDate.format(DateTimeFormatter.ofPattern("MMM d, yyyy"))
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Reports") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = "Back")
                    }
                }
            )
        }
    ) { paddingValues ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
                .verticalScroll(scrollState)
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically
            ) {
                IconButton(onClick = {
                    viewModel.selectDate(uiState.selectedDate.minusDays(1))
                }) {
                    Icon(Icons.Default.KeyboardArrowLeft, contentDescription = "Previous day")
                }
                Text(
                    text = dateLabel,
                    style = MaterialTheme.typography.titleMedium,
                    modifier = Modifier.weight(1f),
                    textAlign = TextAlign.Center
                )
                IconButton(
                    onClick = { viewModel.selectDate(uiState.selectedDate.plusDays(1)) },
                    enabled = uiState.selectedDate.isBefore(LocalDate.now())
                ) {
                    Icon(Icons.Default.KeyboardArrowRight, contentDescription = "Next day")
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
                        Text("Retry")
                    }
                }
                uiState.report != null -> {
                    val report = uiState.report!!
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        SummaryCard(
                            label = "Net Sales",
                            value = currencyFormatter.format(report.netSales),
                            modifier = Modifier.weight(1f)
                        )
                        SummaryCard(
                            label = "Orders",
                            value = report.ordersCount.toString(),
                            modifier = Modifier.weight(1f)
                        )
                        SummaryCard(
                            label = "Avg Ticket",
                            value = currencyFormatter.format(report.avgTicket),
                            modifier = Modifier.weight(1f)
                        )
                    }
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        SummaryCard(
                            label = "Cash Drops",
                            value = currencyFormatter.format(report.cashDrops),
                            modifier = Modifier.weight(1f)
                        )
                        SummaryCard(
                            label = "Variance",
                            value = report.variance?.let { currencyFormatter.format(it) } ?: "—",
                            modifier = Modifier.weight(1f)
                        )
                    }

                    if (report.topItems.isNotEmpty()) {
                        Text(
                            text = "Top Items",
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

                    TextButton(
                        onClick = { viewModel.loadReport() },
                        modifier = Modifier.align(Alignment.End)
                    ) {
                        Icon(Icons.Default.Refresh, contentDescription = null)
                        Spacer(modifier = Modifier.width(4.dp))
                        Text("Refresh")
                    }
                }
                else -> {
                    Text(
                        text = "No data for this date.",
                        modifier = Modifier.padding(8.dp)
                    )
                }
            }
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
