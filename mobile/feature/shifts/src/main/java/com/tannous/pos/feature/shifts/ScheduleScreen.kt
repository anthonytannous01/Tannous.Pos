package com.tannous.pos.feature.shifts

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.KeyboardArrowLeft
import androidx.compose.material.icons.filled.KeyboardArrowRight
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.model.EmployeeScheduleDto
import com.tannous.pos.core.data.model.TimeEntryDto
import com.tannous.pos.core.data.model.UserDto
import com.tannous.pos.core.ui.LocalIsArabic
import kotlinx.coroutines.delay
import java.time.DayOfWeek
import java.time.Duration
import java.time.Instant
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.LocalTime
import java.time.OffsetDateTime
import java.time.ZoneId
import java.time.ZoneOffset
import java.time.format.DateTimeFormatter

private val timeFormatter: DateTimeFormatter = DateTimeFormatter.ofPattern("HH:mm")
private val dateFormatter: DateTimeFormatter = DateTimeFormatter.ofPattern("EEE MMM d")
private val isoInstant: DateTimeFormatter = DateTimeFormatter.ISO_INSTANT

/** Backend timestamps are UTC; tolerate both offset ("...Z") and plain ISO forms. */
private fun parseUtc(value: String): LocalDateTime? = try {
    OffsetDateTime.parse(value).toLocalDateTime()
} catch (_: Exception) {
    try { LocalDateTime.parse(value) } catch (_: Exception) { null }
}

private fun dayName(day: DayOfWeek, isArabic: Boolean): String = if (isArabic) when (day) {
    DayOfWeek.MONDAY    -> "الإثنين"
    DayOfWeek.TUESDAY   -> "الثلاثاء"
    DayOfWeek.WEDNESDAY -> "الأربعاء"
    DayOfWeek.THURSDAY  -> "الخميس"
    DayOfWeek.FRIDAY    -> "الجمعة"
    DayOfWeek.SATURDAY  -> "السبت"
    DayOfWeek.SUNDAY    -> "الأحد"
} else day.getDisplayName(java.time.format.TextStyle.FULL, java.util.Locale.ENGLISH)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ScheduleScreen(
    onNavigateBack: () -> Unit,
    viewModel: ScheduleViewModel = hiltViewModel()
) {
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val isArabic = LocalIsArabic.current
    val snackbarHostState = remember { SnackbarHostState() }
    var selectedTab by remember { mutableStateOf(0) }
    var showAddShiftSheet by remember { mutableStateOf(false) }

    LaunchedEffect(uiState.error) {
        uiState.error?.let {
            snackbarHostState.showSnackbar(it, duration = SnackbarDuration.Short)
            viewModel.clearError()
        }
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbarHostState) },
        topBar = {
            TopAppBar(
                title = { Text(if (isArabic) "جدول الموظفين" else "Staff Schedule") },
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = if (isArabic) "رجوع" else "Back")
                    }
                }
            )
        },
        floatingActionButton = {
            // Show FAB only on the Schedule tab
            if (selectedTab == 0) {
                FloatingActionButton(onClick = { showAddShiftSheet = true }) {
                    Icon(Icons.Default.Add, contentDescription = if (isArabic) "إضافة مناوبة" else "Add Shift")
                }
            }
        }
    ) { padding ->
        Column(Modifier.fillMaxSize().padding(padding)) {
            TabRow(selectedTabIndex = selectedTab) {
                Tab(
                    selected = selectedTab == 0,
                    onClick = { selectedTab = 0 },
                    text = { Text(if (isArabic) "الجدول" else "Schedule") }
                )
                Tab(
                    selected = selectedTab == 1,
                    onClick = { selectedTab = 1 },
                    text = { Text(if (isArabic) "ساعة الحضور" else "Time Clock") }
                )
            }

            when (selectedTab) {
                0 -> ScheduleTab(
                    uiState = uiState,
                    isArabic = isArabic,
                    onPreviousWeek = { viewModel.previousWeek() },
                    onNextWeek = { viewModel.nextWeek() },
                    onCancelShift = { viewModel.cancelShift(it) },
                    onPublishDrafts = { viewModel.publishDrafts() }
                )
                1 -> TimeClockTab(
                    uiState = uiState,
                    isArabic = isArabic,
                    onClockIn = { viewModel.clockIn() },
                    onClockOut = { breakMin -> viewModel.clockOut(breakMin) }
                )
            }
        }
    }

    // Add Shift bottom sheet
    if (showAddShiftSheet) {
        AddShiftSheet(
            users = uiState.users,
            isArabic = isArabic,
            isSaving = uiState.isSaving,
            onConfirm = { userId, start, end, position ->
                viewModel.createShift(
                    userId = userId,
                    scheduledStart = start,
                    scheduledEnd = end,
                    position = position
                )
                showAddShiftSheet = false
            },
            onDismiss = { showAddShiftSheet = false }
        )
    }
}

