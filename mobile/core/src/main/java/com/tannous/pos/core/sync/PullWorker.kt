package com.tannous.pos.core.sync

import android.content.Context
import androidx.hilt.work.HiltWorker
import androidx.work.*
import com.tannous.pos.core.data.local.dao.KeyValueDao
import com.tannous.pos.core.data.local.dao.*
import com.tannous.pos.core.data.remote.SyncService
import dagger.assisted.Assisted
import dagger.assisted.AssistedInject
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.withContext
import timber.log.Timber
import java.time.Instant
import java.util.concurrent.TimeUnit

@HiltWorker
class PullWorker @AssistedInject constructor(
    @Assisted appContext: Context,
    @Assisted workerParams: WorkerParameters,
    private val syncService: SyncService,
    private val keyValueDao: KeyValueDao,
    private val categoryDao: CategoryDao,
    private val menuItemDao: MenuItemDao,
    private val addOnDao: AddOnDao,
    private val customerDao: CustomerDao
) : CoroutineWorker(appContext, workerParams) {
    
    override suspend fun doWork(): Result = withContext(Dispatchers.IO) {
        try {
            Timber.d("Starting pull sync...")
            
            val syncCursor = keyValueDao.get("sync_cursor")
            var nextToken: String? = null
            var hasMore = true
            var totalProcessed = 0
            
            while (hasMore) {
                val response = syncService.pull(
                    since = syncCursor,
                    limit = 500,
                    token = nextToken
                )
                
                // Process upserts
                response.upserts.categories?.let { dtos ->
                    val categories = dtos.map { dto ->
                        com.tannous.pos.core.data.local.entity.CategoryEntity(
                            id = dto.id,
                            name = dto.name,
                            description = dto.description,
                            displayOrder = dto.displayOrder,
                            isActive = dto.isActive,
                            updatedAt = dto.updatedAt?.let { Instant.parse(it) } ?: Instant.now(),
                            isDeleted = dto.isDeleted ?: false
                        )
                    }
                    categoryDao.upsertAll(categories)
                    totalProcessed += categories.size
                }

                response.upserts.items?.let { dtos ->
                    val menuItems = dtos.map { dto ->
                        com.tannous.pos.core.data.local.entity.MenuItemEntity(
                            id = dto.id,
                            categoryId = dto.categoryId,
                            name = dto.name,
                            description = dto.description,
                            price = dto.price,
                            imageUrl = dto.imageUrl,
                            isActive = dto.isActive,
                            hasAddOns = dto.hasAddOns,
                            updatedAt = dto.updatedAt?.let { Instant.parse(it) } ?: Instant.now(),
                            isDeleted = dto.isDeleted ?: false,
                            version = dto.version
                        )
                    }
                    menuItemDao.upsertAll(menuItems)
                    totalProcessed += menuItems.size
                }

                response.upserts.addOns?.let { dtos ->
                    val addOns = dtos.map { dto ->
                        com.tannous.pos.core.data.local.entity.AddOnEntity(
                            id = dto.id,
                            name = dto.name,
                            price = dto.price,
                            isActive = dto.isActive,
                            updatedAt = dto.updatedAt?.let { Instant.parse(it) } ?: Instant.now(),
                            isDeleted = dto.isDeleted ?: false,
                            version = dto.version
                        )
                    }
                    addOnDao.upsertAll(addOns)
                    totalProcessed += addOns.size
                }

                response.upserts.customers?.let { dtos ->
                    val customers = dtos.map { dto ->
                        com.tannous.pos.core.data.local.entity.CustomerEntity(
                            id = dto.id,
                            firstName = dto.firstName,
                            lastName = dto.lastName,
                            email = dto.email,
                            phone = dto.phone,
                            address = dto.address,
                            notes = dto.notes,
                            allergies = dto.allergies,
                            isActive = dto.isActive,
                            lastVisitDate = dto.lastVisitDate?.let { Instant.parse(it) },
                            totalOrders = dto.totalOrders,
                            isDeleted = dto.isDeleted ?: false,
                            deletedAt = dto.deletedAt?.let { Instant.parse(it) },
                            version = dto.version
                        )
                    }
                    customerDao.upsertAll(customers)
                    totalProcessed += customers.size
                }

                // Process deletes (soft-delete in local DB)
                response.deletes.items?.forEach { id -> menuItemDao.markDeleted(id) }
                response.deletes.customers?.forEach { id -> customerDao.markDeleted(id) }

                nextToken = response.nextToken
                hasMore = response.hasMore
            }
            
            // Update sync cursor
            keyValueDao.set(com.tannous.pos.core.data.local.entity.KeyValueEntity(
                "sync_cursor",
                Instant.now().toString()
            ))
            
            Timber.d("Pull sync completed. Processed $totalProcessed entities")
            Result.success()
            
        } catch (e: CancellationException) {
            Timber.i("Pull sync cancelled")
            throw e
        } catch (e: Exception) {
            Timber.e(e, "Pull sync failed")
            Result.retry()
        }
    }
    
    companion object {
        fun enqueue(context: Context) {
            val constraints = Constraints.Builder()
                .setRequiredNetworkType(NetworkType.CONNECTED)
                .build()
            
            val request = PeriodicWorkRequestBuilder<PullWorker>(
                repeatInterval = 15,
                repeatIntervalTimeUnit = TimeUnit.MINUTES
            )
            .setConstraints(constraints)
            .build()
            
            WorkManager.getInstance(context).enqueueUniquePeriodicWork(
                "pull_sync",
                ExistingPeriodicWorkPolicy.KEEP,
                request
            )
        }
    }
}
