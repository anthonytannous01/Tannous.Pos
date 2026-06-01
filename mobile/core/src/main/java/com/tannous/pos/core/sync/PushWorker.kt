package com.tannous.pos.core.sync

import android.content.Context
import androidx.hilt.work.HiltWorker
import androidx.work.*
import com.tannous.pos.core.data.local.dao.OutboxDao
import com.tannous.pos.core.data.local.entity.OutboxStatus
import com.tannous.pos.core.data.remote.SyncService
import dagger.assisted.Assisted
import dagger.assisted.AssistedInject
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.withContext
import timber.log.Timber
import java.util.concurrent.TimeUnit

@HiltWorker
class PushWorker @AssistedInject constructor(
    @Assisted appContext: Context,
    @Assisted workerParams: WorkerParameters,
    private val syncService: SyncService,
    private val outboxDao: OutboxDao
) : CoroutineWorker(appContext, workerParams) {
    
    override suspend fun doWork(): Result = withContext(Dispatchers.IO) {
        try {
            Timber.d("Starting push sync...")
            
            val pendingOperations = outboxDao.getPendingOperations(OutboxStatus.PENDING, 20)
            
            if (pendingOperations.isEmpty()) {
                Timber.d("No pending operations to push")
                return@withContext Result.success()
            }
            
            val operations = pendingOperations.map { entity ->
                com.tannous.pos.core.data.model.OutboxOperationDto(
                    operationId = entity.operationId,
                    type = entity.type,
                    payload = entity.payloadJson,
                    createdAt = entity.createdAt.toString()
                )
            }
            
            val request = com.tannous.pos.core.data.model.SyncPushRequest(operations)
            val response = syncService.push(request)
            
            // Process results
            response.results.forEach { result ->
                when {
                    result.success -> {
                        outboxDao.updateStatus(result.operationId, OutboxStatus.SENT)
                    }
                    result.conflict -> {
                        outboxDao.updateStatus(
                            result.operationId,
                            OutboxStatus.FAILED_CONFLICT,
                            result.serverEntity
                        )
                    }
                    else -> {
                        outboxDao.updateStatus(
                            result.operationId,
                            OutboxStatus.FAILED,
                            result.error
                        )
                    }
                }
            }
            
            Timber.d("Push sync completed. Processed ${operations.size} operations")
            Result.success()
            
        } catch (e: CancellationException) {
            Timber.i("Push sync cancelled")
            throw e
        } catch (e: Exception) {
            Timber.e(e, "Push sync failed")
            Result.retry()
        }
    }
    
    companion object {
        fun enqueue(context: Context) {
            val constraints = Constraints.Builder()
                .setRequiredNetworkType(NetworkType.CONNECTED)
                .build()
            
            val request = OneTimeWorkRequestBuilder<PushWorker>()
                .setConstraints(constraints)
                .setBackoffCriteria(BackoffPolicy.EXPONENTIAL, 30, TimeUnit.SECONDS)
                .build()
            
            WorkManager.getInstance(context).enqueueUniqueWork(
                "push_sync",
                ExistingWorkPolicy.REPLACE,
                request
            )
        }
    }
}
