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

    /** Called after the user toggles the language in Settings, to refresh the app-level direction. */
    fun refresh() {
        viewModelScope.launch {
            _isArabic.value = settingsRepository.getLanguage() == SettingsRepository.LANG_AR
        }
    }
}
