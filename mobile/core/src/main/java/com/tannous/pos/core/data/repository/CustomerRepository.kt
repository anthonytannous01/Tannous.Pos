package com.tannous.pos.core.data.repository

import com.tannous.pos.core.data.local.dao.CustomerDao
import com.tannous.pos.core.data.local.entity.CustomerEntity
import com.tannous.pos.core.data.model.CreateCustomerRequest
import com.tannous.pos.core.data.model.CustomerDto
import com.tannous.pos.core.data.model.UpdateCustomerRequest
import com.tannous.pos.core.data.remote.CustomerService
import kotlinx.coroutines.flow.Flow
import retrofit2.HttpException
import timber.log.Timber
import java.io.IOException
import java.time.Instant
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class CustomerRepository @Inject constructor(
    private val customerDao: CustomerDao,
    private val customerService: CustomerService
) {

    fun searchCustomers(query: String): Flow<List<CustomerEntity>> =
        if (query.isBlank()) customerDao.getAllActive()
        else customerDao.searchCustomers(query.trim())

    suspend fun getCustomerById(id: String): CustomerEntity? =
        customerDao.getById(id)

    suspend fun createCustomer(
        firstName: String,
        lastName: String,
        email: String?,
        phone: String?,
        address: String?,
        notes: String?,
        allergies: String?
    ): Result<CustomerEntity> {
        return try {
            val request = CreateCustomerRequest(
                firstName = firstName.trim(),
                lastName = lastName.trim(),
                email = email?.trim()?.takeIf { it.isNotBlank() },
                phone = phone?.trim()?.takeIf { it.isNotBlank() },
                address = address?.trim()?.takeIf { it.isNotBlank() },
                notes = notes?.trim()?.takeIf { it.isNotBlank() },
                allergies = allergies?.trim()?.takeIf { it.isNotBlank() }
            )
            val dto = customerService.createCustomer(request)
            val entity = dto.toEntity()
            customerDao.insert(entity)
            Timber.d("Customer created: ${entity.id}")
            Result.success(entity)
        } catch (e: IOException) {
            Timber.w(e, "Network error creating customer")
            Result.failure(IOException("No connection. Customer not saved."))
        } catch (e: Exception) {
            Timber.e(e, "Error creating customer")
            Result.failure(e)
        }
    }

    suspend fun updateCustomer(
        id: String,
        firstName: String,
        lastName: String,
        email: String?,
        phone: String?,
        address: String?,
        notes: String?,
        allergies: String?,
        version: String
    ): Result<CustomerEntity> {
        return try {
            val request = UpdateCustomerRequest(
                firstName = firstName.trim(),
                lastName = lastName.trim(),
                email = email?.trim()?.takeIf { it.isNotBlank() },
                phone = phone?.trim()?.takeIf { it.isNotBlank() },
                address = address?.trim()?.takeIf { it.isNotBlank() },
                notes = notes?.trim()?.takeIf { it.isNotBlank() },
                allergies = allergies?.trim()?.takeIf { it.isNotBlank() },
                version = version
            )
            val dto = customerService.updateCustomer(id, request)
            val entity = dto.toEntity()
            customerDao.insert(entity)
            Timber.d("Customer updated: $id, new version: ${entity.version}")
            Result.success(entity)
        } catch (e: HttpException) {
            if (e.code() == 409) {
                Result.failure(
                    IllegalStateException(
                        "Customer was updated elsewhere. Please refresh and try again."
                    )
                )
            } else {
                Result.failure(RuntimeException("Server error: ${e.code()}"))
            }
        } catch (e: IOException) {
            Result.failure(IOException("No connection. Changes not saved."))
        } catch (e: Exception) {
            Timber.e(e, "Error updating customer $id")
            Result.failure(e)
        }
    }

    private fun CustomerDto.toEntity() = CustomerEntity(
        id = id,
        firstName = firstName,
        lastName = lastName,
        email = email,
        phone = phone,
        address = address,
        notes = notes,
        allergies = allergies,
        isActive = isActive,
        lastVisitDate = lastVisitDate?.let {
            runCatching { Instant.parse(it) }.getOrNull()
        },
        totalOrders = totalOrders,
        isDeleted = isDeleted ?: false,
        deletedAt = deletedAt?.let {
            runCatching { Instant.parse(it) }.getOrNull()
        },
        version = version
    )
}
