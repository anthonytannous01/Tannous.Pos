package com.tannous.pos

import android.app.Application
import dagger.hilt.android.HiltAndroidApp
import timber.log.Timber

@HiltAndroidApp
class TannousPosApplication : Application() {
    
    override fun onCreate() {
        super.onCreate()
        
        // Initialize Timber
        if (BuildConfig.DEBUG) {
            Timber.plant(Timber.DebugTree())
        }
        
        Timber.d("Tannous POS Application initialized - Environment: ${BuildConfig.ENVIRONMENT}")
    }
}