// ─── Schedule tab ────────────────────────────────────────────────────────────

@Composable
private fun ScheduleTab(
    uiState: ScheduleUiState,
    isArabic: Boolean,
    onPreviousWeek: () -> Unit,
    onNextWeek: () -> Unit,
    onCancelShift: (String) -> Unit,
    onPublishDrafts: () -> Unit
) {
    val weekStart = uiState.currentWeekStart
    val weekEnd = weekStart.plusDays(6)

    Column(Modifier.fillMaxSize()) {
        // Week navigator
        Row(
            Modifier.fillMaxWidth().padding(horizontal = 8.dp, vertical = 4.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            IconButton(onClick = onPreviousWeek) {
                Icon(Icons.Default.KeyboardArrowLeft, contentDescription = if (isArabic) "الأسبوع السابق" else "Previous week")
            }
            Text(
                "${weekStart.format(dateFormatter)} – ${weekEnd.format(dateFormatter)}",
                style = MaterialTheme.typography.titleSmall,
                fontWeight = FontWeight.SemiBold
            )
            IconButton(onClick = onNextWeek) {
                Icon(Icons.Default.KeyboardArrowRight, contentDescription = if (isArabic) "الأسبوع التالي" else "Next week")
            }
        }

        // Publish Drafts button — visible only when there are draft shifts
        if (uiState.hasDrafts) {
            Button(
                onClick = onPublishDrafts,
                enabled = !uiState.isSaving,
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 12.dp, vertical = 4.dp)
            ) {
                if (uiState.isSaving) {
                    CircularProgressIndicator(
                        modifier = Modifier.size(16.dp),
                        strokeWidth = 2.dp,
                        color = MaterialTheme.colorScheme.onPrimary
                    )
                    Spacer(Modifier.width(8.dp))
                }
                Text(if (isArabic) "نشر المسودات" else "Publish Drafts")
            }
        }

        val schedules = uiState.weeklySchedule?.schedules.orEmpty()

        when {
            uiState.isLoading && uiState.weeklySchedule == null -> Box(
                Modifier.fillMaxSize(), contentAlignment = Alignment.Center
            ) { CircularProgressIndicator() }

            schedules.isEmpty() -> Box(
                Modifier.fillMaxSize(), contentAlignment = Alignment.Center
            ) {
                Text(
                    if (isArabic) "لا يوجد جدول منشور لهذا الأسبوع."
                    else "No schedule published for this week.",
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            else -> {
                val byDate = schedules.groupBy { parseUtc(it.scheduledStart)?.toLocalDate() }
                LazyColumn(
                    Modifier.fillMaxSize(),
                    contentPadding = PaddingValues(start = 12.dp, end = 12.dp, top = 4.dp, bottom = 80.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    (0..6).forEach { offset ->
                        val date = weekStart.plusDays(offset.toLong())
                        val daySchedules = byDate[date].orEmpty()
                        item(key = "header-$date") {
                            DayHeader(date = date, isArabic = isArabic)
                        }
                        if (daySchedules.isEmpty()) {
                            item(key = "empty-$date") {
                                Text(
                                    if (isArabic) "— لا توجد مناوبات مجدولة"
                                    else "— No shifts scheduled",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                                    modifier = Modifier.padding(start = 8.dp, bottom = 4.dp)
                                )
                            }
                        } else {
                            items(daySchedules, key = { it.id }) { schedule ->
                                ScheduleCard(
                                    schedule = schedule,
                                    isArabic = isArabic,
                                    onCancel = if (schedule.status.equals("Draft", ignoreCase = true))
                                        { { onCancelShift(schedule.id) } }
                                    else null
                                )
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun DayHeader(date: LocalDate, isArabic: Boolean) {
    Text(
        "${dayName(date.dayOfWeek, isArabic)} · ${date.format(DateTimeFormatter.ofPattern("MMM d"))}",
        style = MaterialTheme.typography.titleSmall,
        fontWeight = FontWeight.Bold,
        color = MaterialTheme.colorScheme.primary,
        modifier = Modifier.padding(top = 8.dp)
    )
}

@Composable
private fun ScheduleCard(
    schedule: EmployeeScheduleDto,
    isArabic: Boolean,
    onCancel: (() -> Unit)? = null
) {
    val start = parseUtc(schedule.scheduledStart)
    val end = parseUtc(schedule.scheduledEnd)
    val timeRange = if (start != null && end != null)
        "${start.format(timeFormatter)} – ${end.format(timeFormatter)}"
    else "—"
    val hours = schedule.durationMinutes / 60
    val minutes = schedule.durationMinutes % 60

    Card(Modifier.fillMaxWidth()) {
        Row(
            Modifier.fillMaxWidth().padding(12.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(Modifier.weight(1f), verticalArrangement = Arrangement.spacedBy(2.dp)) {
                Row(
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        schedule.userFullName,
                        style = MaterialTheme.typography.bodyMedium,
                        fontWeight = FontWeight.Medium
                    )
                    Surface(
                        color = MaterialTheme.colorScheme.secondaryContainer,
                        shape = MaterialTheme.shapes.small
                    ) {
                        Text(
                            schedule.userRole,
                            style = MaterialTheme.typography.labelSmall,
                            modifier = Modifier.padding(horizontal = 6.dp, vertical = 2.dp)
                        )
                    }
                    if (schedule.status.equals("Draft", ignoreCase = true)) {
                        Surface(
                            color = MaterialTheme.colorScheme.surfaceVariant,
                            shape = MaterialTheme.shapes.small
                        ) {
                            Text(
                                if (isArabic) "مسودة" else "Draft",
                                style = MaterialTheme.typography.labelSmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                                modifier = Modifier.padding(horizontal = 6.dp, vertical = 2.dp)
                            )
                        }
                    }
                }
                Text(
                    listOfNotNull(timeRange, schedule.position?.takeIf { it.isNotBlank() })
                        .joinToString("  ·  "),
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            Row(
                horizontalArrangement = Arrangement.spacedBy(4.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    if (minutes == 0) "${hours}h" else "${hours}h ${minutes}m",
                    style = MaterialTheme.typography.labelLarge,
                    fontWeight = FontWeight.Bold
                )
                if (onCancel != null) {
                    IconButton(onClick = onCancel, modifier = Modifier.size(32.dp)) {
                        Icon(
                            Icons.Default.Close,
                            contentDescription = if (isArabic) "حذف المناوبة" else "Delete shift",
                            tint = MaterialTheme.colorScheme.error
                        )
                    }
                }
            }
        }
    }
}

// ─── Add Shift sheet ─────────────────────────────────────────────────────────

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun AddShiftSheet(
    users: List<UserDto>,
    isArabic: Boolean,
    isSaving: Boolean,
    onConfirm: (userId: String, start: String, end: String, position: String?) -> Unit,
    onDismiss: () -> Unit
) {
    val sheetState = rememberModalBottomSheetState(skipPartiallyExpanded = true)

    // Form state
    var selectedUser by remember { mutableStateOf<UserDto?>(null) }
    var employeeExpanded by remember { mutableStateOf(false) }
    var selectedDate by remember { mutableStateOf(LocalDate.now()) }
    var startHour by remember { mutableIntStateOf(8) }
    var startMinute by remember { mutableIntStateOf(0) }
    var endHour by remember { mutableIntStateOf(16) }
    var endMinute by remember { mutableIntStateOf(0) }
    var position by remember { mutableStateOf("") }

    // Dialog visibility
    var showDatePicker by remember { mutableStateOf(false) }
    var showStartTimePicker by remember { mutableStateOf(false) }
    var showEndTimePicker by remember { mutableStateOf(false) }

    // Date picker state
    val datePickerState = rememberDatePickerState(
        initialSelectedDateMillis = selectedDate
            .atStartOfDay(ZoneOffset.UTC).toInstant().toEpochMilli()
    )
    val startTimePickerState = rememberTimePickerState(
        initialHour = startHour, initialMinute = startMinute, is24Hour = true
    )
    val endTimePickerState = rememberTimePickerState(
        initialHour = endHour, initialMinute = endMinute, is24Hour = true
    )

    val canConfirm = selectedUser != null

    fun buildUtcString(date: LocalDate, hour: Int, minute: Int): String =
        LocalDateTime.of(date, LocalTime.of(hour, minute))
            .atZone(ZoneId.systemDefault())
            .withZoneSameInstant(ZoneOffset.UTC)
            .format(isoInstant)

    ModalBottomSheet(
        onDismissRequest = onDismiss,
        sheetState = sheetState
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 16.dp)
                .padding(bottom = 32.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Text(
                text = if (isArabic) "إضافة مناوبة" else "Add Shift",
                style = MaterialTheme.typography.titleLarge,
                modifier = Modifier.padding(vertical = 4.dp)
            )

            // Employee picker
            ExposedDropdownMenuBox(
                expanded = employeeExpanded,
                onExpandedChange = { employeeExpanded = it }
            ) {
                OutlinedTextField(
                    value = selectedUser?.let { "${it.firstName} ${it.lastName}".trim().ifBlank { it.username } }
                        ?: "",
                    onValueChange = {},
                    readOnly = true,
                    label = { Text(if (isArabic) "الموظف *" else "Employee *") },
                    placeholder = { Text(if (isArabic) "اختر موظفاً" else "Select employee") },
                    trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = employeeExpanded) },
                    modifier = Modifier
                        .fillMaxWidth()
                        .menuAnchor()
                )
                ExposedDropdownMenu(
                    expanded = employeeExpanded,
                    onDismissRequest = { employeeExpanded = false }
                ) {
                    if (users.isEmpty()) {
                        DropdownMenuItem(
                            text = { Text(if (isArabic) "لا يوجد موظفون" else "No employees found") },
                            onClick = { employeeExpanded = false },
                            enabled = false
                        )
                    } else {
                        users.forEach { user ->
                            DropdownMenuItem(
                                text = {
                                    Column {
                                        Text(
                                            "${user.firstName} ${user.lastName}".trim().ifBlank { user.username },
                                            style = MaterialTheme.typography.bodyMedium
                                        )
                                        Text(
                                            user.role,
                                            style = MaterialTheme.typography.labelSmall,
                                            color = MaterialTheme.colorScheme.onSurfaceVariant
                                        )
                                    }
                                },
                                onClick = {
                                    selectedUser = user
                                    employeeExpanded = false
                                }
                            )
                        }
                    }
                }
            }

            // Date field
            OutlinedTextField(
                value = selectedDate.format(DateTimeFormatter.ofPattern("EEE, MMM d yyyy")),
                onValueChange = {},
                readOnly = true,
                label = { Text(if (isArabic) "التاريخ" else "Date") },
                modifier = Modifier.fillMaxWidth(),
                trailingIcon = {
                    TextButton(onClick = { showDatePicker = true }) {
                        Text(if (isArabic) "تغيير" else "Change")
                    }
                }
            )

            // Start / End time row
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                OutlinedTextField(
                    value = LocalTime.of(startHour, startMinute).format(timeFormatter),
                    onValueChange = {},
                    readOnly = true,
                    label = { Text(if (isArabic) "وقت البدء" else "Start") },
                    modifier = Modifier
                        .weight(1f),
                    trailingIcon = {
                        TextButton(onClick = { showStartTimePicker = true }) {
                            Text(if (isArabic) "تغيير" else "Edit")
                        }
                    }
                )
                OutlinedTextField(
                    value = LocalTime.of(endHour, endMinute).format(timeFormatter),
                    onValueChange = {},
                    readOnly = true,
                    label = { Text(if (isArabic) "وقت الانتهاء" else "End") },
                    modifier = Modifier
                        .weight(1f),
                    trailingIcon = {
                        TextButton(onClick = { showEndTimePicker = true }) {
                            Text(if (isArabic) "تغيير" else "Edit")
                        }
                    }
                )
            }

            // Optional position
            OutlinedTextField(
                value = position,
                onValueChange = { position = it },
                label = { Text(if (isArabic) "المنصب (اختياري)" else "Position (optional)") },
                placeholder = { Text(if (isArabic) "مثال: كاشير" else "e.g. Cashier") },
                singleLine = true,
                modifier = Modifier.fillMaxWidth()
            )

            Button(
                onClick = {
                    val user = selectedUser ?: return@Button
                    onConfirm(
                        user.id,
                        buildUtcString(selectedDate, startHour, startMinute),
                        buildUtcString(selectedDate, endHour, endMinute),
                        position.trim().ifBlank { null }
                    )
                },
                enabled = canConfirm && !isSaving,
                modifier = Modifier.fillMaxWidth()
            ) {
                if (isSaving) {
                    CircularProgressIndicator(
                        modifier = Modifier.size(16.dp),
                        strokeWidth = 2.dp,
                        color = MaterialTheme.colorScheme.onPrimary
                    )
                    Spacer(Modifier.width(8.dp))
                }
                Text(if (isArabic) "إنشاء المناوبة" else "Create Shift")
            }

            OutlinedButton(onClick = onDismiss, modifier = Modifier.fillMaxWidth()) {
                Text(if (isArabic) "إلغاء" else "Cancel")
            }
        }
    }

    // Date picker dialog
    if (showDatePicker) {
        DatePickerDialog(
            onDismissRequest = { showDatePicker = false },
            confirmButton = {
                TextButton(onClick = {
                    datePickerState.selectedDateMillis?.let { millis ->
                        selectedDate = Instant.ofEpochMilli(millis)
                            .atZone(ZoneOffset.UTC).toLocalDate()
                    }
                    showDatePicker = false
                }) { Text(if (isArabic) "تأكيد" else "OK") }
            },
            dismissButton = {
                TextButton(onClick = { showDatePicker = false }) {
                    Text(if (isArabic) "إلغاء" else "Cancel")
                }
            }
        ) {
            DatePicker(state = datePickerState)
        }
    }

    // Start time picker dialog
    if (showStartTimePicker) {
        AlertDialog(
            onDismissRequest = { showStartTimePicker = false },
            title = { Text(if (isArabic) "وقت البدء" else "Start time") },
            text = { TimePicker(state = startTimePickerState) },
            confirmButton = {
                TextButton(onClick = {
                    startHour = startTimePickerState.hour
                    startMinute = startTimePickerState.minute
                    showStartTimePicker = false
                }) { Text(if (isArabic) "تأكيد" else "OK") }
            },
            dismissButton = {
                TextButton(onClick = { showStartTimePicker = false }) {
                    Text(if (isArabic) "إلغاء" else "Cancel")
                }
            }
        )
    }

    // End time picker dialog
    if (showEndTimePicker) {
        AlertDialog(
            onDismissRequest = { showEndTimePicker = false },
            title = { Text(if (isArabic) "وقت الانتهاء" else "End time") },
            text = { TimePicker(state = endTimePickerState) },
            confirmButton = {
                TextButton(onClick = {
                    endHour = endTimePickerState.hour
                    endMinute = endTimePickerState.minute
                    showEndTimePicker = false
                }) { Text(if (isArabic) "تأكيد" else "OK") }
            },
            dismissButton = {
                TextButton(onClick = { showEndTimePicker = false }) {
                    Text(if (isArabic) "إلغاء" else "Cancel")
                }
            }
        )
    }
}

// ─── Time Clock tab ──────────────────────────────────────────────────────────

@Composable
private fun TimeClockTab(
    uiState: ScheduleUiState,
    isArabic: Boolean,
    onClockIn: () -> Unit,
    onClockOut: (Int?) -> Unit
) {
    var breakMinutesInput by remember { mutableStateOf("") }

    // Re-render the elapsed label every minute while clocked in.
    var minuteTick by remember { mutableStateOf(0) }
    LaunchedEffect(uiState.myClockStatus?.id) {
        while (uiState.myClockStatus != null) {
            delay(60_000)
            minuteTick++
        }
    }

    LazyColumn(
        Modifier.fillMaxSize(),
        contentPadding = PaddingValues(12.dp),
        verticalArrangement = Arrangement.spacedBy(10.dp)
    ) {
        item {
            Card(
                Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(
                    containerColor = if (uiState.myClockStatus != null)
                        MaterialTheme.colorScheme.errorContainer.copy(alpha = 0.35f)
                    else MaterialTheme.colorScheme.primaryContainer.copy(alpha = 0.35f)
                )
            ) {
                Column(
                    Modifier.fillMaxWidth().padding(16.dp),
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    when {
                        uiState.isClockBusy -> CircularProgressIndicator()

                        uiState.myClockStatus == null -> {
                            Text(
                                if (isArabic) "لم تسجل الحضور بعد" else "Not clocked in",
                                style = MaterialTheme.typography.titleMedium
                            )
                            Button(
                                onClick = onClockIn,
                                colors = ButtonDefaults.buttonColors(
                                    containerColor = MaterialTheme.colorScheme.primary
                                )
                            ) {
                                Text(if (isArabic) "تسجيل الحضور" else "Clock In")
                            }
                        }

                        else -> {
                            @Suppress("UNUSED_EXPRESSION") minuteTick  // recompose dependency
                            val clockIn = parseUtc(uiState.myClockStatus.clockIn)
                            val elapsed = clockIn?.let {
                                Duration.between(it, LocalDateTime.now(ZoneOffset.UTC))
                            }
                            val elapsedLabel = elapsed?.let {
                                val h = it.toHours()
                                val m = it.toMinutes() % 60
                                if (isArabic) "مسجل الحضور منذ ${h}س ${m}د"
                                else "Clocked in for ${h}h ${m}m"
                            } ?: (if (isArabic) "مسجل الحضور" else "Clocked in")

                            Text(elapsedLabel, style = MaterialTheme.typography.titleMedium)

                            OutlinedTextField(
                                value = breakMinutesInput,
                                onValueChange = { v ->
                                    if (v.length <= 3 && v.all { it.isDigit() }) breakMinutesInput = v
                                },
                                label = {
                                    Text(if (isArabic) "دقائق الاستراحة (اختياري)" else "Break minutes (optional)")
                                },
                                singleLine = true,
                                modifier = Modifier.fillMaxWidth(0.7f)
                            )

                            Button(
                                onClick = {
                                    onClockOut(breakMinutesInput.toIntOrNull())
                                    breakMinutesInput = ""
                                },
                                colors = ButtonDefaults.buttonColors(
                                    containerColor = MaterialTheme.colorScheme.error
                                )
                            ) {
                                Text(if (isArabic) "تسجيل الانصراف" else "Clock Out")
                            }
                        }
                    }
                }
            }
        }

        item {
            Text(
                if (isArabic) "السجلات الأخيرة" else "Recent entries",
                style = MaterialTheme.typography.titleSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }

        if (uiState.todayEntries.isEmpty()) {
            item {
                Text(
                    if (isArabic) "لا توجد سجلات اليوم" else "No entries today",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        } else {
            items(uiState.todayEntries, key = { it.id }) { entry ->
                TimeEntryCard(entry = entry, isArabic = isArabic)
            }
        }
    }
}

@Composable
private fun TimeEntryCard(entry: TimeEntryDto, isArabic: Boolean) {
    val clockIn = parseUtc(entry.clockIn)?.format(timeFormatter) ?: "—"
    val clockOut = entry.clockOut?.let { parseUtc(it)?.format(timeFormatter) }
    val worked = entry.workedMinutes?.let { "${it / 60}h ${it % 60}m" } ?: "—"

    Card(Modifier.fillMaxWidth()) {
        Row(
            Modifier.fillMaxWidth().padding(12.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(verticalArrangement = Arrangement.spacedBy(2.dp)) {
                Text(
                    "$clockIn → ${clockOut ?: (if (isArabic) "نشط" else "Active")}",
                    style = MaterialTheme.typography.bodyMedium,
                    fontWeight = FontWeight.Medium,
                    color = if (clockOut == null) MaterialTheme.colorScheme.primary
                            else MaterialTheme.colorScheme.onSurface
                )
                entry.breakMinutes?.let {
                    Text(
                        if (isArabic) "استراحة: ${it}د" else "Break: ${it}m",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }
            Text(
                worked,
                style = MaterialTheme.typography.bodyLarge,
                fontWeight = FontWeight.Bold
            )
        }
    }
}
