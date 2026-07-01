package com.tannous.pos.feature.settings

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.ArrowForward
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.model.CreateReservationRequest
import com.tannous.pos.core.data.model.ReservationDto
import com.tannous.pos.core.ui.LocalIsArabic
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ReservationsScreen(
    onNavigateBack: () -> Unit,
    viewModel: ReservationsViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val isArabic = LocalIsArabic.current
    val snackbarHostState = remember { SnackbarHostState() }

    LaunchedEffect(uiState.error) {
        uiState.error?.let {
            snackbarHostState.showSnackbar(it, duration = SnackbarDuration.Short)
            viewModel.clearError()
        }
    }

    if (uiState.showCreateDialog) {
        CreateReservationDialog(
            availableTables = uiState.availableTables,
            isCreating      = uiState.isCreating,
            error           = uiState.createError,
            isArabic        = isArabic,
            onLoadTables    = { slot, size -> viewModel.loadAvailableTables(slot, size) },
            onCreate        = { viewModel.create(it) },
            onDismiss       = { viewModel.dismissCreateDialog() }
        )
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHostState) },
        topBar = {
            TopAppBar(
                title = { Text(if (isArabic) "الحجوزات" else "Reservations") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = if (isArabic) "رجوع" else "Back")
                    }
                }
            )
        },
        floatingActionButton = {
            FloatingActionButton(onClick = { viewModel.showCreateDialog() }) {
                Icon(Icons.Default.Add, contentDescription = if (isArabic) "حجز جديد" else "New reservation")
            }
        }
    ) { padding ->
        Column(
            modifier = Modifier.fillMaxSize().padding(padding)
        ) {
            // Date navigation
            Row(
                modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                IconButton(onClick = {
                    viewModel.loadForDate(uiState.selectedDate.minusDays(1))
                }) { Icon(Icons.Default.ArrowBack, contentDescription = if (isArabic) "اليوم السابق" else "Previous day") }

                Text(
                    text = uiState.selectedDate.format(DateTimeFormatter.ofPattern("EEE, dd MMM yyyy")),
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.SemiBold
                )

                IconButton(onClick = {
                    viewModel.loadForDate(uiState.selectedDate.plusDays(1))
                }) { Icon(Icons.Default.ArrowForward, contentDescription = if (isArabic) "اليوم التالي" else "Next day") }
            }

            Divider()

            when {
                uiState.isLoading -> Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    CircularProgressIndicator()
                }
                uiState.reservations.isEmpty() -> Box(
                    Modifier.fillMaxSize(), contentAlignment = Alignment.Center
                ) {
                    Column(horizontalAlignment = Alignment.CenterHorizontally) {
                        Text(if (isArabic) "لا توجد حجوزات" else "No reservations", style = MaterialTheme.typography.titleMedium)
                        Text(
                            if (isArabic) "اضغط + لإضافة حجز" else "Tap + to add one",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }
                else -> LazyColumn(
                    modifier = Modifier.fillMaxSize(),
                    contentPadding = PaddingValues(12.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    items(uiState.reservations) { res ->
                        ReservationCard(
                            reservation = res,
                            isArabic    = isArabic,
                            onConfirm   = { viewModel.updateStatus(res.id, RESERVATION_CONFIRMED) },
                            onSeat      = { viewModel.updateStatus(res.id, RESERVATION_SEATED) },
                            onCancel    = { viewModel.updateStatus(res.id, RESERVATION_CANCELLED) },
                            onNoShow    = { viewModel.updateStatus(res.id, RESERVATION_NOSHOW) }
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun ReservationCard(
    reservation: ReservationDto,
    isArabic:    Boolean,
    onConfirm:   () -> Unit,
    onSeat:      () -> Unit,
    onCancel:    () -> Unit,
    onNoShow:    () -> Unit
) {
    val timeFmt = DateTimeFormatter.ofPattern("HH:mm")
    val time = try {
        LocalDateTime.parse(reservation.reservationDateTime.take(19)).format(timeFmt)
    } catch (e: Exception) { reservation.reservationDateTime }

    val statusColor = when (reservation.status) {
        RESERVATION_CONFIRMED -> MaterialTheme.colorScheme.primary
        RESERVATION_SEATED    -> MaterialTheme.colorScheme.tertiary
        RESERVATION_CANCELLED, RESERVATION_NOSHOW -> MaterialTheme.colorScheme.error
        else -> MaterialTheme.colorScheme.onSurfaceVariant
    }

    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(reservation.customerName, fontWeight = FontWeight.SemiBold,
                    style = MaterialTheme.typography.bodyLarge)
                Text(time, style = MaterialTheme.typography.bodyLarge, fontWeight = FontWeight.Bold)
            }

            Row(horizontalArrangement = Arrangement.spacedBy(16.dp)) {
                Text(
                    if (isArabic) "${reservation.partySize} ضيوف" else "${reservation.partySize} guests",
                    style = MaterialTheme.typography.bodySmall
                )
                reservation.tableNumber?.let {
                    Text(
                        if (isArabic) "طاولة $it" else "Table $it",
                        style = MaterialTheme.typography.bodySmall
                    )
                }
                reservation.customerPhone?.let {
                    Text(it, style = MaterialTheme.typography.bodySmall)
                }
            }

            Text(
                reservation.statusName,
                style = MaterialTheme.typography.labelMedium,
                color = statusColor
            )

            reservation.notes?.let {
                Text(it, style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant)
            }

            // Action buttons — only show valid transitions
            if (reservation.status == RESERVATION_PENDING ||
                reservation.status == RESERVATION_CONFIRMED) {
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    if (reservation.status == RESERVATION_PENDING) {
                        OutlinedButton(onClick = onConfirm, modifier = Modifier.weight(1f)) {
                            Text(if (isArabic) "تأكيد" else "Confirm")
                        }
                    }
                    OutlinedButton(onClick = onSeat, modifier = Modifier.weight(1f)) {
                        Text(if (isArabic) "إجلاس" else "Seat")
                    }
                    OutlinedButton(
                        onClick = onCancel, modifier = Modifier.weight(1f),
                        colors = ButtonDefaults.outlinedButtonColors(
                            contentColor = MaterialTheme.colorScheme.error)
                    ) { Text(if (isArabic) "إلغاء" else "Cancel") }
                    if (reservation.status == RESERVATION_CONFIRMED) {
                        OutlinedButton(onClick = onNoShow, modifier = Modifier.weight(1f)) {
                            Text(if (isArabic) "لم يحضر" else "No Show")
                        }
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun CreateReservationDialog(
    availableTables: List<com.tannous.pos.core.data.model.AvailableTableDto>,
    isCreating:      Boolean,
    error:           String?,
    isArabic:        Boolean,
    onLoadTables:    (slot: String, partySize: Int) -> Unit,
    onCreate:        (CreateReservationRequest) -> Unit,
    onDismiss:       () -> Unit
) {
    var name      by remember { mutableStateOf("") }
    var phone     by remember { mutableStateOf("") }
    var partySize by remember { mutableStateOf("2") }
    var date      by remember { mutableStateOf(LocalDate.now().plusDays(1).toString()) }
    var time      by remember { mutableStateOf("19:00") }
    var notes     by remember { mutableStateOf("") }
    var tableId   by remember { mutableStateOf<String?>(null) }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(if (isArabic) "حجز جديد" else "New Reservation") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                OutlinedTextField(value = name, onValueChange = { name = it },
                    label = { Text(if (isArabic) "اسم العميل *" else "Customer Name *") },
                    modifier = Modifier.fillMaxWidth(), singleLine = true)
                OutlinedTextField(value = phone, onValueChange = { phone = it },
                    label = { Text(if (isArabic) "الهاتف" else "Phone") },
                    modifier = Modifier.fillMaxWidth(), singleLine = true)
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    OutlinedTextField(value = partySize, onValueChange = { partySize = it },
                        label = { Text(if (isArabic) "الضيوف" else "Guests") },
                        modifier = Modifier.weight(1f), singleLine = true)
                    OutlinedTextField(value = time, onValueChange = { time = it },
                        label = { Text(if (isArabic) "الوقت (HH:mm)" else "Time (HH:mm)") },
                        modifier = Modifier.weight(1f), singleLine = true)
                }
                OutlinedTextField(value = date, onValueChange = { date = it },
                    label = { Text(if (isArabic) "التاريخ (YYYY-MM-DD)" else "Date (YYYY-MM-DD)") },
                    modifier = Modifier.fillMaxWidth(), singleLine = true)

                // Check availability button
                OutlinedButton(
                    onClick = {
                        val slot = "${date}T${time}:00Z"
                        onLoadTables(slot, partySize.toIntOrNull() ?: 2)
                    },
                    modifier = Modifier.fillMaxWidth()
                ) { Text(if (isArabic) "التحقق من الطاولات المتاحة" else "Check Available Tables") }

                // Table picker
                if (availableTables.isNotEmpty()) {
                    Text(if (isArabic) "اختر طاولة:" else "Select table:", style = MaterialTheme.typography.labelMedium)
                    availableTables.forEach { t ->
                        FilterChip(
                            selected  = tableId == t.id,
                            onClick   = { tableId = if (tableId == t.id) null else t.id },
                            label     = { Text("${t.tableNumber} (${t.floorPlan}, cap ${t.capacity})") }
                        )
                    }
                }

                OutlinedTextField(value = notes, onValueChange = { notes = it },
                    label = { Text(if (isArabic) "ملاحظات" else "Notes") },
                    modifier = Modifier.fillMaxWidth(), minLines = 2)

                error?.let {
                    Text(it, color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodySmall)
                }
            }
        },
        confirmButton = {
            TextButton(
                onClick = {
                    val slot = "${date}T${time}:00Z"
                    onCreate(CreateReservationRequest(
                        customerName        = name,
                        customerPhone       = phone.takeIf { it.isNotBlank() },
                        partySize           = partySize.toIntOrNull() ?: 2,
                        reservationDateTime = slot,
                        notes               = notes.takeIf { it.isNotBlank() },
                        tableId             = tableId
                    ))
                },
                enabled = name.isNotBlank() && !isCreating
            ) {
                if (isCreating) CircularProgressIndicator(modifier = Modifier.size(16.dp), strokeWidth = 2.dp)
                else Text(if (isArabic) "إنشاء" else "Create")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) { Text(if (isArabic) "إلغاء" else "Cancel") }
        }
    )
}
