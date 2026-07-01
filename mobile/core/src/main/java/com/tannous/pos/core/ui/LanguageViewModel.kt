package com.tannous.pos.core.ui

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.repository.SettingsRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class LanguageViewModel @Inject constructor(
    private val settingsRepository: SettingsRepository
) : ViewModel() {

    private val _isArabic = MutableStateFlow(false)
    val isArabic: StateFlow<Boolean> = _isArabic.asStateFlow()

    init {
        viewModelScope.launch {
            _isArabic.value = settingsRepository.getLanguage() == SettingsRepository.LANG_AR
        }
    }

    /**
     * Directly sets isArabic without an async DB read.
     * Call this immediately after toggling the language so the UI recomposes synchronously
     * without a race between the write coroutine and a follow-up read coroutine.
     */
    fun setIsArabic(value: Boolean) {
        _isArabic.value = value
    }

    /**
     * Re-reads the persisted language from the DB.
     * Useful on cold start or after an external change. Prefer [setIsArabic] when the
     * new value is already known to avoid a write/read race condition.
     */
    fun refresh() {
        viewModelScope.launch {
            _isArabic.value = settingsRepository.getLanguage() == SettingsRepository.LANG_AR
        }
    }
}
