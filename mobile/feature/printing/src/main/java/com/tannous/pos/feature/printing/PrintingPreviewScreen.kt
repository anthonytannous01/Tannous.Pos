package com.tannous.pos.feature.printing

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Info
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

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PrintingPreviewScreen(
    onNavigateBack: () -> Unit
) {
    val context = LocalContext.current
    val sampleReceipt = """
        TANNOUS POS
        ================
        
        Receipt #: 2025-001
        Date: 2025-08-31 22:40:00
        Cashier: John Doe
        
        ================
        
        ITEM                    QTY    PRICE
        --------------------------------
        Coffee - Large          1     $4.50
        + Extra Shot            1     $0.75
        --------------------------------
        Subtotal:                    $5.25
        Tax (8.5%):                  $0.45
        Total:                        $5.70
        
        ================
        
        Payment: Cash
        Amount: $6.00
        Change: $0.30
        
        ================
        
        Thank you for your purchase!
        Please come again.
        
        ================
    """.trimIndent()

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Printing Preview") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = "Back")
                    }
                },
                actions = {
                    IconButton(
                        onClick = {
                            // TODO: Implement copy to clipboard
                        }
                    ) {
                        Icon(Icons.Default.Info, contentDescription = "Copy")
                    }
                    IconButton(
                        onClick = {
                            // TODO: Implement share functionality
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
                    onClick = {
                        // TODO: Implement print functionality
                    },
                    modifier = Modifier.weight(1f)
                ) {
                    Text("Print Receipt")
                }
                
                OutlinedButton(
                    onClick = {
                        // TODO: Implement test print
                    },
                    modifier = Modifier.weight(1f)
                ) {
                    Text("Test Print")
                }
            }
        }
    }
}
