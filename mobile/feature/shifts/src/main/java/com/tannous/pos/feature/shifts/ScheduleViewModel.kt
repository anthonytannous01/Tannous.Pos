package com.tannous.pos.feature.shifts

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.ClockInRequest
import com.tannous.pos.core.data.model.ClockOutRequest
import com.tannous.pos.core.data.model.TimeEntryDto
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
            // Warm the branch cache so clockIn() can resolve the default branch.
            try { branchRepository.getBranches() } catch (_: Exception) { /* offline-tolerant */ }
            loadWeeklySchedule()
            loadMyClockStatus()
            loadTodayEntries()
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

    fun clearError() = _uiState.update { it.copy(error = null) }

    private suspend fun loadWeeklySchedule() {
        try {
            val weekStartUtc = _uiState.value.currentWeekStart
                .atStartOfDay(ZoneOffset.UTC).toLocalDateTime().toString()
            val schedule = scheduleService.getWeeklySchedule(weekStart = weekStartUtc)
            _uiState.update { it.copy(weeklySchedule = schedule, error = null) }
        } catch (e: Exception) {
            Timber.w(e, "Weekly schedule load failed")
            _uiState.update { it.copy(error = "Could not load schedule") }
        }
    }

    private suspend fun loadMyClockStatus() {
        try {
            // 200 → active entry; 204 → null → not clocked in.
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
}

data class ScheduleUiState(
    val isLoading: Boolean = false,
    val weeklySchedule: WeeklyScheduleDto? = null,
    /** Monday of the week being viewed (local date). */
    val currentWeekStart: LocalDate = LocalDate.now().with(DayOfWeek.MONDAY),
    /** Current Active entry; null = not clocked in. */
    val myClockStatus: TimeEntryDto? = null,
    val todayEntries: List<TimeEntryDto> = emptyList(),
    val isClockBusy: Boolean = false,
    val error: String? = null
)
