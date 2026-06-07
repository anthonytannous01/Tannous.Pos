package com.tannous.pos.feature.sell

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Close
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.model.PublicMenuCategoryDto
import com.tannous.pos.core.data.model.PublicMenuItemDto
import com.tannous.pos.core.ui.LocalIsArabic
import java.math.BigDecimal

private const val KIOSK_EXIT_PIN = "1234" // operator changes this in production settings

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun KioskScreen(
    onExit: () -> Unit,
    viewModel: KioskViewModel = hiltViewModel()
) {
    val uiState   by viewModel.uiState.collectAsStateWithLifecycle()
    val isArabic   = LocalIsArabic.current
    var showCart   by remember { mutableStateOf(false) }
    var showPinDialog by remember { mutableStateOf(false) }
    var enteredPin by remember { mutableStateOf("") }
    var pinError   by remember { mutableStateOf(false) }

    // PIN exit dialog
    if (showPinDialog) {
        AlertDialog(
            onDismissRequest = { showPinDialog = false; enteredPin = ""; pinError = false },
            title = { Text("Exit Kiosk Mode") },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    Text("Enter operator PIN to exit:")
                    OutlinedTextField(
                        value = enteredPin,
                        onValueChange = { if (it.length <= 4) enteredPin = it },
                        label = { Text("PIN") },
                        visualTransformation = androidx.compose.ui.text.input.PasswordVisualTransformation(),
                        isError = pinError,
                        supportingText = if (pinError) {{ Text("Incorrect PIN", color = MaterialTheme.colorScheme.error) }} else null,
                        modifier = Modifier.fillMaxWidth()
                    )
                }
            },
            confirmButton = {
                TextButton(onClick = {
                    if (enteredPin == KIOSK_EXIT_PIN) {
                        showPinDialog = false; onExit()
                    } else {
                        pinError = true; enteredPin = ""
                    }
                }) { Text("Exit") }
            },
            dismissButton = {
                TextButton(onClick = { showPinDialog = false; enteredPin = ""; pinError = false }) {
                    Text("Cancel")
                }
            }
        )
    }

    // Order placed screen
    if (uiState.placedOrder != null) {
        KioskOrderPlacedScreen(
            result   = uiState.placedOrder!!,
            currency = uiState.currency,
            onNewOrder = { viewModel.resetAfterOrder(); showCart = false }
        )
        return
    }

    if (showCart) {
        KioskCartScreen(
            cart      = uiState.cart,
            currency  = uiState.currency,
            isPlacing = uiState.isPlacing,
            error     = uiState.placeError,
            onRemove  = { viewModel.removeItem(it) },
            onAdd     = { item -> viewModel.addItem(item) },
            onBack    = { showCart = false },
            onPlace   = { name, notes -> viewModel.placeOrder(name, notes) },
            onClearError = { viewModel.clearError() }
        )
        return
    }

    // ── Main browse screen ────────────────────────────────────────────────────
    Box(modifier = Modifier.fillMaxSize().background(MaterialTheme.colorScheme.background)) {
        Column(modifier = Modifier.fillMaxSize()) {

            // Header
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .background(MaterialTheme.colorScheme.primary)
                    .padding(horizontal = 24.dp, vertical = 20.dp)
            ) {
                Text(
                    text = if (isArabic && uiState.businessName.isNotBlank())
                        uiState.businessName else uiState.businessName,
                    color = Color.White,
                    fontSize = 28.sp,
                    fontWeight = FontWeight.Bold,
                    modifier = Modifier.align(Alignment.CenterStart)
                )
                // Exit kiosk — small, hidden corner button
                TextButton(
                    onClick = { showPinDialog = true },
                    modifier = Modifier.align(Alignment.CenterEnd),
                    colors = ButtonDefaults.textButtonColors(contentColor = Color.White.copy(alpha = 0.5f))
                ) { Text("Exit", fontSize = 12.sp) }
            }

            when {
                uiState.isLoading -> Box(Modifier.weight(1f).fillMaxWidth(),
                    contentAlignment = Alignment.Center) {
                    Column(horizontalAlignment = Alignment.CenterHorizontally) {
                        CircularProgressIndicator()
                        Spacer(Modifier.height(16.dp))
                        Text("Loading menu...", style = MaterialTheme.typography.bodyLarge)
                    }
                }

                uiState.error != null -> Box(Modifier.weight(1f).fillMaxWidth(),
                    contentAlignment = Alignment.Center) {
                    Column(horizontalAlignment = Alignment.CenterHorizontally,
                        verticalArrangement = Arrangement.spacedBy(12.dp)) {
                        Text(uiState.error!!, style = MaterialTheme.typography.bodyLarge)
                        Button(onClick = { viewModel.loadMenu() }) { Text("Retry") }
                    }
                }

                else -> {
                    // Category tabs
                    LazyRow(
                        modifier = Modifier.fillMaxWidth().padding(12.dp),
                        horizontalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        items(uiState.categories) { cat ->
                            val selected = uiState.selectedCategory?.id == cat.id
                            val label = if (isArabic) cat.nameAr?.takeIf { it.isNotBlank() } ?: cat.name else cat.name
                            FilterChip(
                                selected = selected,
                                onClick  = { viewModel.selectCategory(cat) },
                                label    = { Text(label, fontSize = 16.sp, fontWeight = if (selected) FontWeight.Bold else FontWeight.Normal) },
                                modifier = Modifier.height(48.dp)
                            )
                        }
                    }

                    // Menu items grid
                    val items = uiState.selectedCategory?.items ?: emptyList()
                    LazyColumn(
                        modifier = Modifier.weight(1f).fillMaxWidth(),
                        contentPadding = PaddingValues(12.dp),
                        verticalArrangement = Arrangement.spacedBy(12.dp)
                    ) {
                        items(items.chunked(2)) { row ->
                            Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                                row.forEach { item ->
                                    KioskItemCard(
                                        item     = item,
                                        currency = uiState.currency,
                                        isArabic = isArabic,
                                        qty      = uiState.cart.find { it.item.id == item.id }?.quantity ?: 0,
                                        onAdd    = { viewModel.addItem(item) },
                                        onRemove = { viewModel.removeItem(item.id) },
                                        modifier = Modifier.weight(1f)
                                    )
                                }
                                if (row.size == 1) Spacer(Modifier.weight(1f))
                            }
                        }
                    }
                }
            }

            // Cart bar
            val cartCount = uiState.cart.sumOf { it.quantity }
            if (cartCount > 0) {
                Button(
                    onClick  = { showCart = true },
                    modifier = Modifier.fillMaxWidth().height(72.dp).padding(12.dp),
                    shape    = RoundedCornerShape(12.dp)
                ) {
                    Text(
                        text     = "View Order ($cartCount items) · ${uiState.currency} ${viewModel.cartTotal}",
                        fontSize = 18.sp,
                        fontWeight = FontWeight.Bold
                    )
                }
            }
        }
    }
}

