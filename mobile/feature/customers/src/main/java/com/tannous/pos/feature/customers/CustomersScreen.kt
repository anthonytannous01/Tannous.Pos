package com.tannous.pos.feature.customers

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material.icons.filled.Person
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.local.entity.CustomerEntity
import com.tannous.pos.core.ui.LocalIsArabic

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CustomersScreen(
    onNavigateBack: () -> Unit,
    onCustomerSelected: (CustomerEntity) -> Unit,
    viewModel: CustomersViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val isArabic = LocalIsArabic.current
    val snackbarHostState = remember { SnackbarHostState() }

    var firstName by remember { mutableStateOf("") }
    var lastName by remember { mutableStateOf("") }
    var phone by remember { mutableStateOf("") }
    var email by remember { mutableStateOf("") }
    var notes by remember { mutableStateOf("") }

    var editFirstName by remember { mutableStateOf("") }
    var editLastName by remember { mutableStateOf("") }
    var editPhone by remember { mutableStateOf("") }
    var editEmail by remember { mutableStateOf("") }
    var editNotes by remember { mutableStateOf("") }
    var editAllergies by remember { mutableStateOf("") }

    LaunchedEffect(uiState.editingCustomer?.id) {
        uiState.editingCustomer?.let { customer ->
            editFirstName = customer.firstName
            editLastName = customer.lastName
            editPhone = customer.phone.orEmpty()
            editEmail = customer.email.orEmpty()
            editNotes = customer.notes.orEmpty()
            editAllergies = customer.allergies.orEmpty()
        }
    }

    LaunchedEffect(uiState.error) {
        uiState.error?.let { message ->
            snackbarHostState.showSnackbar(message, duration = SnackbarDuration.Long)
            viewModel.clearError()
        }
    }

    LaunchedEffect(uiState.createSuccess) {
        if (uiState.createSuccess) {
            snackbarHostState.showSnackbar(if (isArabic) "تم إنشاء العميل" else "Customer created")
            firstName = ""
            lastName = ""
            phone = ""
            email = ""
            notes = ""
        }
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHostState) },
        topBar = {
            TopAppBar(
                title = { Text(if (isArabic) "العملاء" else "Customers") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = if (isArabic) "رجوع" else "Back")
                    }
                }
            )
        },
        floatingActionButton = {
            FloatingActionButton(onClick = { viewModel.showCreateDialog() }) {
                Icon(Icons.Default.Add, contentDescription = if (isArabic) "إضافة عميل" else "Add customer")
            }
        }
    ) { paddingValues ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
        ) {
            OutlinedTextField(
                value = uiState.searchQuery,
                onValueChange = viewModel::onSearchQueryChange,
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp, vertical = 8.dp),
                placeholder = { Text(if (isArabic) "ابحث بالاسم أو الهاتف" else "Search by name or phone") },
                singleLine = true,
                trailingIcon = {
                    if (uiState.searchQuery.isNotEmpty()) {
                        IconButton(onClick = { viewModel.onSearchQueryChange("") }) {
                            Icon(Icons.Default.Close, contentDescription = if (isArabic) "مسح البحث" else "Clear search")
                        }
                    }
                }
            )

            when {
                uiState.customers.isEmpty() && uiState.searchQuery.isNotBlank() -> {
                    Box(
                        modifier = Modifier
                            .fillMaxSize()
                            .padding(32.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        Text(
                            text = if (isArabic) "لا يوجد عملاء" else "No customers found",
                            style = MaterialTheme.typography.bodyLarge
                        )
                    }
                }
                uiState.customers.isEmpty() -> {
                    Box(
                        modifier = Modifier
                            .fillMaxSize()
                            .padding(32.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        Text(
                            text = if (isArabic) "لا يوجد عملاء بعد" else "No customers yet",
                            style = MaterialTheme.typography.bodyLarge
                        )
                    }
                }
                else -> {
                    LazyColumn(
                        modifier = Modifier.fillMaxSize(),
                        contentPadding = PaddingValues(horizontal = 16.dp, vertical = 8.dp),
                        verticalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        items(uiState.customers, key = { it.id }) { customer ->
                            CustomerRow(
                                customer = customer,
                                isArabic = isArabic,
                                onClick = { onCustomerSelected(customer) },
                                onEditClick = { viewModel.startEdit(it) }
                            )
                        }
                    }
                }
            }
        }
    }

    if (uiState.showCreateDialog) {
        AlertDialog(
            onDismissRequest = { viewModel.dismissCreateDialog() },
            title = { Text(if (isArabic) "عميل جديد" else "New Customer") },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    OutlinedTextField(
                        value = firstName,
                        onValueChange = { firstName = it },
                        label = { Text(if (isArabic) "الاسم الأول *" else "First name *") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = lastName,
                        onValueChange = { lastName = it },
                        label = { Text(if (isArabic) "اسم العائلة *" else "Last name *") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = phone,
                        onValueChange = { phone = it },
                        label = { Text(if (isArabic) "الهاتف" else "Phone") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = email,
                        onValueChange = { email = it },
                        label = { Text(if (isArabic) "البريد الإلكتروني" else "Email") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = notes,
                        onValueChange = { notes = it },
                        label = { Text(if (isArabic) "ملاحظات" else "Notes") },
                        modifier = Modifier.fillMaxWidth(),
                        minLines = 2
                    )
                }
            },
            confirmButton = {
                TextButton(
                    onClick = {
                        viewModel.createCustomer(
                            firstName = firstName,
                            lastName = lastName,
                            email = email.takeIf { it.isNotBlank() },
                            phone = phone.takeIf { it.isNotBlank() },
                            address = null,
                            notes = notes.takeIf { it.isNotBlank() },
                            allergies = null
                        )
                    },
                    enabled = firstName.isNotBlank() && lastName.isNotBlank() && !uiState.isLoading
                ) {
                    if (uiState.isLoading) {
                        CircularProgressIndicator(modifier = Modifier.size(16.dp))
                    } else {
                        Text(if (isArabic) "إنشاء" else "Create")
                    }
                }
            },
            dismissButton = {
                TextButton(onClick = { viewModel.dismissCreateDialog() }) {
                    Text(if (isArabic) "إلغاء" else "Cancel")
                }
            }
        )
    }

    uiState.editingCustomer?.let {
        AlertDialog(
            onDismissRequest = { viewModel.dismissEdit() },
            title = { Text(if (isArabic) "تعديل العميل" else "Edit Customer") },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    OutlinedTextField(
                        value = editFirstName,
                        onValueChange = { editFirstName = it },
                        label = { Text(if (isArabic) "الاسم الأول *" else "First name *") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = editLastName,
                        onValueChange = { editLastName = it },
                        label = { Text(if (isArabic) "اسم العائلة *" else "Last name *") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = editPhone,
                        onValueChange = { editPhone = it },
                        label = { Text(if (isArabic) "الهاتف" else "Phone") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = editEmail,
                        onValueChange = { editEmail = it },
                        label = { Text(if (isArabic) "البريد الإلكتروني" else "Email") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = editNotes,
                        onValueChange = { editNotes = it },
                        label = { Text(if (isArabic) "ملاحظات" else "Notes") },
                        modifier = Modifier.fillMaxWidth(),
                        minLines = 2
                    )
                    OutlinedTextField(
                        value = editAllergies,
                        onValueChange = { editAllergies = it },
                        label = { Text(if (isArabic) "الحساسية" else "Allergies") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true
                    )
                    uiState.updateError?.let { error ->
                        Text(
                            text = error,
                            color = MaterialTheme.colorScheme.error,
                            style = MaterialTheme.typography.bodySmall
                        )
                        TextButton(onClick = { viewModel.dismissEdit() }) {
                            Text(if (isArabic) "إغلاق" else "Dismiss")
                        }
                    }
                }
            },
            confirmButton = {
                TextButton(
                    onClick = {
                        viewModel.updateCustomer(
                            firstName = editFirstName,
                            lastName = editLastName,
                            email = editEmail.takeIf { it.isNotBlank() },
                            phone = editPhone.takeIf { it.isNotBlank() },
                            address = null,
                            notes = editNotes.takeIf { it.isNotBlank() },
                            allergies = editAllergies.takeIf { it.isNotBlank() }
                        )
                    },
                    enabled = editFirstName.isNotBlank() &&
                        editLastName.isNotBlank() &&
                        !uiState.isUpdating
                ) {
                    if (uiState.isUpdating) {
                        CircularProgressIndicator(modifier = Modifier.size(16.dp))
                    } else {
                        Text(if (isArabic) "حفظ" else "Save")
                    }
                }
            },
            dismissButton = {
                TextButton(onClick = { viewModel.dismissEdit() }) {
                    Text(if (isArabic) "إلغاء" else "Cancel")
                }
            }
        )
    }
}

@Composable
private fun CustomerRow(
    customer: CustomerEntity,
    isArabic: Boolean,
    onClick: () -> Unit,
    onEditClick: (CustomerEntity) -> Unit
) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(12.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Icon(
                imageVector = Icons.Default.Person,
                contentDescription = null,
                modifier = Modifier.padding(end = 12.dp)
            )
            Column(
                modifier = Modifier
                    .weight(1f)
                    .clickable(onClick = onClick)
            ) {
                Text(
                    text = "${customer.firstName} ${customer.lastName}",
                    style = MaterialTheme.typography.bodyLarge,
                    fontWeight = FontWeight.Medium
                )
                Text(
                    text = customer.phone ?: customer.email ?: if (isArabic) "لا توجد معلومات تواصل" else "No contact info",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
            AssistChip(
                onClick = onClick,
                label = { Text(if (isArabic) "${customer.totalOrders} طلبات" else "${customer.totalOrders} orders") }
            )
            if (customer.version != null) {
                IconButton(onClick = { onEditClick(customer) }) {
                    Icon(Icons.Default.Edit, contentDescription = if (isArabic) "تعديل العميل" else "Edit customer")
                }
            }
        }
    }
}
