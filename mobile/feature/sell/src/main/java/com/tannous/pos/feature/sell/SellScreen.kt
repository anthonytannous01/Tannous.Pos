package com.tannous.pos.feature.sell

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Info
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.local.entity.CategoryEntity
import com.tannous.pos.core.data.local.entity.MenuItemEntity
import java.math.BigDecimal
import java.text.NumberFormat
import java.util.*

/**
 * Builds a currency formatter for the given ISO 4217 code (e.g. "USD", "LBP"), keeping the
 * device locale's number formatting but overriding the currency symbol. Falls back to US dollars
 * if the code is not a valid ISO 4217 currency.
 */
fun currencyFormatterFor(currencyCode: String): NumberFormat {
    return try {
        NumberFormat.getCurrencyInstance().apply {
            currency = Currency.getInstance(currencyCode)
        }
    } catch (e: Exception) {
        NumberFormat.getCurrencyInstance(Locale.US)
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SellScreen(
    onNavigateToShifts: () -> Unit,
    onNavigateToCustomers: () -> Unit,
    onNavigateToSettings: () -> Unit,
    viewModel: SellViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val selectedCategory by viewModel.selectedCategory.collectAsStateWithLifecycle()
    val cartItems by viewModel.cartItems.collectAsStateWithLifecycle()
    val finalizedOrder by viewModel.finalizedOrder.collectAsStateWithLifecycle()
    
    var showPaymentDialog by remember { mutableStateOf(false) }
    var showAddOnPicker by remember { mutableStateOf(false) }
    var pendingMenuItem by remember { mutableStateOf<MenuItemEntity?>(null) }
    
    val currencyFormatter = remember(uiState.currencyCode) { currencyFormatterFor(uiState.currencyCode) }

    fun onAddMenuItem(menuItem: MenuItemEntity) {
        if (menuItem.hasAddOns && uiState.availableAddOns.isNotEmpty()) {
            pendingMenuItem = menuItem
            showAddOnPicker = true
        } else {
            viewModel.addItemToCart(menuItem)
        }
    }
    
    // Show receipt screen if order is finalized
    finalizedOrder?.let { order ->
        ReceiptScreen(
            order = order,
            onDone = {
                viewModel.clearFinalizedOrder()
            }
        )
        return
    }
    
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Sell") },
                actions = {
                    IconButton(onClick = onNavigateToShifts) {
                        Icon(Icons.Default.Info, contentDescription = "Shifts")
                    }
                    IconButton(onClick = onNavigateToCustomers) {
                        Icon(Icons.Default.Person, contentDescription = "Customers")
                    }
                    IconButton(onClick = onNavigateToSettings) {
                        Icon(Icons.Default.Settings, contentDescription = "Settings")
                    }
                }
            )
        },
        bottomBar = {
            if (cartItems.isNotEmpty()) {
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    elevation = CardDefaults.cardElevation(defaultElevation = 8.dp)
                ) {
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(16.dp),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column {
                            Text(
                                text = "Total: ${currencyFormatter.format(uiState.cartTotal)}",
                                style = MaterialTheme.typography.headlineSmall,
                                fontWeight = FontWeight.Bold
                            )
                            Text(
                                text = "${cartItems.size} items",
                                style = MaterialTheme.typography.bodyMedium
                            )
                        }
                        Button(
                            onClick = { showPaymentDialog = true },
                            enabled = !uiState.isLoading && !uiState.isFinalizing
                        ) {
                            if (uiState.isFinalizing) {
                                CircularProgressIndicator(
                                    modifier = Modifier.size(16.dp),
                                    color = MaterialTheme.colorScheme.onPrimary
                                )
                            } else {
                                Text("Finalize Order")
                            }
                        }
                    }
                }
            }
        }
    ) { paddingValues ->
        if (uiState.isLoading) {
            Box(
                modifier = Modifier.fillMaxSize(),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator()
            }
        } else {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(paddingValues)
            ) {
                // Categories
                if (uiState.categories.isNotEmpty()) {
                    Text(
                        text = "Categories",
                        style = MaterialTheme.typography.titleMedium,
                        modifier = Modifier.padding(16.dp)
                    )
                    LazyRow(
                        contentPadding = PaddingValues(horizontal = 16.dp),
                        horizontalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        items(uiState.categories) { category ->
                            FilterChip(
                                selected = selectedCategory?.id == category.id,
                                onClick = { viewModel.selectCategory(category) },
                                label = { Text(category.name) }
                            )
                        }
                    }
                }
                
                // Menu Items
                if (selectedCategory != null) {
                    val categoryItems = uiState.menuItems.filter { it.categoryId == selectedCategory!!.id }
                    if (categoryItems.isNotEmpty()) {
                        Text(
                            text = "Menu Items",
                            style = MaterialTheme.typography.titleMedium,
                            modifier = Modifier.padding(16.dp)
                        )
                        LazyColumn(
                            modifier = Modifier.weight(1f),
                            contentPadding = PaddingValues(horizontal = 16.dp),
                            verticalArrangement = Arrangement.spacedBy(8.dp)
                        ) {
                            items(categoryItems) { menuItem ->
                                MenuItemCard(
                                    menuItem = menuItem,
                                    currencyFormatter = currencyFormatter,
                                    onAddToCart = { onAddMenuItem(menuItem) }
                                )
                            }
                        }
                    } else {
                        Box(
                            modifier = Modifier
                                .fillMaxWidth()
                                .weight(1f),
                            contentAlignment = Alignment.Center
                        ) {
                            Text(
                                text = "No items in this category",
                                style = MaterialTheme.typography.bodyLarge,
                                textAlign = TextAlign.Center
                            )
                        }
                    }
                }
                
                // Cart review (lets the cashier verify/edit the order before payment)
                Divider()
                Text(
                    text = "Cart",
                    style = MaterialTheme.typography.titleMedium,
                    modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp)
                )
                if (cartItems.isEmpty()) {
                    Text(
                        text = "Cart is empty",
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp)
                    )
                } else {
                    LazyColumn(
                        modifier = Modifier
                            .fillMaxWidth()
                            .heightIn(max = 220.dp),
                        contentPadding = PaddingValues(horizontal = 16.dp),
                        verticalArrangement = Arrangement.spacedBy(4.dp)
                    ) {
                        items(cartItems) { item ->
                            CartItemRow(
                                item = item,
                                currencyFormatter = currencyFormatter,
                                onIncrement = { viewModel.addItemToCart(item.menuItem) },
                                onDecrement = { viewModel.removeItemFromCart(item.menuItem) },
                                onRemoveLine = {
                                    // Remove the whole line via the existing decrement op (no VM change)
                                    repeat(item.quantity) { viewModel.removeItemFromCart(item.menuItem) }
                                }
                            )
                        }
                    }
                }

                // Error handling
                uiState.error?.let { error ->
                    val isNoShiftError = error.contains("active shift", ignoreCase = true)
                    Card(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(16.dp),
                        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.errorContainer)
                    ) {
                        Column(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(16.dp)
                        ) {
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    text = error,
                                    style = MaterialTheme.typography.bodyMedium,
                                    color = MaterialTheme.colorScheme.onErrorContainer,
                                    modifier = Modifier.weight(1f)
                                )
                                IconButton(onClick = { viewModel.clearError() }) {
                                    Icon(
                                        Icons.Default.Close,
                                        contentDescription = "Dismiss",
                                        tint = MaterialTheme.colorScheme.onErrorContainer
                                    )
                                }
                            }
                            if (isNoShiftError) {
                                Spacer(modifier = Modifier.height(8.dp))
                                Button(
                                    onClick = {
                                        viewModel.clearError()
                                        onNavigateToShifts()
                                    },
                                    modifier = Modifier.align(Alignment.End)
                                ) {
                                    Text("Open Shift")
                                }
                            }
                        }
                    }
                }
            }
        }
        
        if (showAddOnPicker && pendingMenuItem != null) {
            AddOnPickerDialog(
                menuItem = pendingMenuItem!!,
                availableAddOns = uiState.availableAddOns,
                currencyFormatter = currencyFormatter,
                onConfirm = { selectedAddOns ->
                    viewModel.addItemToCart(pendingMenuItem!!, selectedAddOns)
                    showAddOnPicker = false
                    pendingMenuItem = null
                },
                onDismiss = {
                    showAddOnPicker = false
                    pendingMenuItem = null
                }
            )
        }

        // Payment selection dialog
        if (showPaymentDialog) {
            PaymentSelectionDialog(
                total = BigDecimal.valueOf(uiState.cartTotal),
                currencyCode = uiState.currencyCode,
                onConfirm = { payments ->
                    showPaymentDialog = false
                    viewModel.finalizeOrder(payments)
                },
                onDismiss = { showPaymentDialog = false }
            )
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MenuItemCard(
    menuItem: MenuItemEntity,
    currencyFormatter: NumberFormat,
    onAddToCart: () -> Unit
) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        onClick = onAddToCart
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(
                modifier = Modifier.weight(1f)
            ) {
                Text(
                    text = menuItem.name,
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold
                )
                menuItem.description?.let { description ->
                    if (description.isNotEmpty()) {
                        Text(
                            text = description,
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }
            }
            Column(
                horizontalAlignment = Alignment.End
            ) {
                Text(
                    text = currencyFormatter.format(menuItem.price),
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold
                )
                IconButton(onClick = onAddToCart) {
                    Icon(Icons.Default.Add, contentDescription = "Add to cart")
                }
            }
        }
    }
}

