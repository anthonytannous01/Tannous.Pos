package com.tannous.pos.feature.sell

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.SubmitFeedbackRequest
import com.tannous.pos.core.data.remote.FeedbackService
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import timber.log.Timber
import javax.inject.Inject

@HiltViewModel
class FeedbackViewModel @Inject constructor(
    private val feedbackService: FeedbackService
) : ViewModel() {

    private val _uiState = MutableStateFlow(FeedbackUiState())
    val uiState: StateFlow<FeedbackUiState> = _uiState.asStateFlow()

    fun setRating(rating: Int) = _uiState.update { it.copy(selectedRating = rating) }
    fun setComment(comment: String) = _uiState.update { it.copy(comment = comment) }
    fun setCategory(category: Int) = _uiState.update { it.copy(selectedCategory = category) }

    fun submit(orderId: String?, orderNumber: String?, branchId: String?) {
        val state = _uiState.value
        if (state.selectedRating == 0) return
        _uiState.update { it.copy(isSubmitting = true) }

        viewModelScope.launch {
            try {
                feedbackService.submit(
                    SubmitFeedbackRequest(
                        rating      = state.selectedRating,
                        comment     = state.comment.takeIf { it.isNotBlank() },
                        category    = state.selectedCategory,
                        orderId     = orderId,
                        orderNumber = orderNumber,
                        branchId    = branchId
                    )
                )
                _uiState.update { it.copy(isSubmitting = false, submitted = true) }
            } catch (e: Exception) {
                Timber.w(e, "Feedback submit failed")
                _uiState.update { it.copy(isSubmitting = false, error = "Could not submit feedback") }
            }
        }
    }

    fun clearError() = _uiState.update { it.copy(error = null) }
}

data class FeedbackUiState(
    val selectedRating:   Int     = 0,
    val comment:          String  = "",
    val selectedCategory: Int     = 0,  // 0 = General
    val isSubmitting:     Boolean = false,
    val submitted:        Boolean = false,
    val error:            String? = null
)

// Category names matching backend FeedbackCategory enum
val feedbackCategories = listOf("General", "Food", "Service", "Delivery", "Cleanliness", "Complaint")
