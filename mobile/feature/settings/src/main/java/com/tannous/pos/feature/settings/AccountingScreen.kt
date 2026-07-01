package com.tannous.pos.feature.settings

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.List
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.model.AccountingConnectionStatusDto
import com.tannous.pos.core.ui.LocalIsArabic

private data class ProviderInfo(
    val key: String,
    val displayName: String,
    val displayNameAr: String,
    val connectEnabled: Boolean
)

private val PROVIDERS = listOf(
    ProviderInfo("QuickBooks", "QuickBooks Online", "QuickBooks Online", connectEnabled = true),
    ProviderInfo("Xero", "Xero", "Xero", connectEnabled = false)
)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AccountingScreen(
    onNavigateBack: () -> Unit,
    viewModel: AccountingViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val isArabic = LocalIsArabic.current
    val context = LocalContext.current
    val snackbarHostState = remember { SnackbarHostState() }

    LaunchedEffect(uiState.error) {
        uiState.error?.let {
            snackbarHostState.showSnackbar(it, duration = SnackbarDuration.Short)
            viewModel.clearError()
        }
    }

    LaunchedEffect(uiState.syncMessage) {
        uiState.syncMessage?.let {
            snackbarHostState.showSnackbar(it, duration = SnackbarDuration.Short)
            viewModel.clearSyncMessage()
        }
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHostState) },
        topBar = {
            TopAppBar(
                title = { Text(if (isArabic) "المحاسبة" else "Accounting") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = if (isArabic) "رجوع" else "Back")
                    }
                }
            )
        }
    ) { padding ->
        if (uiState.isLoading && uiState.connections.isEmpty()) {
            Box(
                modifier = Modifier.fillMaxSize().padding(padding),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator()
            }
        } else {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding)
                    .verticalScroll(rememberScrollState())
                    .padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                PROVIDERS.forEach { provider ->
                    val status = uiState.connections.firstOrNull {
                        it.provider.equals(provider.key, ignoreCase = true)
                    }
                    ProviderCard(
                        provider = provider,
                        status = status,
                        isArabic = isArabic,
                        isLoading = uiState.isLoading,
                        onConnect = {
                            if (provider.key == "QuickBooks") {
                                viewModel.connectQuickBooks(context)
                            }
                        },
                        onSync = { viewModel.triggerSync() },
                        onDisconnect = { viewModel.disconnect(provider.key) }
                    )
                }
            }
        }
    }
}

@Composable
private fun ProviderCard(
    provider: ProviderInfo,
    status: AccountingConnectionStatusDto?,
    isArabic: Boolean,
    isLoading: Boolean,
    onConnect: () -> Unit,
    onSync: () -> Unit,
    onDisconnect: () -> Unit
) {
    val connected = status?.isConnected == true
    val title = if (isArabic) provider.displayNameAr else provider.displayName

    Card(modifier = Modifier.fillMaxWidth()) {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                Icon(
                    Icons.Default.List,
                    contentDescription = null,
                    modifier = Modifier.size(32.dp)
                )
                Text(title, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
            }

            if (connected) {
                val company = status?.companyName?.takeIf { it.isNotBlank() }
                Text(
                    text = if (company != null)
                        (if (isArabic) "● متصل — $company" else "● Connected — $company")
                    else
                        (if (isArabic) "● متصل" else "● Connected"),
                    color = MaterialTheme.colorScheme.primary,
                    style = MaterialTheme.typography.bodyMedium
                )
                status?.lastSyncAt?.let {
                    Text(
                        text = if (isArabic) "آخر مزامنة: $it" else "Last sync: $it",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
                status?.lastSyncError?.let {
                    Text(
                        text = it,
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.error
                    )
                }
            } else {
                Text(
                    text = if (isArabic) "○ غير متصل" else "○ Not connected",
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                if (connected) {
                    OutlinedButton(onClick = onSync, enabled = !isLoading) {
                        Text(if (isArabic) "مزامنة الآن" else "Sync Now")
                    }
                    OutlinedButton(onClick = onDisconnect, enabled = !isLoading) {
                        Text(if (isArabic) "قطع الاتصال" else "Disconnect")
                    }
                } else if (provider.connectEnabled) {
                    Button(onClick = onConnect, enabled = !isLoading) {
                        Text(if (isArabic) "ربط" else "Connect")
                    }
                } else {
                    Text(
                        text = if (isArabic) "قريباً" else "Coming soon",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }
        }
    }
}
