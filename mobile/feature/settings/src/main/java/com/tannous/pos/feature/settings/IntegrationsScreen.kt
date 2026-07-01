package com.tannous.pos.feature.settings

import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.model.ApiKeyDto
import com.tannous.pos.core.data.model.WebhookSubscriptionDto
import com.tannous.pos.core.ui.LocalIsArabic
import java.time.Instant
import java.time.ZoneId
import java.time.format.DateTimeFormatter

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun IntegrationsScreen(
    onNavigateBack: () -> Unit,
    viewModel: IntegrationsViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val isArabic = LocalIsArabic.current
    val snackbarHostState = remember { SnackbarHostState() }
    var selectedTab by remember { mutableIntStateOf(0) }
    var revokeKeyId by remember { mutableStateOf<String?>(null) }

    LaunchedEffect(uiState.error) {
        uiState.error?.let {
            snackbarHostState.showSnackbar(it, duration = SnackbarDuration.Short)
            viewModel.clearError()
        }
    }

    if (revokeKeyId != null) {
        AlertDialog(
            onDismissRequest = { revokeKeyId = null },
            title = { Text(if (isArabic) "إلغاء المفتاح" else "Revoke API Key") },
            text = {
                Text(if (isArabic) "هل أنت متأكد؟ لا يمكن التراجع." else "Are you sure? This cannot be undone.")
            },
            confirmButton = {
                TextButton(onClick = {
                    revokeKeyId?.let { viewModel.revokeApiKey(it) }
                    revokeKeyId = null
                }) { Text(if (isArabic) "إلغاء الصلاحية" else "Revoke") }
            },
            dismissButton = {
                TextButton(onClick = { revokeKeyId = null }) {
                    Text(if (isArabic) "رجوع" else "Cancel")
                }
            }
        )
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHostState) },
        topBar = {
            TopAppBar(
                title = { Text(if (isArabic) "واجهة برمجة التطبيقات والتكاملات" else "API & Integrations") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = if (isArabic) "رجوع" else "Back")
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
                    text = { Text(if (isArabic) "Webhooks" else "Webhooks") }
                )
                Tab(
                    selected = selectedTab == 1,
                    onClick = { selectedTab = 1 },
                    text = { Text(if (isArabic) "مفاتيح API" else "API Keys") }
                )
            }

            when {
                uiState.isLoading && uiState.webhooks.isEmpty() && uiState.apiKeys.isEmpty() -> {
                    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        CircularProgressIndicator()
                    }
                }
                selectedTab == 0 -> WebhooksTab(
                    webhooks = uiState.webhooks,
                    isArabic = isArabic,
                    onTest = viewModel::testWebhook,
                    onDelete = viewModel::deleteWebhook
                )
                else -> ApiKeysTab(
                    apiKeys = uiState.apiKeys,
                    isArabic = isArabic,
                    onRevoke = { revokeKeyId = it }
                )
            }
        }
    }
}

