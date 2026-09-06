package com.tannous.pos

import android.app.Application
import androidx.hilt.work.HiltWorkerFactory
import androidx.work.Configuration
import com.tannous.pos.core.logging.FileLogTree
import com.tannous.pos.core.sync.SyncManager
import dagger.hilt.android.HiltAndroidApp
import timber.log.Timber
import javax.inject.Inject

@HiltAndroidApp
class TannousPosApplication : Application(), Configuration.Provider {

    @Inject lateinit var workerFactory: HiltWorkerFactory
    @Inject lateinit var syncManager: SyncManager

    override val workManagerConfiguration: Configuration
        get() = Configuration.Builder()
            .setWorkerFactory(workerFactory)
            .build()

    override fun onCreate() {
        super.onCreate()

        // Release builds previously planted nothing, so every Timber.w and Timber.e in production
        // was discarded and a failing tablet left no diagnostic trace at all.
        if (BuildConfig.DEBUG) {
            Timber.plant(Timber.DebugTree())
        } else {
            Timber.plant(FileLogTree(this))
        }

        Timber.d("Tannous POS Application initialized - Environment: ${BuildConfig.ENVIRONMENT}")

        // Start background sync workers (pull every 15 min, push every 5 min)
        syncManager.schedulePeriodicSync()
    }
}
