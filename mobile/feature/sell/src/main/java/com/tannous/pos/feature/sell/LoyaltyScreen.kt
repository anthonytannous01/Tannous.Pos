package com.tannous.pos.feature.sell

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.model.LoyaltyAccountDto

// Transaction type constants
private const val TX_EARN   = 0
private const val TX_REDEEM = 1

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun LoyaltyScreen(
    customerId: String,
    onNavigateBack: () -> Unit,
    viewModel: LoyaltyViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }
    var redeemInput by remember { mutableStateOf("") }
    var showRedeemDialog by remember { mutableStateOf(false) }

    LaunchedEffect(customerId) { viewModel.loadAccount(customerId) }

    LaunchedEffect(uiState.error) {
        uiState.error?.let {
            snackbarHostState.showSnackbar(it, duration = SnackbarDuration.Short)
            viewModel.clearError()
        }
    }
    LaunchedEffect(uiState.redeemSuccess) {
        if (uiState.redeemSuccess) {
            snackbarHostState.showSnackbar("Points redeemed successfully")
            viewModel.clearError()
        }
    }

    if (showRedeemDialog) {
        AlertDialog(
            onDismissRequest = { showRedeemDialog = false },
            title = { Text("Redeem Points") },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    Text("Available: ${uiState.account?.pointBalance ?: 0} points")
                    OutlinedTextField(
                        value = redeemInput,
                        onValueChange = { redeemInput = it.filter { c -> c.isDigit() } },
                        label = { Text("Points to redeem") },
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                        singleLine = true
                    )
                }
            },
            confirmButton = {
                TextButton(onClick = {
                    val pts = redeemInput.toIntOrNull() ?: 0
                    if (pts > 0) viewModel.redeem(pts)
                    showRedeemDialog = false
                    redeemInput = ""
                }) { Text("Redeem") }
            },
            dismissButton = {
                TextButton(onClick = { showRedeemDialog = false; redeemInput = "" }) { Text("Cancel") }
            }
        )
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHostState) },
        topBar = {
            TopAppBar(
                title = { Text("Loyalty Account") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = "Back")
                    }
                }
            )
        }
    ) { padding ->
        when {
            uiState.isLoading -> Box(
                Modifier.fillMaxSize().padding(padding),
                contentAlignment = Alignment.Center
            ) { CircularProgressIndicator() }

            uiState.noAccount || uiState.account == null -> Box(
                Modifier.fillMaxSize().padding(padding),
                contentAlignment = Alignment.Center
            ) {
                Column(
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    Text("No loyalty account yet", style = MaterialTheme.typography.titleMedium)
                    Text("Points will be created automatically on the next purchase.",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
            }

            else -> LoyaltyContent(
                account = uiState.account!!,
                isRedeeming = uiState.isRedeeming,
                onRedeemClick = { showRedeemDialog = true },
                modifier = Modifier.padding(padding)
            )
        }
    }
}

@Composable
private fun LoyaltyContent(
    account: LoyaltyAccountDto,
    isRedeeming: Boolean,
    onRedeemClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    LazyColumn(
        modifier = modifier.fillMaxSize().padding(horizontal = 16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
        contentPadding = PaddingValues(vertical = 16.dp)
    ) {
        // Balance card
        item {
            Card(
                Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.primaryContainer)
            ) {
                Column(
                    Modifier.fillMaxWidth().padding(20.dp),
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.spacedBy(4.dp)
                ) {
                    Text(account.customerName,
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold)
                    Text("${account.pointBalance}",
                        style = MaterialTheme.typography.displayMedium,
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.primary)
                    Text("available points", style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
            }
        }

        // Stats
        item {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                Card(Modifier.weight(1f)) {
                    Column(Modifier.padding(12.dp), verticalArrangement = Arrangement.spacedBy(2.dp)) {
                        Text("Lifetime Earned", style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant)
                        Text("${account.lifetimePointsEarned}",
                            style = MaterialTheme.typography.titleLarge, fontWeight = FontWeight.Bold)
                    }
                }
                Card(Modifier.weight(1f)) {
                    Column(Modifier.padding(12.dp), verticalArrangement = Arrangement.spacedBy(2.dp)) {
                        Text("Lifetime Redeemed", style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant)
                        Text("${account.lifetimePointsRedeemed}",
                            style = MaterialTheme.typography.titleLarge, fontWeight = FontWeight.Bold)
                    }
                }
            }
        }

        // Redeem button
        item {
            Button(
                onClick = onRedeemClick,
                modifier = Modifier.fillMaxWidth(),
                enabled = account.pointBalance > 0 && !isRedeeming
            ) {
                if (isRedeeming) {
                    CircularProgressIndicator(Modifier.size(16.dp), strokeWidth = 2.dp)
                    Spacer(Modifier.width(8.dp))
                }
                Text(if (isRedeeming) "Redeeming…" else "Redeem Points")
            }
        }

        // Transaction history
        if (account.recentTransactions.isNotEmpty()) {
            item {
                Text("Recent Transactions", style = MaterialTheme.typography.titleSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
            items(account.recentTransactions) { tx ->
                val isEarn = tx.transactionType == TX_EARN
                Card(Modifier.fillMaxWidth()) {
                    Row(
                        Modifier.fillMaxWidth().padding(12.dp),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column(Modifier.weight(1f)) {
                            Text(
                                when (tx.transactionType) {
                                    TX_EARN -> "Points earned"
                                    TX_REDEEM -> "Points redeemed"
                                    else -> "Adjustment"
                                },
                                style = MaterialTheme.typography.bodyMedium,
                                fontWeight = FontWeight.Medium
                            )
                            tx.notes?.takeIf { it.isNotBlank() }?.let { note ->
                                Text(note, style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant)
                            }
                        }
                        Text(
                            "${if (isEarn) "+" else ""}${tx.points}",
                            style = MaterialTheme.typography.bodyLarge,
                            fontWeight = FontWeight.Bold,
                            color = if (isEarn) Color(0xFF2E7D32) else MaterialTheme.colorScheme.error
                        )
                    }
                }
            }
        }
    }
}
