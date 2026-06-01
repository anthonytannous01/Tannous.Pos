package com.tannous.pos.core.data.repository

import com.tannous.pos.core.data.model.AdjustInventoryPayload
import com.tannous.pos.core.data.model.InventoryItemDto
import com.tannous.pos.core.data.model.RecordWastagePayload
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
}
