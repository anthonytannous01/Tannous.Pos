package com.tannous.pos.core.logging

import android.content.Context
import android.util.Log
import timber.log.Timber
import java.io.File
import java.util.concurrent.Executors

/**
 * Plants warnings and errors into a rotating log file on the device.
 *
 * Release builds previously planted no Timber tree at all, so every Timber.w and Timber.e in
 * production was discarded: sync failures, print failures, order errors, all of it. A tablet that
 * misbehaved during service left no trace anywhere.
 *
 * A file was chosen over a server endpoint deliberately. The failures worth diagnosing here are
 * offline sync problems and printer faults, which happen precisely when the network is the thing
 * that is broken - a sink that needs an API call cannot report that it could not reach the API.
 * The file also survives a backend outage and needs no endpoint, auth, or rate limiting.
 *
 * Logs live in app-scoped external storage, so they can be pulled over USB without root and are
 * removed when the app is uninstalled.
 *
 * **Do not log customer data.** These files sit on a tablet in a restaurant and can be read by
 * anyone with physical access. The app's log statements use identifiers - order id, device id -
 * rather than names, phone numbers or the free-text notes on a customer record. Keep it that way.
 *
 * Everything about file naming, size limits and retention lives in [LogFileWriter], which is
 * plain JVM code and covered by tests. This class is the Android shell around it.
 */
class FileLogTree(context: Context) : Timber.Tree() {

    private val writerThread = Executors.newSingleThreadExecutor { runnable ->
        Thread(runnable, "pos-file-log").apply { isDaemon = true }
    }

    private val logFiles: LogFileWriter? =
        logDirectory(context)?.let { LogFileWriter(it) }

    init {
        // Prune on startup rather than on every write.
        logFiles?.let { files ->
            writerThread.execute { runCatching { files.prune(System.currentTimeMillis()) } }
        }
    }

    override fun isLoggable(tag: String?, priority: Int): Boolean =
        logFiles != null && priority >= Log.WARN

    override fun log(priority: Int, tag: String?, message: String, t: Throwable?) {
        val files = logFiles ?: return

        // Read on the calling thread so the entry reflects when it happened, not when it flushed.
        val occurredAt = System.currentTimeMillis()
        val level = priorityLabel(priority)
        val stackTrace = t?.let { Log.getStackTraceString(it) }

        writerThread.execute {
            // Logging must never take the app down, and a failed write is not worth reporting to
            // a logger that is itself the thing failing.
            runCatching { files.write(occurredAt, level, tag, message, stackTrace) }
        }
    }

    private fun priorityLabel(priority: Int): String = when (priority) {
        Log.WARN -> "WARN"
        Log.ERROR -> "ERROR"
        Log.ASSERT -> "WTF"
        else -> "INFO"
    }

    companion object {
        /**
         * Directory holding the log files, or null when no storage is available. Exposed so a
         * future "share diagnostics" action can attach them without duplicating the path.
         */
        fun logDirectory(context: Context): File? {
            val appContext = context.applicationContext
            val external = appContext.getExternalFilesDir("logs")
            if (external != null) return external
            return File(appContext.filesDir, "logs")
        }
    }
}
