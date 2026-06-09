package com.tannous.pos.feature.customers

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.CustomerAnalyticsDto
import com.tannous.pos.core.data.model.LoyaltyCampaignDto
import com.tannous.pos.core.data.model.SendCampaignRequest
import com.tannous.pos.core.data.model.TopCustomerDto
import com.tannous.pos.core.data.remote.LoyaltyService
import com.tannous.pos.core.data.repository.SettingsRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import timber.log.Timber
import javax.inject.Inject

@HiltViewModel
class LoyaltyCrmViewModel @Inject constructor(
    private val loyaltyService: LoyaltyService,
    private val settingsRepository: SettingsRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow(LoyaltyCrmUiState())
    val uiState: StateFlow<LoyaltyCrmUiState> = _uiState.asStateFlow()

    init {
        viewModelScope.launch {
            val isArabic = settingsRepository.isArabic(settingsRepository.getLanguage())
            _uiState.update { it.copy(isArabic = isArabic) }
        }
        loadAnalytics()
    }

    fun loadAnalytics() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoadingAnalytics = true, analyticsError = null) }
            try {
                val analytics = loyaltyService.getAnalytics()
                _uiState.update { it.copy(isLoadingAnalytics = false, analytics = analytics) }
            } catch (e: Exception) {
                Timber.w(e, "Failed to load customer analytics")
                _uiState.update {
                    it.copy(
                        isLoadingAnalytics = false,
                        analyticsError = e.message ?: "Failed to load analytics"
                    )
                }
            }
        }
    }

    fun loadSegment(segment: Int) {
        viewModelScope.launch {
            _uiState.update {
                it.copy(selectedSegment = segment, isLoadingSegment = true, segmentError = null)
            }
            try {
                val page = loyaltyService.getSegment(segment)
                _uiState.update {
                    it.copy(isLoadingSegment = false, segmentCustomers = page.items)
                }
            } catch (e: Exception) {
                Timber.w(e, "Failed to load segment $segment")
                _uiState.update {
                    it.copy(
                        isLoadingSegment = false,
                        segmentCustomers = emptyList(),
                        segmentError = e.message ?: "Failed to load segment"
                    )
                }
            }
        }
    }

    fun sendCampaign(name: String, message: String, segment: Int) {
        if (name.isBlank() || message.isBlank()) {
            _uiState.update { it.copy(sendError = "Name and message are required") }
            return
        }
        viewModelScope.launch {
            _uiState.update { it.copy(isSending = true, sendError = null, lastCampaign = null) }
            try {
                val campaign = loyaltyService.sendCampaign(
                    SendCampaignRequest(name = name, message = message, targetSegment = segment)
                )
                _uiState.update { it.copy(isSending = false, lastCampaign = campaign) }
            } catch (e: Exception) {
                Timber.w(e, "Failed to send campaign")
                _uiState.update {
                    it.copy(isSending = false, sendError = e.message ?: "Failed to send campaign")
                }
            }
        }
    }

    fun clearSendResult() {
        _uiState.update { it.copy(lastCampaign = null, sendError = null) }
    }
}

data class LoyaltyCrmUiState(
    val isArabic: Boolean = false,
    val isLoadingAnalytics: Boolean = false,
    val analytics: CustomerAnalyticsDto? = null,
    val analyticsError: String? = null,
    val selectedSegment: Int = 0,
    val isLoadingSegment: Boolean = false,
    val segmentCustomers: List<TopCustomerDto> = emptyList(),
    val segmentError: String? = null,
    val isSending: Boolean = false,
    val lastCampaign: LoyaltyCampaignDto? = null,
    val sendError: String? = null
)
