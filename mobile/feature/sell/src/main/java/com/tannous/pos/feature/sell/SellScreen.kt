package com.tannous.pos.feature.sell

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Info
import androidx.compose.material.icons.filled.List
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.filled.Search
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
import com.tannous.pos.core.data.local.entity.OrderEntity
import com.tannous.pos.core.data.repository.isAlreadyVoidedStatus
import com.tannous.pos.core.data.repository.isVoidableStatus
import com.tannous.pos.core.ui.LocalIsArabic
import com.tannous.pos.core.util.currencyFormatterFor
import java.math.BigDecimal
import java.text.NumberFormat

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SellScreen(
    onNavigateToShifts: () -> Unit,
    onNavigateToCustomers: () -> Unit,
    onNavigateToSettings: () -> Unit,
    onNavigateToOrderHistory: () -> Unit,
    onNavigateToSplitBill: (String) -> Unit = {},
    viewModel: SellViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val selectedCategory by viewModel.selectedCategory.collectAsStateWithLifecycle()
    val cartItems by viewModel.cartItems.collectAsStateWithLifecycle()
    val finalizedOrder by viewModel.finalizedOrder.collectAsStateWithLifecycle()

    var showPaymentDialog by remember { mutableStateOf(false) }
    var showDeliverySheet by remember { mutableStateOf(false) }
    var showAddOnPicker by remember { mutableStateOf(false) }
    var pendingMenuItem by remember { mutableStateOf<MenuItemEntity?>(null) }
    var showOrderHistory by remember { mutableStateOf(false) }
    var historyVoidDialogOrderId by remember { mutableStateOf<String?>(null) }
    var historyVoidReason by remember { mutableStateOf("") }
    val sheetState = rememberModalBottomSheetState(skipPartiallyExpanded = true)
    val snackbarHostState = remember { SnackbarHostState() }

    val currencyFormatter = remember(uiState.currencyCode) { currencyFormatterFor(uiState.currencyCode) }
    val isArabic = LocalIsArabic.current

    LaunchedEffect(uiState.historyVoidError) {
        uiState.historyVoidError?.let { error ->
            snackbarHostState.showSnackbar(error, duration = SnackbarDuration.Long)
            viewModel.clearHistoryVoidError()
        }
    }

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
            onDone = { viewModel.clearFinalizedOrder() }
        )
        return
    }

    Scaffold(
        snackbarHost = { SnackbarHost(hostState = snackbarHostState) },
        topBar = {
            TopAppBar(
                title = { Text(if (isArabic) "البيع" else "Sell") },
                actions = {
                    if (uiState.shiftOrders.isNotEmpty()) {
                        IconButton(onClick = { showOrderHistory = true }) {
                            Icon(
                                imageVector = Icons.Default.List,
                                contentDescription = if (isArabic) "طلبات الوردية" else "This shift's orders"
                            )
                        }
                    }
                    IconButton(onClick = onNavigateToOrderHistory) {
                        Icon(
                            imageVector = Icons.Default.Search,
                            contentDescription = if (isArabic) "سجل الطلبات" else "Order history"
                        )
                    }
                    IconButton(onClick = onNavigateToShifts) {
                        Icon(Icons.Default.Info, contentDescription = if (isArabic) "الورديات" else "Shifts")
                    }
                    IconButton(onClick = onNavigateToCustomers) {
                        Icon(Icons.Default.Person, contentDescription = if (isArabic) "العملاء" else "Customers")
                    }
                    IconButton(onClick = onNavigateToSettings) {
                        Icon(Icons.Default.Settings, contentDescription = if (isArabic) "الإعدادات" else "Settings")
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
                        horizontalArrangement = Arrangement.spacedBy(8.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column(modifier = Modifier.weight(1f)) {
                            Text(
                                text = if (isArabic) "الإجمالي: ${currencyFormatter.format(uiState.cartTotal)}" else "Total: ${currencyFormatter.format(uiState.cartTotal)}",
                                style = MaterialTheme.typography.headlineSmall,
                                fontWeight = FontWeight.Bold
                            )
                            Text(
                                text = if (isArabic) "${cartItems.size} عناصر" else "${cartItems.size} items",
                                style = MaterialTheme.typography.bodyMedium
                            )
                        }
                        // Split Bill is dine-in only; hide for delivery
                        if (uiState.orderType != OrderType.Delivery) {
                            OutlinedButton(
                                onClick = { viewModel.startSplitBill(onNavigateToSplitBill) },
                                enabled = !uiState.isLoading && !uiState.isFinalizing && uiState.cartTotal > 0
                            ) {
                                Text(if (isArabic) "تقسيم الفاتورة" else "Split Bill")
                            }
                        }
                        Button(
                            onClick = {
                                if (uiState.orderType == OrderType.Delivery) {
                                    // Collect address/details first, then payment
                                    showDeliverySheet = true
                                } else {
                                    showPaymentDialog = true
                                }
                            },
                            enabled = !uiState.isLoading && !uiState.isFinalizing
                        ) {
                            if (uiState.isFinalizing) {
                                CircularProgressIndicator(
                                    modifier = Modifier.size(16.dp),
                                    color = MaterialTheme.colorScheme.onPrimary
                                )
                            } else {
                                Text(when (uiState.orderType) {
                                    OrderType.Delivery -> if (isArabic) "توصيل" else "Deliver"
                                    else               -> if (isArabic) "إتمام الطلب" else "Finalize Order"
                                })
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
                // Order-type selector (Dine-In / Takeaway / Delivery)
                OrderTypeSelector(
                    selected = uiState.orderType,
                    onSelect = { viewModel.setOrderType(it) },
                    isArabic = isArabic,
                    modifier = Modifier.padding(horizontal = 16.dp, vertical = 4.dp)
                )

                // Categories / empty catalog
                if (uiState.categories.isEmpty()) {
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .weight(1f),
                        contentAlignment = Alignment.Center
                    ) {
                        Column(
                            horizontalAlignment = Alignment.CenterHorizontally,
                            verticalArrangement = Arrangement.spacedBy(12.dp),
                            modifier = Modifier.padding(32.dp)
                        ) {
                            Icon(
                                imageVector = Icons.Default.Refresh,
                                contentDescription = null,
                                modifier = Modifier.size(48.dp),
                                tint = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                            Text(
                                text = if (isArabic) "لم يتم تحميل القائمة" else "Catalog not loaded",
                                style = MaterialTheme.typography.titleMedium,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                            Text(
                                text = if (isArabic) "اتصل بالشبكة وقم بالمزامنة لتحميل القائمة." else "Connect to the network and sync to load the menu.",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                                textAlign = TextAlign.Center
                            )
                            Button(onClick = { viewModel.refreshCatalogData() }) {
                                Icon(
                                    Icons.Default.Refresh,
                                    contentDescription = null,
                                    modifier = Modifier.size(18.dp)
                                )
                                Spacer(modifier = Modifier.width(8.dp))
                                Text(if (isArabic) "مزامنة القائمة" else "Sync Catalog")
                            }
                        }
                    }
                } else {
                    Text(
                        text = if (isArabic) "الأقسام" else "Categories",
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
                                label = {
                                    Text(if (isArabic) category.nameAr?.takeIf { it.isNotBlank() } ?: category.name else category.name)
                                }
                            )
                        }
                    }
                }

                // Menu Items
                if (selectedCategory != null) {
                    val categoryItems = uiState.menuItems.filter { it.categoryId == selectedCategory!!.id }
                    if (categoryItems.isNotEmpty()) {
                        Text(
                            text = if (isArabic) "عناصر القائمة" else "Menu Items",
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
                                text = if (isArabic) "لا توجد عناصر في هذا القسم" else "No items in this category",
                                style = MaterialTheme.typography.bodyLarge,
                                textAlign = TextAlign.Center
                            )
                        }
                    }
                }

                // Cart review (lets the cashier verify/edit the order before payment)
                Divider()
                Text(
                    text = if (isArabic) "السلة" else "Cart",
                    style = MaterialTheme.typography.titleMedium,
                    modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp)
                )

                val attachedCustomer = uiState.attachedCustomer
                if (attachedCustomer == null) {
                    TextButton(
                        onClick = onNavigateToCustomers,
                        modifier = Modifier.padding(horizontal = 8.dp)
                    ) {
                        Icon(
                            imageVector = Icons.Default.Person,
                            contentDescription = null,
                            modifier = Modifier.size(18.dp)
                        )
                        Spacer(modifier = Modifier.width(4.dp))
                        Text(if (isArabic) "إضافة عميل" else "Add customer")
                    }
                } else {
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(horizontal = 16.dp, vertical = 4.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Icon(
                            imageVector = Icons.Default.Person,
                            contentDescription = null,
                            modifier = Modifier.size(20.dp)
                        )
                        Spacer(modifier = Modifier.width(8.dp))
                        Text(
                            text = "${attachedCustomer.firstName} ${attachedCustomer.lastName}",
                            style = MaterialTheme.typography.bodyMedium,
                            modifier = Modifier.weight(1f)
                        )
                        IconButton(onClick = { viewModel.detachCustomer() }) {
                            Icon(
                                imageVector = Icons.Default.Close,
                                contentDescription = if (isArabic) "إزالة العميل" else "Remove customer"
                            )
                        }
                    }
                }
                if (cartItems.isEmpty()) {
                    Text(
                        text = if (isArabic) "السلة فارغة" else "Cart is empty",
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
                                        contentDescription = if (isArabic) "إغلاق" else "Dismiss",
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
                                    Text(if (isArabic) "فتح وردية" else "Open Shift")
                                }
                            }
                        }
                    }
                }
            }
        }

        if (showOrderHistory) {
            ModalBottomSheet(
                onDismissRequest = { showOrderHistory = false },
                sheetState = sheetState
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 16.dp)
                        .padding(bottom = 32.dp)
                ) {
                    Text(
                        text = if (isArabic) "طلبات هذه الوردية" else "This Shift's Orders",
                        style = MaterialTheme.typography.titleMedium,
                        modifier = Modifier.padding(vertical = 12.dp)
                    )

                    if (uiState.shiftOrders.isEmpty()) {
                        Text(
                            text = if (isArabic) "لا توجد طلبات في هذه الوردية" else "No orders this shift",
                            style = MaterialTheme.typography.bodyMedium
                        )
                    } else {
                        LazyColumn(
                            modifier = Modifier.heightIn(max = 400.dp),
                            verticalArrangement = Arrangement.spacedBy(8.dp)
                        ) {
                            items(uiState.shiftOrders, key = { it.id }) { order ->
                                ShiftOrderRow(
                                    order = order,
                                    currencyFormatter = currencyFormatter,
                                    isVoiding = uiState.voidingOrderId == order.id,
                                    onVoidClick = {
                                        historyVoidDialogOrderId = order.id
                                        historyVoidReason = ""
                                    }
                                )
                            }
                        }
                    }
                }
            }
        }

        historyVoidDialogOrderId?.let { orderId ->
            AlertDialog(
                onDismissRequest = {
                    historyVoidDialogOrderId = null
                    historyVoidReason = ""
                },
                title = { Text(if (isArabic) "إلغاء الطلب" else "Void Order") },
                text = {
                    Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                        Text(if (isArabic) "أدخل سبب إلغاء هذا الطلب." else "Enter a reason to void this order.")
                        OutlinedTextField(
                            value = historyVoidReason,
                            onValueChange = { if (it.length <= 500) historyVoidReason = it },
                            label = { Text(if (isArabic) "السبب" else "Reason") },
                            modifier = Modifier.fillMaxWidth(),
                            minLines = 2,
                            supportingText = { Text("${historyVoidReason.length}/500") }
                        )
                    }
                },
                confirmButton = {
                    TextButton(
                        onClick = {
                            viewModel.voidShiftOrder(orderId, historyVoidReason)
                            historyVoidDialogOrderId = null
                            historyVoidReason = ""
                        },
                        enabled = historyVoidReason.isNotBlank()
                    ) {
                        Text(if (isArabic) "تأكيد الإلغاء" else "Void", color = MaterialTheme.colorScheme.error)
                    }
                },
                dismissButton = {
                    TextButton(onClick = {
                        historyVoidDialogOrderId = null
                        historyVoidReason = ""
                    }) {
                        Text(if (isArabic) "رجوع" else "Cancel")
                    }
                }
            )
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

        // Delivery details sheet — shown before payment when order type is Delivery
        if (showDeliverySheet) {
            DeliveryDetailsSheet(
                isArabic = isArabic,
                onConfirm = { details ->
                    viewModel.setDeliveryDetails(details)
                    showDeliverySheet = false
                    showPaymentDialog = true
                },
                onDismiss = { showDeliverySheet = false }
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
private fun OrderTypeSelector(
    selected: OrderType,
    onSelect: (OrderType) -> Unit,
    isArabic: Boolean,
    modifier: Modifier = Modifier
) {
    Row(
        modifier = modifier,
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        listOf(OrderType.DineIn, OrderType.Takeaway, OrderType.Delivery).forEach { type ->
            FilterChip(
                selected = selected == type,
                onClick  = { onSelect(type) },
                label    = {
                    Text(when (type) {
                        OrderType.DineIn   -> if (isArabic) "داخل" else "Dine-In"
                        OrderType.Takeaway -> if (isArabic) "تيك أواي" else "Takeaway"
                        OrderType.Delivery -> if (isArabic) "توصيل" else "Delivery"
                    })
                }
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
    val isArabic = LocalIsArabic.current
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
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = if (isArabic) menuItem.nameAr?.takeIf { it.isNotBlank() } ?: menuItem.name else menuItem.name,
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
            Column(horizontalAlignment = Alignment.End) {
                Text(
                    text = currencyFormatter.format(menuItem.price),
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold
                )
                IconButton(onClick = onAddToCart) {
                    Icon(Icons.Default.Add, contentDescription = if (isArabic) "إضافة إلى السلة" else "Add to cart")
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
    val isArabic = LocalIsArabic.current
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
                    text = if (isArabic) item.menuItem.nameAr?.takeIf { it.isNotBlank() } ?: item.menuItem.name else item.menuItem.name,
                    style = MaterialTheme.typography.bodyLarge,
                    fontWeight = FontWeight.SemiBold
                )
                Text(
                    text = if (isArabic) "${currencyFormatter.format(item.menuItem.price)} للوحدة" else "${currencyFormatter.format(item.menuItem.price)} each",
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
                Icon(Icons.Default.Close, contentDescription = if (isArabic) "إزالة العنصر" else "Remove ${item.menuItem.name}")
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

@Composable
private fun ShiftOrderRow(
    order: OrderEntity,
    currencyFormatter: NumberFormat,
    isVoiding: Boolean,
    onVoidClick: () -> Unit
) {
    val isArabic = LocalIsArabic.current
    val statusText = when {
        order.status.isAlreadyVoidedStatus() -> if (isArabic) "ملغى" else "Voided"
        order.receiptNumber?.startsWith("PENDING") == true -> if (isArabic) "في انتظار المزامنة" else "Sync pending"
        order.status.isVoidableStatus() -> if (isArabic) "مدفوع / مفتوح" else "Paid / Open"
        else -> order.status
    }

    Row(
        modifier = Modifier.fillMaxWidth(),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Column(modifier = Modifier.weight(1f)) {
            Text(
                text = order.orderNumber ?: order.id.take(8),
                style = MaterialTheme.typography.bodyMedium,
                fontWeight = FontWeight.Medium
            )
            Text(
                text = "${currencyFormatter.format(order.total)}  ·  $statusText",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }

        when {
            isVoiding -> CircularProgressIndicator(modifier = Modifier.size(20.dp))
            order.status.isAlreadyVoidedStatus() -> Unit
            order.receiptNumber?.startsWith("PENDING") == true -> {
                Text(
                    text = if (isArabic) "زامن أولاً" else "Sync first",
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
            order.status.isVoidableStatus() -> {
                TextButton(onClick = onVoidClick) {
                    Text(if (isArabic) "إلغاء" else "Void", color = MaterialTheme.colorScheme.error)
                }
            }
        }
    }
    Divider(modifier = Modifier.padding(top = 8.dp))
}
