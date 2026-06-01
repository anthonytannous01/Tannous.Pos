package com.tannous.pos.core.di

import android.content.Context
import androidx.work.*
import com.tannous.pos.core.sync.PullWorker
import com.tannous.pos.core.sync.PushWorker
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.android.qualifiers.ApplicationContext
import dagger.hilt.components.SingletonComponent
import java.util.concurrent.TimeUnit
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object WorkManagerModule {
    
    @Provides
    @Singleton
    fun provideWorkManager(@ApplicationContext context: Context): WorkManager {
        return WorkManager.getInstance(context)
    }
    
    @Provides
    @Singleton
    fun providePullWorkerConstraints(): Constraints {
        return Constraints.Builder()
            .setRequiredNetworkType(NetworkType.CONNECTED)
            .build()
    }
    
    @Provides
    @Singleton
    fun providePushWorkerConstraints(): Constraints {
        return Constraints.Builder()
            .setRequiredNetworkType(NetworkType.CONNECTED)
            .build()
    }
    
    @Provides
    @Singleton
    fun providePeriodicPullWorkRequest(constraints: Constraints): PeriodicWorkRequest {
        return PeriodicWorkRequestBuilder<PullWorker>(
            repeatInterval = 15,
            repeatIntervalTimeUnit = TimeUnit.MINUTES
        )
        .setConstraints(constraints)
        .setBackoffCriteria(
            BackoffPolicy.EXPONENTIAL,
            WorkRequest.MIN_BACKOFF_MILLIS,
            TimeUnit.MILLISECONDS
        )
        .build()
    }
    
    @Provides
    @Singleton
    fun providePeriodicPushWorkRequest(constraints: Constraints): PeriodicWorkRequest {
        return PeriodicWorkRequestBuilder<PushWorker>(
            repeatInterval = 5,
            repeatIntervalTimeUnit = TimeUnit.MINUTES
        )
        .setConstraints(constraints)
        .setBackoffCriteria(
            BackoffPolicy.EXPONENTIAL,
            WorkRequest.MIN_BACKOFF_MILLIS,
            TimeUnit.MILLISECONDS
        )
        .build()
    }
}
