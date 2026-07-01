package com.tannous.pos.feature.shifts

import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.ui.LocalIsArabic
import com.tannous.pos.core.util.currencyFormatterFor
import java.math.BigDecimal
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ShiftsScreen(
    onNavigateBack: () -> Unit,
    viewModel: ShiftViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val isArabic = LocalIsArabic.current
    val currencyFormatter = remember(uiState.currencyCode) {
        currencyFormatterFor(uiState.currencyCode)
    }
    
    var showOpenShiftDialog by remember { mutableStateOf(false) }
    var showCloseShiftDialog by remember { mutableStateOf(false) }
    var showCashDropDialog by remember { mutableStateOf(false) }
    
    val snackbarHostState = remember { SnackbarHostState() }
    
    // Show snackbar on error
    LaunchedEffect(uiState.errorMessage) {
        uiState.errorMessage?.let { error ->
            snackbarHostState.showSnackbar(
                message = error,
                duration = SnackbarDuration.Long
            )
            viewModel.clearError()
        }
    }
    
    // Show success snackbar after operations
    LaunchedEffect(uiState.activeShift, uiState.isLoading) {
        if (!uiState.isLoading && uiState.activeShift != null && uiState.errorMessage == null) {
            // Success - could show a snackbar here if needed
        }
    }
    
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(if (isArabic) "إدارة الوردية" else "Shift Management") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = if (isArabic) "رجوع" else "Back")
                    }
                }
            )
        },
        snackbarHost = {
            SnackbarHost(hostState = snackbarHostState)
        }
    ) { paddingValues ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
                .padding(16.dp)
        ) {
            when {
                uiState.isLoading -> {
                    CircularProgressIndicator(
                        modifier = Modifier.align(Alignment.Center)
                    )
                }
                
                uiState.activeShift == null -> {
                    // No active shift - show open shift option
    Column(
        modifier = Modifier
            .fillMaxSize()
                            .align(Alignment.Center),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Text(
                            text = if (isArabic) "لا توجد وردية نشطة" else "No Active Shift",
                            style = MaterialTheme.typography.headlineMedium,
                            fontWeight = FontWeight.Bold
                        )
                        
                        Spacer(modifier = Modifier.height(16.dp))
                        
                        Text(
                            text = if (isArabic) "افتح وردية لبدء معالجة المبيعات" else "Open a shift to begin processing sales",
                            style = MaterialTheme.typography.bodyLarge,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        
        Spacer(modifier = Modifier.height(32.dp))
        
        Button(
                            onClick = { showOpenShiftDialog = true },
            modifier = Modifier.fillMaxWidth()
        ) {
                            Text(if (isArabic) "فتح وردية" else "Open Shift")
                        }
                    }
                }
                
                else -> {
                    // Active shift exists - show details and close option
                    val shift = uiState.activeShift!!
                    Column(
                        modifier = Modifier.fillMaxSize(),
                        verticalArrangement = Arrangement.spacedBy(16.dp)
                    ) {
                        // Shift Details Card
                        Card(
                            modifier = Modifier.fillMaxWidth(),
                            elevation = CardDefaults.cardElevation(defaultElevation = 4.dp)
                        ) {
                            Column(
                                modifier = Modifier.padding(16.dp),
                                verticalArrangement = Arrangement.spacedBy(12.dp)
                            ) {
                                Text(
                                    text = if (isArabic) "الوردية النشطة" else "Active Shift",
                                    style = MaterialTheme.typography.titleLarge,
                                    fontWeight = FontWeight.Bold
                                )
                                
                                Divider()
                                
                                Row(
                                    modifier = Modifier.fillMaxWidth(),
                                    horizontalArrangement = Arrangement.SpaceBetween
                                ) {
                                    Text(
                                        text = if (isArabic) "رقم الوردية:" else "Shift Number:",
                                        style = MaterialTheme.typography.bodyMedium
                                    )
                                    Text(
                                        text = shift.shiftNumber,
                                        style = MaterialTheme.typography.bodyMedium,
                                        fontWeight = FontWeight.SemiBold
                                    )
                                }
                                
                                Row(
                                    modifier = Modifier.fillMaxWidth(),
                                    horizontalArrangement = Arrangement.SpaceBetween
                                ) {
                                    Text(
                                        text = if (isArabic) "فُتحت:" else "Opened:",
                                        style = MaterialTheme.typography.bodyMedium
                                    )
                                    Text(
                                        text = formatDateTime(shift.startTime),
                                        style = MaterialTheme.typography.bodyMedium
                                    )
                                }
                                
                                Row(
                                    modifier = Modifier.fillMaxWidth(),
                                    horizontalArrangement = Arrangement.SpaceBetween
                                ) {
                                    Text(
                                        text = if (isArabic) "رصيد الافتتاح:" else "Opening Balance:",
                                        style = MaterialTheme.typography.bodyMedium
                                    )
                                    Text(
                                        text = currencyFormatter.format(shift.openingBalance),
                                        style = MaterialTheme.typography.bodyMedium,
                                        fontWeight = FontWeight.SemiBold
                                    )
                                }
                                
                                // Live sales summary — computed from paid orders in Room.
                                // Updates automatically when new orders are finalized.
                                Divider()

                                Row(
                                    modifier = Modifier.fillMaxWidth(),
                                    horizontalArrangement = Arrangement.SpaceBetween
                                ) {
                                    Text(
                                        text = if (isArabic) "طلبات هذه الوردية:" else "Orders This Shift:",
                                        style = MaterialTheme.typography.bodyMedium
                                    )
                                    Text(
                                        text = "${uiState.shiftOrderCount}",
                                        style = MaterialTheme.typography.bodyMedium,
                                        fontWeight = FontWeight.SemiBold
                                    )
                                }

                                Row(
                                    modifier = Modifier.fillMaxWidth(),
                                    horizontalArrangement = Arrangement.SpaceBetween
                                ) {
                                    Text(
                                        text = if (isArabic) "إجمالي المبيعات:" else "Sales Total:",
                                        style = MaterialTheme.typography.bodyMedium
                                    )
                                    Text(
                                        text = currencyFormatter.format(uiState.shiftSalesTotal),
                                        style = MaterialTheme.typography.bodyMedium,
                                        fontWeight = FontWeight.SemiBold,
                                        color = MaterialTheme.colorScheme.primary
                                    )
                                }

                                shift.expectedCash?.let { expected ->
                                    Row(
                                        modifier = Modifier.fillMaxWidth(),
                                        horizontalArrangement = Arrangement.SpaceBetween
                                    ) {
                                        Text(
                                            text = if (isArabic) "النقد المتوقع:" else "Expected Cash:",
                                            style = MaterialTheme.typography.bodyMedium
                                        )
                                        Text(
                                            text = currencyFormatter.format(expected),
                                            style = MaterialTheme.typography.bodyMedium,
                                            fontWeight = FontWeight.SemiBold
                                        )
                                    }
                                }
                                
                                shift.notes?.let { notes ->
                                    if (notes.isNotEmpty()) {
                                        Spacer(modifier = Modifier.height(4.dp))
                                        Text(
                                            text = if (isArabic) "ملاحظات: $notes" else "Notes: $notes",
                                            style = MaterialTheme.typography.bodySmall,
                                            color = MaterialTheme.colorScheme.onSurfaceVariant
                                        )
                                    }
                                }
                            }
                        }
                        
                        // Cash Drop Button
                        OutlinedButton(
                            onClick = { showCashDropDialog = true },
                            modifier = Modifier.fillMaxWidth(),
                            enabled = !uiState.isLoading
                        ) {
                            Text(if (isArabic) "إيداع نقدي" else "Cash Drop")
                        }

                        // Close Shift Button
                        Button(
                            onClick = { showCloseShiftDialog = true },
                            modifier = Modifier.fillMaxWidth(),
                            enabled = !uiState.isLoading,
                            colors = ButtonDefaults.buttonColors(
                                containerColor = MaterialTheme.colorScheme.error
                            )
                        ) {
                            Text(if (isArabic) "إغلاق الوردية" else "Close Shift")
                        }
                    }
                }
            }
        }
    }
    
    // Open Shift Dialog
    if (showOpenShiftDialog) {
        OpenShiftDialog(
            onConfirm = { openingBalance, notes ->
                viewModel.openShift(openingBalance, notes)
                showOpenShiftDialog = false
            },
            onDismiss = { showOpenShiftDialog = false }
        )
    }
    
    // Cash Drop Dialog
    if (showCashDropDialog && uiState.activeShift != null) {
        CashDropDialog(
            onConfirm = { amount, note ->
                viewModel.cashDrop(uiState.activeShift!!.id, amount, note)
                showCashDropDialog = false
            },
            onDismiss = { showCashDropDialog = false }
        )
    }
    
    // Close Shift Dialog
    if (showCloseShiftDialog && uiState.activeShift != null) {
        CloseShiftDialog(
            shiftId = uiState.activeShift!!.id,
            expectedCash = uiState.activeShift!!.expectedCash ?: uiState.activeShift!!.openingBalance,
            onConfirm = { closingCount, note ->
                viewModel.closeShift(uiState.activeShift!!.id, closingCount, note)
                showCloseShiftDialog = false
            },
            onDismiss = { showCloseShiftDialog = false }
        )
    }
}

private fun formatDateTime(dateTimeString: String): String {
    return try {
        // Try to parse ISO format from backend
        val dateTime = LocalDateTime.parse(dateTimeString, DateTimeFormatter.ISO_DATE_TIME)
        dateTime.format(DateTimeFormatter.ofPattern("MM/dd/yyyy HH:mm"))
    } catch (e: Exception) {
        // Fallback to original string if parsing fails
        dateTimeString
    }
}
