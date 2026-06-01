package com.tannous.pos.feature.customers

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Close
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

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CustomersScreen(
    onNavigateBack: () -> Unit,
    onCustomerSelected: (CustomerEntity) -> Unit,
    viewModel: CustomersViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val snackbarHostState = remember { SnackbarHostState() }

    var firstName by remember { mutableStateOf("") }
    var lastName by remember { mutableStateOf("") }
    var phone by remember { mutableStateOf("") }
    var email by remember { mutableStateOf("") }
    var notes by remember { mutableStateOf("") }

    LaunchedEffect(uiState.error) {
        uiState.error?.let { message ->
            snackbarHostState.showSnackbar(message, duration = SnackbarDuration.Long)
            viewModel.clearError()
        }
    }

    LaunchedEffect(uiState.createSuccess) {
        if (uiState.createSuccess) {
            snackbarHostState.showSnackbar("Customer created")
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
                title = { Text("Customers") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = "Back")
                    }
                }
            )
        },
        floatingActionButton = {
            FloatingActionButton(onClick = { viewModel.showCreateDialog() }) {
                Icon(Icons.Default.Add, contentDescription = "Add customer")
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
                placeholder = { Text("Search by name or phone") },
                singleLine = true,
                trailingIcon = {
                    if (uiState.searchQuery.isNotEmpty()) {
                        IconButton(onClick = { viewModel.onSearchQueryChange("") }) {
                            Icon(Icons.Default.Close, contentDescription = "Clear search")
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
                            text = "No customers found",
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
                            text = "No customers yet",
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
                                onClick = { onCustomerSelected(customer) }
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
            title = { Text("New Customer") },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    OutlinedTextField(
                        value = firstName,
                        onValueChange = { firstName = it },
                        label = { Text("First name *") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = lastName,
                        onValueChange = { lastName = it },
                        label = { Text("Last name *") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = phone,
                        onValueChange = { phone = it },
                        label = { Text("Phone") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = email,
                        onValueChange = { email = it },
                        label = { Text("Email") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true
                    )
                    OutlinedTextField(
                        value = notes,
                        onValueChange = { notes = it },
                        label = { Text("Notes") },
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
                        Text("Create")
                    }
                }
            },
            dismissButton = {
                TextButton(onClick = { viewModel.dismissCreateDialog() }) {
                    Text("Cancel")
                }
            }
        )
    }
}

@Composable
private fun CustomerRow(
    customer: CustomerEntity,
    onClick: () -> Unit
) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .clickable(onClick = onClick)
    ) {
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
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = "${customer.firstName} ${customer.lastName}",
                    style = MaterialTheme.typography.bodyLarge,
                    fontWeight = FontWeight.Medium
                )
                Text(
                    text = customer.phone ?: customer.email ?: "No contact info",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
            AssistChip(
                onClick = onClick,
                label = { Text("${customer.totalOrders} orders") }
            )
        }
    }
}
