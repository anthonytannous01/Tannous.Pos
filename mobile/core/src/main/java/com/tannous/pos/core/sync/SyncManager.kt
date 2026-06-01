package com.tannous.pos.core.sync

import androidx.work.*
import com.tannous.pos.core.sync.PullWorker
import com.tannous.pos.core.sync.PushWorker
import timber.log.Timber
import java.util.concurrent.TimeUnit
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class SyncManager @Inject constructor(
    private val workManager: WorkManager
) {
    
    companion object {
        private const val PULL_WORK_NAME = "sync_pull_periodic"
        private const val PUSH_WORK_NAME = "sync_push_periodic"
    }
    
    fun schedulePeriodicSync() {
        Timber.d("Scheduling periodic sync workers")
        
        // Schedule periodic pull (every 15 minutes)
        val pullWorkRequest = PeriodicWorkRequestBuilder<PullWorker>(
            repeatInterval = 15,
            repeatIntervalTimeUnit = TimeUnit.MINUTES
        )
        .setConstraints(
            Constraints.Builder()
                .setRequiredNetworkType(NetworkType.CONNECTED)
                .build()
        )
        .setBackoffCriteria(
            BackoffPolicy.EXPONENTIAL,
            WorkRequest.MIN_BACKOFF_MILLIS,
            TimeUnit.MILLISECONDS
        )
        .build()
        
        workManager.enqueueUniquePeriodicWork(
            PULL_WORK_NAME,
            ExistingPeriodicWorkPolicy.KEEP,
            pullWorkRequest
        )
        
        // Schedule periodic push (every 5 minutes)
        val pushWorkRequest = PeriodicWorkRequestBuilder<PushWorker>(
            repeatInterval = 5,
            repeatIntervalTimeUnit = TimeUnit.MINUTES
        )
        .setConstraints(
            Constraints.Builder()
                .setRequiredNetworkType(NetworkType.CONNECTED)
                .build()
        )
        .setBackoffCriteria(
            BackoffPolicy.EXPONENTIAL,
            WorkRequest.MIN_BACKOFF_MILLIS,
            TimeUnit.MILLISECONDS
        )
        .build()
        
        workManager.enqueueUniquePeriodicWork(
            PUSH_WORK_NAME,
            ExistingPeriodicWorkPolicy.KEEP,
            pushWorkRequest
        )
        
        Timber.d("Periodic sync workers scheduled")
    }
    
    fun triggerImmediatePull() {
        Timber.d("Triggering immediate pull sync")
        val pullWorkRequest = OneTimeWorkRequestBuilder<PullWorker>()
            .setConstraints(
                Constraints.Builder()
                    .setRequiredNetworkType(NetworkType.CONNECTED)
                    .build()
            )
            .setBackoffCriteria(
                BackoffPolicy.EXPONENTIAL,
                WorkRequest.MIN_BACKOFF_MILLIS,
                TimeUnit.MILLISECONDS
            )
            .build()
        
        workManager.enqueue(pullWorkRequest)
    }
    
    fun triggerImmediatePush() {
        Timber.d("Triggering immediate push sync")
        val pushWorkRequest = OneTimeWorkRequestBuilder<PushWorker>()
            .setConstraints(
                Constraints.Builder()
                    .setRequiredNetworkType(NetworkType.CONNECTED)
                    .build()
            )
            .setBackoffCriteria(
                BackoffPolicy.EXPONENTIAL,
                WorkRequest.MIN_BACKOFF_MILLIS,
                TimeUnit.MILLISECONDS
            )
            .build()
        
        workManager.enqueue(pushWorkRequest)
    }
    
    fun cancelAllSync() {
        Timber.d("Cancelling all sync workers")
        workManager.cancelUniqueWork(PULL_WORK_NAME)
        workManager.cancelUniqueWork(PUSH_WORK_NAME)
    }
}
