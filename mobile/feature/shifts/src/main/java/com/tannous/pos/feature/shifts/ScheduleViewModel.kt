package com.tannous.pos.feature.shifts

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.ClockInRequest
import com.tannous.pos.core.data.model.ClockOutRequest
import com.tannous.pos.core.data.model.CreateScheduleRequest
import com.tannous.pos.core.data.model.EmployeeScheduleDto
import com.tannous.pos.core.data.model.PublishScheduleRequest
import com.tannous.pos.core.data.model.TimeEntryDto
import com.tannous.pos.core.data.model.UserDto
import com.tannous.pos.core.data.model.WeeklyScheduleDto
import com.tannous.pos.core.data.remote.ScheduleService
import com.tannous.pos.core.data.repository.BranchRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import timber.log.Timber
import java.time.DayOfWeek
import java.time.LocalDate
import java.time.ZoneOffset
import java.time.format.DateTimeFormatter
import javax.inject.Inject

@HiltViewModel
class ScheduleViewModel @Inject constructor(
    private val scheduleService: ScheduleService,
    private val branchRepository: BranchRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow(ScheduleUiState())
    val uiState: StateFlow<ScheduleUiState> = _uiState.asStateFlow()

    init { load() }

    fun load() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true) }
            try { branchRepository.getBranches() } catch (_: Exception) { /* offline-tolerant */ }
            loadWeeklySchedule()
            loadMyClockStatus()
            loadTodayEntries()
            loadUsers()
            _uiState.update { it.copy(isLoading = false) }
        }
    }

    fun previousWeek() {
        _uiState.update { it.copy(currentWeekStart = it.currentWeekStart.minusDays(7)) }
        viewModelScope.launch { loadWeeklySchedule() }
    }

    fun nextWeek() {
        _uiState.update { it.copy(currentWeekStart = it.currentWeekStart.plusDays(7)) }
        viewModelScope.launch { loadWeeklySchedule() }
    }

    // ── Staff: Clock In / Out ────────────────────────────────────────────────

    fun clockIn() {
        viewModelScope.launch {
            _uiState.update { it.copy(isClockBusy = true) }
            try {
                val branchId = branchRepository.getDefaultBranch()?.id
                if (branchId == null) {
                    _uiState.update { it.copy(isClockBusy = false, error = "No branch available") }
                    return@launch
                }
                val entry = scheduleService.clockIn(ClockInRequest(branchId = branchId))
                _uiState.update { it.copy(myClockStatus = entry, isClockBusy = false) }
                loadTodayEntries()
            } catch (e: Exception) {
                Timber.w(e, "Clock-in failed")
                _uiState.update { it.copy(isClockBusy = false, error = "Clock-in failed") }
            }
        }
    }

    fun clockOut(breakMinutes: Int? = null) {
        viewModelScope.launch {
            _uiState.update { it.copy(isClockBusy = true) }
            try {
                val branchId = _uiState.value.myClockStatus?.branchId
                    ?: branchRepository.getDefaultBranch()?.id
                if (branchId == null) {
                    _uiState.update { it.copy(isClockBusy = false, error = "No branch available") }
                    return@launch
                }
                scheduleService.clockOut(ClockOutRequest(branchId = branchId, breakMinutes = breakMinutes))
                _uiState.update { it.copy(myClockStatus = null, isClockBusy = false) }
                loadTodayEntries()
            } catch (e: Exception) {
                Timber.w(e, "Clock-out failed")
                _uiState.update { it.copy(isClockBusy = false, error = "Clock-out failed") }
            }
        }
    }

    // ── Manager: Create / Cancel / Publish ──────────────────────────────────

    /**
     * Creates a new shift. [scheduledStart] and [scheduledEnd] are ISO-8601 UTC strings
     * e.g. "2026-06-30T08:00:00Z".
     */
    fun createShift(
        userId: String,
        scheduledStart: String,
        scheduledEnd: String,
        position: String? = null,
        notes: String? = null
    ) {
        viewModelScope.launch {
            _uiState.update { it.copy(isSaving = true) }
            try {
                val branchId = branchRepository.getDefaultBranch()?.id
                if (branchId == null) {
                    _uiState.update { it.copy(isSaving = false, error = "No branch available") }
                    return@launch
                }
                scheduleService.createSchedule(
                    CreateScheduleRequest(
                        userId = userId,
                        branchId = branchId,
                        scheduledStart = scheduledStart,
                        scheduledEnd = scheduledEnd,
                        position = position?.takeIf { it.isNotBlank() },
                        notes = notes?.takeIf { it.isNotBlank() }
                    )
                )
                _uiState.update { it.copy(isSaving = false) }
                loadWeeklySchedule()
            } catch (e: Exception) {
                Timber.w(e, "Create shift failed")
                _uiState.update { it.copy(isSaving = false, error = "Could not create shift") }
            }
        }
    }

    /** Cancels (deletes) a draft shift. Only draft shifts can be cancelled this way. */
    fun cancelShift(scheduleId: String) {
        viewModelScope.launch {
            _uiState.update { it.copy(isSaving = true) }
            try {
                scheduleService.cancelSchedule(scheduleId)
                _uiState.update { it.copy(isSaving = false) }
                loadWeeklySchedule()
            } catch (e: Exception) {
                Timber.w(e, "Cancel shift $scheduleId failed")
                _uiState.update { it.copy(isSaving = false, error = "Could not cancel shift") }
            }
        }
    }

    /** Publishes all currently visible draft shifts in the week view. */
    fun publishDrafts() {
        val draftIds = _uiState.value.weeklySchedule?.schedules
            ?.filter { it.status.equals("Draft", ignoreCase = true) }
            ?.map { it.id }
            ?: return
        if (draftIds.isEmpty()) return

        viewModelScope.launch {
            _uiState.update { it.copy(isSaving = true) }
            try {
                scheduleService.publishSchedule(PublishScheduleRequest(scheduleIds = draftIds))
                _uiState.update { it.copy(isSaving = false) }
                loadWeeklySchedule()
            } catch (e: Exception) {
                Timber.w(e, "Publish drafts failed")
                _uiState.update { it.copy(isSaving = false, error = "Could not publish shifts") }
            }
        }
    }

    /** Loads all time entries for the current week (manager view across all staff). */
    fun loadAllTimeEntries() {
        viewModelScope.launch {
            try {
                val weekStart = _uiState.value.currentWeekStart
                val from = weekStart.atStartOfDay(ZoneOffset.UTC)
                    .format(DateTimeFormatter.ISO_INSTANT)
                val to = weekStart.plusDays(7).atStartOfDay(ZoneOffset.UTC)
                    .format(DateTimeFormatter.ISO_INSTANT)
                val entries = scheduleService.getTimeEntries(from = from, to = to)
                _uiState.update { it.copy(allTimeEntries = entries) }
            } catch (e: Exception) {
                Timber.w(e, "Load all time entries failed")
            }
        }
    }

    fun clearError() = _uiState.update { it.copy(error = null) }

    // ── Private loaders ─────────────────────────────────────────────────────

    private suspend fun loadWeeklySchedule() {
        try {
            val weekStartUtc = _uiState.value.currentWeekStart
                .atStartOfDay(ZoneOffset.UTC).toLocalDateTime().toString()
            val schedule = scheduleService.getWeeklySchedule(weekStart = weekStartUtc)
            val hasDrafts = schedule.schedules.any { it.status.equals("Draft", ignoreCase = true) }
            _uiState.update { it.copy(weeklySchedule = schedule, hasDrafts = hasDrafts, error = null) }
        } catch (e: Exception) {
            Timber.w(e, "Weekly schedule load failed")
            _uiState.update { it.copy(error = "Could not load schedule") }
        }
    }

    private suspend fun loadMyClockStatus() {
        try {
            val entry = scheduleService.myClockStatus()
            _uiState.update { it.copy(myClockStatus = entry) }
        } catch (e: Exception) {
            Timber.w(e, "Clock status load failed")
        }
    }

    private suspend fun loadTodayEntries() {
        try {
            val today = LocalDate.now(ZoneOffset.UTC)
            val entries = scheduleService.getMyTimeEntries(
                from = today.atStartOfDay().toString(),
                to = today.plusDays(1).atStartOfDay().toString()
            )
            _uiState.update { it.copy(todayEntries = entries) }
        } catch (e: Exception) {
            Timber.w(e, "Today's time entries load failed")
        }
    }

    private suspend fun loadUsers() {
        try {
            val staff = scheduleService.listStaff()
            _uiState.update { it.copy(users = staff) }
        } catch (e: Exception) {
            Timber.w(e, "Staff list load failed — shift picker will be empty")
            // Non-fatal: manager can't pick employees if offline, but rest of screen still works
        }
    }
}

data class ScheduleUiState(
    val isLoading: Boolean = false,
    val weeklySchedule: WeeklyScheduleDto? = null,
    /** Monday of the week being viewed (local date). */
    val currentWeekStart: LocalDate = LocalDate.now().with(DayOfWeek.MONDAY),
    /** Current active clock entry; null = not clocked in. */
    val myClockStatus: TimeEntryDto? = null,
    val todayEntries: List<TimeEntryDto> = emptyList(),
    val isClockBusy: Boolean = false,
    // Manager extras
    val users: List<UserDto> = emptyList(),
    val allTimeEntries: List<TimeEntryDto> = emptyList(),
    val isSaving: Boolean = false,
    val hasDrafts: Boolean = false,
    val error: String? = null
)
