package com.tannous.pos.core.data.repository

import com.tannous.pos.core.data.model.AdjustInventoryPayload
import com.tannous.pos.core.data.model.CreateIngredientRequest
import com.tannous.pos.core.data.model.IngredientDto
import com.tannous.pos.core.data.model.InventoryItemDto
import com.tannous.pos.core.data.model.RecordWastagePayload
import com.tannous.pos.core.data.model.UpdateIngredientRequest
import com.tannous.pos.core.data.remote.InventoryService
import com.tannous.pos.core.sync.OutboxManager
import retrofit2.HttpException
import timber.log.Timber
import java.io.IOException
import java.math.BigDecimal
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class InventoryRepository @Inject constructor(
    private val inventoryService: InventoryService,
    private val outboxManager: OutboxManager
) {

    suspend fun getInventoryItems(lowStockOnly: Boolean = false): Result<List<InventoryItemDto>> {
        return try {
            val items = if (lowStockOnly) {
                inventoryService.getLowStockItems()
            } else {
                inventoryService.getInventoryItems()
            }
            Result.success(items)
        } catch (e: HttpException) {
            Result.failure(
                RuntimeException(
                    if (e.code() == 403) "Inventory requires owner access"
                    else "Server error: ${e.code()}"
                )
            )
        } catch (e: IOException) {
            Result.failure(IOException("No connection"))
        } catch (e: Exception) {
            Timber.e(e, "Error loading inventory")
            Result.failure(e)
        }
    }

    suspend fun getIngredients(): Result<List<IngredientDto>> {
        return try {
            Result.success(inventoryService.getIngredients())
        } catch (e: HttpException) {
            Result.failure(
                RuntimeException(
                    if (e.code() == 403) "Inventory requires owner access"
                    else "Server error: ${e.code()}"
                )
            )
        } catch (e: IOException) {
            Result.failure(IOException("No connection"))
        } catch (e: Exception) {
            Timber.e(e, "Error loading ingredients")
            Result.failure(e)
        }
    }

    suspend fun createIngredient(request: CreateIngredientRequest): Result<IngredientDto> {
        return try {
            Result.success(inventoryService.createIngredient(request))
        } catch (e: HttpException) {
            val msg = if (e.code() == 403) {
                "Requires owner access"
            } else {
                parseErrorMessage(e) ?: "Server error: ${e.code()}"
            }
            Result.failure(RuntimeException(msg))
        } catch (e: IOException) {
            Result.failure(IOException("No connection"))
        } catch (e: Exception) {
            Timber.e(e, "Error creating ingredient")
            Result.failure(e)
        }
    }

    suspend fun updateIngredient(
        id: String,
        request: UpdateIngredientRequest
    ): Result<IngredientDto> {
        return try {
            Result.success(inventoryService.updateIngredient(id, request))
        } catch (e: HttpException) {
            val msg = if (e.code() == 403) {
                "Requires owner access"
            } else {
                parseErrorMessage(e) ?: "Server error: ${e.code()}"
            }
            Result.failure(RuntimeException(msg))
        } catch (e: IOException) {
            Result.failure(IOException("No connection"))
        } catch (e: Exception) {
            Timber.e(e, "Error updating ingredient")
            Result.failure(e)
        }
    }

    suspend fun deleteIngredient(id: String, force: Boolean = false): Result<Unit> {
        return try {
            val response = inventoryService.deleteIngredient(id, force)
            if (response.isSuccessful) {
                Result.success(Unit)
            } else {
                val errorBody = response.errorBody()?.string().orEmpty()
                val msg = when {
                    response.code() == 403 -> "Requires owner access"
                    errorBody.contains("active recipes", ignoreCase = true) ->
                        "RECIPE_CONFLICT:$errorBody"
                    else -> "Delete failed: ${response.code()}"
                }
                Result.failure(RuntimeException(msg))
            }
        } catch (e: IOException) {
            Result.failure(IOException("No connection"))
        } catch (e: Exception) {
            Timber.e(e, "Error deleting ingredient")
            Result.failure(e)
        }
    }

    suspend fun adjustStock(
        ingredientId: String,
        quantity: BigDecimal,
        reason: String
    ) {
        val payload = AdjustInventoryPayload(
            ingredientId = ingredientId,
            quantity = quantity.toPlainString(),
            reason = reason
        )
        outboxManager.enqueueOperation("AdjustInventory", payload)
        outboxManager.triggerImmediatePush()
    }

    suspend fun recordWastage(
        ingredientId: String,
        quantity: BigDecimal,
        reason: String
    ) {
        val payload = RecordWastagePayload(
            ingredientId = ingredientId,
            quantity = quantity.toPlainString(),
            reason = reason
        )
        outboxManager.enqueueOperation("RecordWastage", payload)
        outboxManager.triggerImmediatePush()
    }

    private fun parseErrorMessage(e: HttpException): String? {
        return try {
            e.response()?.errorBody()?.string()
        } catch (_: Exception) {
            null
        }
    }
}
