package com.tannous.pos.feature.printing

import android.content.ClipData
import android.content.ClipboardManager
import android.content.Context
import android.content.Intent
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Create
import androidx.compose.material.icons.filled.Share
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.ui.LocalIsArabic
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PrintingPreviewScreen(
    onNavigateBack: () -> Unit,
    viewModel: PrintingPreviewViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val isArabic = LocalIsArabic.current
    val context = LocalContext.current
    val snackbarHostState = remember { SnackbarHostState() }
    val coroutineScope = rememberCoroutineScope()
    val sampleReceipt = """
        TANNOUS POS
        ================
        
        Receipt #: 2025-001
        Date: 2025-08-31 22:40:00
        Cashier: John Doe
        
        ================
        
        ITEM                    QTY    PRICE
        --------------------------------
        Coffee - Large          1     ${'$'}4.50
        + Extra Shot            1     ${'$'}0.75
        --------------------------------
        Subtotal:                    ${'$'}5.25
        Tax (8.5%):                  ${'$'}0.45
        Total:                        ${'$'}5.70
        
        ================
        
        Payment: Cash
        Amount: ${'$'}6.00
        Change: ${'$'}0.30
        
        ================
        
        Thank you for your purchase!
        Please come again.
        
        ================
    """.trimIndent()

    LaunchedEffect(uiState.printResult) {
        uiState.printResult?.let { msg ->
            snackbarHostState.showSnackbar(msg)
            viewModel.clearPrintResult()
        }
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHostState) },
        topBar = {
            TopAppBar(
                title = { Text(if (isArabic) "معاينة الطباعة" else "Printing Preview") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = if (isArabic) "رجوع" else "Back")
                    }
                },
                actions = {
                    IconButton(
                        onClick = {
                            val clipboardManager = context.getSystemService(
                                Context.CLIPBOARD_SERVICE
                            ) as ClipboardManager
                            val clip = ClipData.newPlainText("Receipt Preview", sampleReceipt)
                            clipboardManager.setPrimaryClip(clip)
                            coroutineScope.launch {
                                snackbarHostState.showSnackbar("Receipt text copied")
                            }
                        }
                    ) {
                        Icon(Icons.Default.Create, contentDescription = "Copy")
                    }
                    IconButton(
                        onClick = {
                            val intent = Intent(Intent.ACTION_SEND).apply {
                                type = "text/plain"
                                putExtra(Intent.EXTRA_TEXT, sampleReceipt)
                                putExtra(Intent.EXTRA_SUBJECT, "Receipt Preview")
                            }
                            context.startActivity(
                                Intent.createChooser(intent, "Share Receipt")
                            )
                        }
                    ) {
                        Icon(Icons.Default.Share, contentDescription = "Share")
                    }
                }
            )
        }
    ) { paddingValues ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
                .padding(16.dp)
        ) {
            Card(
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(
                    modifier = Modifier.padding(16.dp)
                ) {
                    Text(
                        text = "Receipt Preview",
                        style = MaterialTheme.typography.headlineSmall,
                        modifier = Modifier.fillMaxWidth(),
                        textAlign = TextAlign.Center
                    )

                    Spacer(modifier = Modifier.height(16.dp))

                    Text(
                        text = "This is how your receipt will look when printed:",
                        style = MaterialTheme.typography.bodyMedium
                    )
                }
            }

            Spacer(modifier = Modifier.height(16.dp))

            Card(
                modifier = Modifier.fillMaxWidth()
            ) {
                Box(
                    modifier = Modifier
                        .fillMaxWidth()
                        .background(Color.White)
                        .padding(16.dp)
                ) {
                    Text(
                        text = sampleReceipt,
                        fontFamily = FontFamily.Monospace,
                        fontSize = 12.sp,
                        color = Color.Black,
                        modifier = Modifier
                            .fillMaxWidth()
                            .verticalScroll(rememberScrollState())
                    )
                }
            }

            Spacer(modifier = Modifier.height(16.dp))

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                Button(
                    onClick = { viewModel.printSampleReceipt("Receipt preview") },
                    enabled = !uiState.isPrinting,
                    modifier = Modifier.weight(1f)
                ) {
                    if (uiState.isPrinting) {
                        CircularProgressIndicator(
                            modifier = Modifier.size(16.dp),
                            strokeWidth = 2.dp
                        )
                        Spacer(modifier = Modifier.width(8.dp))
                    }
                    Text(if (isArabic) "طباعة الفاتورة" else "Print Receipt")
                }

                OutlinedButton(
                    onClick = { viewModel.printSampleReceipt("Test print") },
                    enabled = !uiState.isPrinting,
                    modifier = Modifier.weight(1f)
                ) {
                    if (uiState.isPrinting) {
                        CircularProgressIndicator(
                            modifier = Modifier.size(16.dp),
                            strokeWidth = 2.dp
                        )
                        Spacer(modifier = Modifier.width(8.dp))
                    }
                    Text(if (isArabic) "طباعة تجريبية" else "Test Print")
                }
            }
        }
    }
}
