package com.tannous.pos.feature.customers

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tannous.pos.core.data.local.entity.CustomerEntity
import com.tannous.pos.core.data.repository.CustomerRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import timber.log.Timber
import javax.inject.Inject

@OptIn(ExperimentalCoroutinesApi::class)
@HiltViewModel
class CustomersViewModel @Inject constructor(
    private val customerRepository: CustomerRepository
) : ViewModel() {

    private val _searchQuery = MutableStateFlow("")
    private val _uiState = MutableStateFlow(CustomersUiState())
    val uiState: StateFlow<CustomersUiState> = _uiState.asStateFlow()

    init {
        viewModelScope.launch {
            _searchQuery
                .flatMapLatest { query -> customerRepository.searchCustomers(query) }
                .collect { customers ->
                    _uiState.update { it.copy(customers = customers) }
                }
        }
    }

    fun onSearchQueryChange(query: String) {
        _uiState.update { it.copy(searchQuery = query) }
        _searchQuery.value = query
    }

    fun showCreateDialog() {
        _uiState.update { it.copy(showCreateDialog = true, createSuccess = false, error = null) }
    }

    fun dismissCreateDialog() {
        _uiState.update { it.copy(showCreateDialog = false, createSuccess = false) }
    }

    fun createCustomer(
        firstName: String,
        lastName: String,
        email: String?,
        phone: String?,
        address: String?,
        notes: String?,
        allergies: String?
    ) {
        if (firstName.isBlank() || lastName.isBlank()) {
            _uiState.update { it.copy(error = "First and last name are required") }
            return
        }
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            val result = customerRepository.createCustomer(
                firstName = firstName,
                lastName = lastName,
                email = email,
                phone = phone,
                address = address,
                notes = notes,
                allergies = allergies
            )
            result.fold(
                onSuccess = {
                    _uiState.update {
                        it.copy(isLoading = false, showCreateDialog = false, createSuccess = true)
                    }
                },
                onFailure = { e ->
                    Timber.w(e, "Create customer failed")
                    _uiState.update {
                        it.copy(isLoading = false, error = e.message ?: "Failed to create customer")
                    }
                }
            )
        }
    }

    fun clearError() {
        _uiState.update { it.copy(error = null) }
    }

    fun startEdit(customer: CustomerEntity) {
        _uiState.update { it.copy(editingCustomer = customer, updateError = null) }
    }

    fun dismissEdit() {
        _uiState.update { it.copy(editingCustomer = null, updateError = null) }
    }

    fun updateCustomer(
        firstName: String,
        lastName: String,
        email: String?,
        phone: String?,
        address: String?,
        notes: String?,
        allergies: String?
    ) {
        val customer = _uiState.value.editingCustomer ?: return
        val version = customer.version ?: return

        if (firstName.isBlank() || lastName.isBlank()) {
            _uiState.update { it.copy(updateError = "First and last name are required") }
            return
        }

        viewModelScope.launch {
            _uiState.update { it.copy(isUpdating = true, updateError = null) }
            val result = customerRepository.updateCustomer(
                id = customer.id,
                firstName = firstName,
                lastName = lastName,
                email = email,
                phone = phone,
                address = address,
                notes = notes,
                allergies = allergies,
                version = version
            )
            _uiState.update { state ->
                if (result.isSuccess) {
                    state.copy(isUpdating = false, editingCustomer = null)
                } else {
                    state.copy(
                        isUpdating = false,
                        updateError = result.exceptionOrNull()?.message ?: "Update failed"
                    )
                }
            }
        }
    }

    fun clearUpdateError() {
        _uiState.update { it.copy(updateError = null) }
    }
}

data class CustomersUiState(
    val customers: List<CustomerEntity> = emptyList(),
    val searchQuery: String = "",
    val isLoading: Boolean = false,
    val error: String? = null,
    val showCreateDialog: Boolean = false,
    val createSuccess: Boolean = false,
    val editingCustomer: CustomerEntity? = null,
    val isUpdating: Boolean = false,
    val updateError: String? = null
)