@Composable
private fun KioskItemCard(
    item:     PublicMenuItemDto,
    currency: String,
    isArabic: Boolean,
    qty:      Int,
    onAdd:    () -> Unit,
    onRemove: () -> Unit,
    modifier: Modifier = Modifier
) {
    val displayName = if (isArabic) item.nameAr?.takeIf { it.isNotBlank() } ?: item.name else item.name

    Card(
        modifier  = modifier.height(200.dp),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp),
        shape     = RoundedCornerShape(16.dp)
    ) {
        Column(
            modifier = Modifier.fillMaxSize().padding(16.dp),
            verticalArrangement = Arrangement.SpaceBetween
        ) {
            Column {
                Text(
                    text       = displayName,
                    fontWeight = FontWeight.SemiBold,
                    fontSize   = 18.sp,
                    maxLines   = 2,
                    overflow   = TextOverflow.Ellipsis
                )
                item.description?.let {
                    Text(
                        text     = it,
                        fontSize = 13.sp,
                        color    = MaterialTheme.colorScheme.onSurfaceVariant,
                        maxLines = 2,
                        overflow = TextOverflow.Ellipsis,
                        modifier = Modifier.padding(top = 4.dp)
                    )
                }
            }

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text       = "$currency ${item.price}",
                    fontWeight = FontWeight.Bold,
                    fontSize   = 18.sp,
                    color      = MaterialTheme.colorScheme.primary
                )
                if (qty == 0) {
                    IconButton(
                        onClick  = onAdd,
                        modifier = Modifier.size(44.dp).background(
                            MaterialTheme.colorScheme.primary, RoundedCornerShape(22.dp)
                        )
                    ) {
                        Icon(Icons.Default.Add, contentDescription = "Add",
                            tint = Color.White)
                    }
                } else {
                    Row(verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(4.dp)) {
                        IconButton(onClick = onRemove, modifier = Modifier.size(36.dp)) {
                            Icon(Icons.Default.Close, contentDescription = "Decrease")
                        }
                        Text(qty.toString(), fontWeight = FontWeight.Bold, fontSize = 18.sp)
                        IconButton(onClick = onAdd, modifier = Modifier.size(36.dp)) {
                            Icon(Icons.Default.Add, contentDescription = "Add")
                        }
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun KioskCartScreen(
    cart:      List<KioskCartLine>,
    currency:  String,
    isPlacing: Boolean,
    error:     String?,
    onRemove:  (String) -> Unit,
    onAdd:     (PublicMenuItemDto) -> Unit,
    onBack:    () -> Unit,
    onPlace:   (String?, String?) -> Unit,
    onClearError: () -> Unit
) {
    var customerName by remember { mutableStateOf("") }
    var notes        by remember { mutableStateOf("") }
    val total = cart.fold(BigDecimal.ZERO) { acc, l -> acc + l.item.price * BigDecimal(l.quantity) }

    Column(modifier = Modifier.fillMaxSize().background(MaterialTheme.colorScheme.background)) {
        // Header
        Box(modifier = Modifier.fillMaxWidth().background(MaterialTheme.colorScheme.primary)
            .padding(24.dp)) {
            Text("Your Order", color = Color.White, fontSize = 24.sp,
                fontWeight = FontWeight.Bold, modifier = Modifier.align(Alignment.CenterStart))
            TextButton(onClick = onBack, modifier = Modifier.align(Alignment.CenterEnd),
                colors = ButtonDefaults.textButtonColors(contentColor = Color.White)) {
                Text("Back to Menu")
            }
        }

        LazyColumn(
            modifier = Modifier.weight(1f).padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            items(cart) { line ->
                Card(modifier = Modifier.fillMaxWidth()) {
                    Row(modifier = Modifier.padding(16.dp).fillMaxWidth(),
                        verticalAlignment = Alignment.CenterVertically) {
                        Column(modifier = Modifier.weight(1f)) {
                            Text(line.item.name, fontWeight = FontWeight.SemiBold, fontSize = 16.sp)
                            Text("$currency ${line.item.price} each",
                                style = MaterialTheme.typography.bodySmall)
                        }
                        Row(verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                            IconButton(onClick = { onRemove(line.item.id) }, modifier = Modifier.size(40.dp)) {
                                Icon(Icons.Default.Close, contentDescription = "Decrease")
                            }
                            Text(line.quantity.toString(), fontSize = 20.sp, fontWeight = FontWeight.Bold)
                            IconButton(onClick = { onAdd(line.item) }, modifier = Modifier.size(40.dp)) {
                                Icon(Icons.Default.Add, contentDescription = "Add")
                            }
                        }
                        Text("$currency ${line.item.price * BigDecimal(line.quantity)}",
                            fontWeight = FontWeight.Bold, fontSize = 16.sp,
                            modifier = Modifier.padding(start = 12.dp))
                    }
                }
            }

            item {
                Spacer(Modifier.height(8.dp))
                OutlinedTextField(value = customerName, onValueChange = { customerName = it },
                    label = { Text("Your name (optional)") }, modifier = Modifier.fillMaxWidth(),
                    singleLine = true)
                Spacer(Modifier.height(8.dp))
                OutlinedTextField(value = notes, onValueChange = { notes = it },
                    label = { Text("Special requests (optional)") }, modifier = Modifier.fillMaxWidth(),
                    minLines = 2)
            }

            error?.let {
                item {
                    Text(it, color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodySmall)
                }
            }
        }

        // Total + place order
        Column(modifier = Modifier.fillMaxWidth().padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)) {
            Row(modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween) {
                Text("Total", fontSize = 22.sp, fontWeight = FontWeight.Bold)
                Text("$currency $total", fontSize = 22.sp, fontWeight = FontWeight.Bold,
                    color = MaterialTheme.colorScheme.primary)
            }
            Button(
                onClick = { onPlace(customerName, notes) },
                modifier = Modifier.fillMaxWidth().height(64.dp),
                enabled = !isPlacing,
                shape = RoundedCornerShape(12.dp)
            ) {
                if (isPlacing) CircularProgressIndicator(modifier = Modifier.size(24.dp),
                    color = Color.White, strokeWidth = 3.dp)
                else Text("Place Order", fontSize = 20.sp, fontWeight = FontWeight.Bold)
            }
        }
    }
}

@Composable
private fun KioskOrderPlacedScreen(
    result:    com.tannous.pos.core.data.model.KioskOrderResultDto,
    currency:  String,
    onNewOrder:() -> Unit
) {
    Box(modifier = Modifier.fillMaxSize().background(MaterialTheme.colorScheme.primary),
        contentAlignment = Alignment.Center) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(24.dp),
            modifier = Modifier.padding(32.dp)
        ) {
            Text("✓", fontSize = 80.sp, color = Color.White)
            Text("Order Placed!", fontSize = 36.sp, fontWeight = FontWeight.Bold, color = Color.White)
            Card(modifier = Modifier.fillMaxWidth()) {
                Column(modifier = Modifier.padding(24.dp),
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    Text("Your Order Number", style = MaterialTheme.typography.titleMedium)
                    Text(result.orderNumber, fontSize = 48.sp, fontWeight = FontWeight.Black,
                        color = MaterialTheme.colorScheme.primary)
                    Text("$currency ${result.totalAmount}", fontSize = 24.sp,
                        fontWeight = FontWeight.SemiBold)
                    Text(result.message, style = MaterialTheme.typography.bodyMedium,
                        textAlign = TextAlign.Center, modifier = Modifier.padding(top = 8.dp))
                }
            }
            Button(onClick = onNewOrder, modifier = Modifier.fillMaxWidth().height(60.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = Color.White, contentColor = MaterialTheme.colorScheme.primary),
                shape = RoundedCornerShape(12.dp)) {
                Text("New Order", fontSize = 20.sp, fontWeight = FontWeight.Bold)
            }
        }
    }
}
