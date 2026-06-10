package com.tannous.pos.feature.settings

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.model.ApiKeyDto
import com.tannous.pos.core.data.model.WebhookSubscriptionDto
import com.tannous.pos.core.data.remote.WebhooksService
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import timber.log.Timber
import javax.inject.Inject

@HiltViewModel
class IntegrationsViewModel @Inject constructor(
    private val webhooksService: WebhooksService
) : ViewModel() {

    private val _uiState = MutableStateFlow(IntegrationsUiState())
    val uiState: StateFlow<IntegrationsUiState> = _uiState.asStateFlow()

    init {
        loadAll()
    }

    fun loadAll() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                val webhooks = webhooksService.getSubscriptions()
                val apiKeys = webhooksService.getApiKeys()
                _uiState.update {
                    it.copy(isLoading = false, webhooks = webhooks, apiKeys = apiKeys)
                }
            } catch (e: Exception) {
                Timber.w(e, "Failed to load integrations")
                _uiState.update {
                    it.copy(isLoading = false, error = e.message ?: "Failed to load integrations")
                }
            }
        }
    }

    fun testWebhook(id: String) {
        viewModelScope.launch {
            try {
                webhooksService.testSubscription(id)
                loadAll()
            } catch (e: Exception) {
                Timber.w(e, "Webhook test failed for $id")
                _uiState.update { it.copy(error = e.message ?: "Webhook test failed") }
            }
        }
    }

    fun deleteWebhook(id: String) {
        viewModelScope.launch {
            try {
                webhooksService.deleteSubscription(id)
                loadAll()
            } catch (e: Exception) {
                Timber.w(e, "Webhook delete failed for $id")
                _uiState.update { it.copy(error = e.message ?: "Failed to delete webhook") }
            }
        }
    }

    fun revokeApiKey(id: String) {
        viewModelScope.launch {
            try {
                webhooksService.revokeApiKey(id)
                loadAll()
            } catch (e: Exception) {
                Timber.w(e, "API key revoke failed for $id")
                _uiState.update { it.copy(error = e.message ?: "Failed to revoke API key") }
            }
        }
    }

    fun clearError() = _uiState.update { it.copy(error = null) }
}

data class IntegrationsUiState(
    val isLoading: Boolean = false,
    val webhooks: List<WebhookSubscriptionDto> = emptyList(),
    val apiKeys: List<ApiKeyDto> = emptyList(),
    val error: String? = null
)