@Composable
private fun WebhooksTab(
    webhooks: List<WebhookSubscriptionDto>,
    isArabic: Boolean,
    onTest: (String) -> Unit,
    onDelete: (String) -> Unit
) {
    if (webhooks.isEmpty()) {
        Box(Modifier.fillMaxSize().padding(24.dp), contentAlignment = Alignment.Center) {
            Text(
                if (isArabic) {
                    "لا توجد webhooks. استخدم Swagger أو API لإنشاء اشتراكات."
                } else {
                    "No webhooks configured. Use the API or Swagger to create webhook subscriptions."
                },
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
        return
    }

    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = PaddingValues(16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        items(webhooks, key = { it.id }) { webhook ->
            WebhookCard(webhook, isArabic, onTest, onDelete)
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class, ExperimentalLayoutApi::class)
@Composable
private fun WebhookCard(
    webhook: WebhookSubscriptionDto,
    isArabic: Boolean,
    onTest: (String) -> Unit,
    onDelete: (String) -> Unit
) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(16.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(webhook.name, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
                AssistChip(
                    onClick = {},
                    label = {
                        Text(if (webhook.isActive) {
                            if (isArabic) "نشط" else "Active"
                        } else {
                            if (isArabic) "غير نشط" else "Inactive"
                        })
                    },
                    enabled = false
                )
            }

            Spacer(Modifier.height(4.dp))
            Text(
                truncateUrl(webhook.endpointUrl),
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis
            )

            if (webhook.events.isNotEmpty()) {
                Spacer(Modifier.height(8.dp))
                FlowRow(
                    horizontalArrangement = Arrangement.spacedBy(6.dp),
                    verticalArrangement = Arrangement.spacedBy(4.dp)
                ) {
                    webhook.events.forEach { event ->
                        SuggestionChip(onClick = {}, label = { Text(event, style = MaterialTheme.typography.labelSmall) })
                    }
                }
            }

            webhook.lastDeliveryAt?.let { lastAt ->
                Spacer(Modifier.height(8.dp))
                val status = if (webhook.lastDeliverySucceeded == true) "✅" else "❌"
                Text(
                    "$status ${formatTimestamp(lastAt)}",
                    style = MaterialTheme.typography.bodySmall
                )
            }

            Row(
                modifier = Modifier.fillMaxWidth().padding(top = 8.dp),
                horizontalArrangement = Arrangement.End
            ) {
                IconButton(onClick = { onTest(webhook.id) }) {
                    Icon(Icons.Default.PlayArrow, contentDescription = if (isArabic) "اختبار" else "Test")
                }
                IconButton(onClick = { onDelete(webhook.id) }) {
                    Icon(Icons.Default.Delete, contentDescription = if (isArabic) "حذف" else "Delete")
                }
            }
        }
    }
}

@Composable
private fun ApiKeysTab(
    apiKeys: List<ApiKeyDto>,
    isArabic: Boolean,
    onRevoke: (String) -> Unit
) {
    if (apiKeys.isEmpty()) {
        Box(Modifier.fillMaxSize().padding(24.dp), contentAlignment = Alignment.Center) {
            Text(
                if (isArabic) {
                    "لا توجد مفاتيح API. أنشئ واحداً عبر Swagger على /api/v1/apikeys."
                } else {
                    "No API keys. Create one via Swagger at /api/v1/apikeys."
                },
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
        return
    }

    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = PaddingValues(16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        items(apiKeys, key = { it.id }) { key ->
            ApiKeyCard(key, isArabic, onRevoke)
        }
    }
}

@Composable
private fun ApiKeyCard(
    key: ApiKeyDto,
    isArabic: Boolean,
    onRevoke: (String) -> Unit
) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(16.dp)) {
            Text(key.name, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
            Spacer(Modifier.height(4.dp))
            Text(
                key.keyPrefix,
                style = MaterialTheme.typography.bodyMedium,
                fontFamily = FontFamily.Monospace
            )
            Spacer(Modifier.height(4.dp))
            Text(
                "${if (isArabic) "أُنشئ" else "Created"}: ${formatTimestamp(key.createdAt)}",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Text(
                "${if (isArabic) "آخر استخدام" else "Last used"}: ${
                    key.lastUsedAt?.let { formatTimestamp(it) }
                        ?: if (isArabic) "أبداً" else "Never"
                }",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Row(modifier = Modifier.fillMaxWidth().padding(top = 8.dp), horizontalArrangement = Arrangement.End) {
                TextButton(onClick = { onRevoke(key.id) }) {
                    Text(if (isArabic) "إلغاء" else "Revoke")
                }
            }
        }
    }
}

private fun truncateUrl(url: String): String =
    if (url.length <= 40) url else url.take(40) + "..."

private fun formatTimestamp(iso: String): String = try {
    val instant = Instant.parse(iso)
    DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm")
        .withZone(ZoneId.systemDefault())
        .format(instant)
} catch (_: Exception) {
    iso
}
