package com.tannous.pos.feature.settings

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.AvailableTableDto
import com.tannous.pos.core.data.model.CreateReservationRequest
import com.tannous.pos.core.data.model.ReservationDto
import com.tannous.pos.core.data.model.UpdateReservationStatusRequest
import com.tannous.pos.core.data.remote.ReservationService
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import timber.log.Timber
import java.time.LocalDate
import java.time.format.DateTimeFormatter
import javax.inject.Inject

// Status constants matching backend ReservationStatus enum
const val RESERVATION_PENDING   = 0
const val RESERVATION_CONFIRMED = 1
const val RESERVATION_SEATED    = 2
const val RESERVATION_CANCELLED = 3
const val RESERVATION_NOSHOW    = 4

val reservationStatusLabels = mapOf(
    RESERVATION_PENDING   to "Pending",
    RESERVATION_CONFIRMED to "Confirmed",
    RESERVATION_SEATED    to "Seated",
    RESERVATION_CANCELLED to "Cancelled",
    RESERVATION_NOSHOW    to "No Show"
)

@HiltViewModel
class ReservationsViewModel @Inject constructor(
    private val reservationService: ReservationService
) : ViewModel() {

    private val _uiState = MutableStateFlow(ReservationsUiState())
    val uiState: StateFlow<ReservationsUiState> = _uiState.asStateFlow()

    init { loadForDate(LocalDate.now()) }

    fun loadForDate(date: LocalDate) {
        _uiState.update { it.copy(selectedDate = date, isLoading = true, error = null) }
        viewModelScope.launch {
            try {
                val fmt  = DateTimeFormatter.ISO_DATE
                val from = date.atStartOfDay().toString() + "Z"
                val to   = date.plusDays(1).atStartOfDay().toString() + "Z"
                val list = reservationService.getReservations(from = from, to = to)
                _uiState.update { it.copy(reservations = list, isLoading = false) }
            } catch (e: Exception) {
                Timber.w(e, "Failed to load reservations")
                _uiState.update { it.copy(isLoading = false, error = "Could not load reservations") }
            }
        }
    }

    fun loadAvailableTables(slot: String, partySize: Int) {
        viewModelScope.launch {
            try {
                val tables = reservationService.getAvailableTables(slot = slot, partySize = partySize)
                _uiState.update { it.copy(availableTables = tables) }
            } catch (e: Exception) {
                Timber.w(e, "Failed to load available tables")
                _uiState.update { it.copy(availableTables = emptyList()) }
            }
        }
    }

    fun create(request: CreateReservationRequest) {
        _uiState.update { it.copy(isCreating = true, createError = null) }
        viewModelScope.launch {
            try {
                reservationService.create(request)
                _uiState.update { it.copy(isCreating = false, showCreateDialog = false) }
                loadForDate(_uiState.value.selectedDate)
            } catch (e: Exception) {
                Timber.w(e, "Failed to create reservation")
                _uiState.update { it.copy(isCreating = false, createError = "Could not create reservation") }
            }
        }
    }

    fun updateStatus(id: String, status: Int) {
        viewModelScope.launch {
            try {
                reservationService.updateStatus(id, UpdateReservationStatusRequest(status = status))
                loadForDate(_uiState.value.selectedDate)
            } catch (e: Exception) {
                Timber.w(e, "Failed to update reservation status")
                _uiState.update { it.copy(error = "Could not update reservation") }
            }
        }
    }

    fun showCreateDialog()  = _uiState.update { it.copy(showCreateDialog = true,  createError = null) }
    fun dismissCreateDialog() = _uiState.update { it.copy(showCreateDialog = false, availableTables = emptyList()) }
    fun clearError()        = _uiState.update { it.copy(error = null) }
}

data class ReservationsUiState(
    val reservations:    List<ReservationDto>    = emptyList(),
    val availableTables: List<AvailableTableDto> = emptyList(),
    val selectedDate:    LocalDate               = LocalDate.now(),
    val isLoading:       Boolean                 = false,
    val isCreating:      Boolean                 = false,
    val showCreateDialog:Boolean                 = false,
    val error:           String?                 = null,
    val createError:     String?                 = null
)
