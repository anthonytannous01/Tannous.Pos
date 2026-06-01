package com.tannous.pos.core.logging

import android.util.Log
import com.google.firebase.crashlytics.FirebaseCrashlytics
import timber.log.Timber
import java.util.*

class CrashlyticsTree : Timber.Tree() {
    
    override fun log(priority: Int, tag: String?, message: String, t: Throwable?) {
        if (priority == Log.VERBOSE || priority == Log.DEBUG) {
            return // Skip verbose and debug logs in release
        }
        
        val crashlytics = FirebaseCrashlytics.getInstance()
        
        // Set custom keys for better crash analysis
        crashlytics.setCustomKey("log_priority", priority)
        crashlytics.setCustomKey("log_tag", tag ?: "NO_TAG")
        crashlytics.setCustomKey("timestamp", Date().time)
        
        // Log the message
        crashlytics.log("$tag: $message")
        
        // If there's a throwable, record it
        t?.let { throwable ->
            crashlytics.recordException(throwable)
        }
        
        // For errors and warnings, also record as non-fatal
        if (priority >= Log.WARN) {
            crashlytics.recordException(
                RuntimeException("Log: $message").apply {
                    stackTrace = Thread.currentThread().stackTrace
                }
            )
        }
    }
}
