package com.tannous.pos.feature.shifts

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.KeyboardArrowLeft
import androidx.compose.material.icons.filled.KeyboardArrowRight
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.tannous.pos.core.data.model.EmployeeScheduleDto
import com.tannous.pos.core.data.model.TimeEntryDto
import com.tannous.pos.core.ui.LocalIsArabic
import kotlinx.coroutines.delay
import java.time.DayOfWeek
import java.time.Duration
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.OffsetDateTime
import java.time.ZoneOffset
import java.time.format.DateTimeFormatter

private val timeFormatter: DateTimeFormatter = DateTimeFormatter.ofPattern("HH:mm")
private val dateFormatter: DateTimeFormatter = DateTimeFormatter.ofPattern("EEE MMM d")

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
                        Icon(Icons.Default.ArrowBack, contentDescription = "Back")
                    }
                }
            )
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
                    onNextWeek = { viewModel.nextWeek() }
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
}

// ─── Schedule tab ────────────────────────────────────────────────────────────

@Composable
private fun ScheduleTab(
    uiState: ScheduleUiState,
    isArabic: Boolean,
    onPreviousWeek: () -> Unit,
    onNextWeek: () -> Unit
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
                Icon(Icons.Default.KeyboardArrowLeft, contentDescription = "Previous week")
            }
            Text(
                "${weekStart.format(dateFormatter)} – ${weekEnd.format(dateFormatter)}",
                style = MaterialTheme.typography.titleSmall,
                fontWeight = FontWeight.SemiBold
            )
            IconButton(onClick = onNextWeek) {
                Icon(Icons.Default.KeyboardArrowRight, contentDescription = "Next week")
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
                    contentPadding = PaddingValues(12.dp),
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
                                ScheduleCard(schedule = schedule, isArabic = isArabic)
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
private fun ScheduleCard(schedule: EmployeeScheduleDto, isArabic: Boolean) {
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
                    if (schedule.status == "Draft") {
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
            Text(
                if (minutes == 0) "${hours}h" else "${hours}h ${minutes}m",
                style = MaterialTheme.typography.labelLarge,
                fontWeight = FontWeight.Bold
            )
        }
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