@Composable
fun CartItemRow(
    item: CartItem,
    currencyFormatter: NumberFormat,
    onIncrement: () -> Unit,
    onDecrement: () -> Unit,
    onRemoveLine: () -> Unit
) {
    val unitAddOnsTotal = item.addOns.fold(BigDecimal.ZERO) { acc, addOn ->
        acc + BigDecimal.valueOf(addOn.price) * BigDecimal.valueOf(addOn.quantity.toLong())
    }
    val lineTotal = (item.menuItem.price + unitAddOnsTotal).multiply(BigDecimal(item.quantity))
    Column(modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = item.menuItem.name,
                    style = MaterialTheme.typography.bodyLarge,
                    fontWeight = FontWeight.SemiBold
                )
                Text(
                    text = "${currencyFormatter.format(item.menuItem.price)} each",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
            IconButton(onClick = onDecrement) {
                Text("−", style = MaterialTheme.typography.titleLarge)
            }
            Text(
                text = "${item.quantity}",
                style = MaterialTheme.typography.bodyLarge,
                textAlign = TextAlign.Center,
                modifier = Modifier.widthIn(min = 24.dp)
            )
            IconButton(onClick = onIncrement) {
                Text("+", style = MaterialTheme.typography.titleLarge)
            }
            Text(
                text = currencyFormatter.format(lineTotal),
                style = MaterialTheme.typography.bodyLarge,
                fontWeight = FontWeight.Bold
            )
            IconButton(onClick = onRemoveLine) {
                Icon(Icons.Default.Close, contentDescription = "Remove ${item.menuItem.name}")
            }
        }
        if (item.addOns.isNotEmpty()) {
            Text(
                text = item.addOns.joinToString { "+ ${it.name}" },
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(start = 4.dp, top = 2.dp)
            )
        }
    }
}
