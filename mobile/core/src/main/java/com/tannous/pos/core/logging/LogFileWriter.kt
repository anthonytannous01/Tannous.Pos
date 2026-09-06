package com.tannous.pos.core.logging

import java.io.File
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

/**
 * File-side of on-device diagnostic logging: naming, formatting, size cap and retention.
 *
 * Deliberately free of Android types so it can be exercised by ordinary JVM tests. The previous
 * arrangement - all of this inline in a Timber tree that caught and discarded every exception -
 * could have been broken from the day it shipped without anything saying so, which is the same
 * failure mode as the inert Crashlytics SDKs this replaced.
 *
 * Not thread-safe. [FileLogTree] serialises every call onto one executor thread.
 */
class LogFileWriter(
    private val directory: File,
    private val maxFileBytes: Long = DEFAULT_MAX_FILE_BYTES,
    private val retentionDays: Long = DEFAULT_RETENTION_DAYS
) {

    private val fileNameFormat = SimpleDateFormat("yyyy-MM-dd", Locale.US)
    private val timestampFormat = SimpleDateFormat("yyyy-MM-dd HH:mm:ss.SSS", Locale.US)

    /** Paths already given a truncation marker, so it is written once rather than per dropped entry. */
    private val capped = mutableSetOf<String>()

    /** One file per calendar day, so a day's worth of trouble is one file to pull off the tablet. */
    fun fileFor(occurredAtMillis: Long): File =
        File(directory, FILE_PREFIX + fileNameFormat.format(Date(occurredAtMillis)) + FILE_SUFFIX)

    fun format(
        occurredAtMillis: Long,
        level: String,
        tag: String?,
        message: String,
        stackTrace: String?
    ): String = buildString {
        append(timestampFormat.format(Date(occurredAtMillis)))
        append(' ').append(level)
        if (!tag.isNullOrBlank()) append(" [").append(tag).append(']')
        append(' ').append(message).append('\n')
        if (!stackTrace.isNullOrBlank()) append(stackTrace.trimEnd()).append('\n')
    }

    /**
     * Appends one entry. Returns false when the entry was not written: the directory could not be
     * created, or today's file has reached [maxFileBytes]. A full file is capped rather than
     * rotated mid-day so a log storm cannot fill a tablet's storage during service.
     */
    fun write(
        occurredAtMillis: Long,
        level: String,
        tag: String?,
        message: String,
        stackTrace: String? = null
    ): Boolean {
        if (!directory.exists() && !directory.mkdirs()) return false

        val file = fileFor(occurredAtMillis)
        if (file.length() >= maxFileBytes) {
            if (capped.add(file.path)) {
                file.appendText("--- log capped at $maxFileBytes bytes; further entries dropped ---\n")
            }
            return false
        }

        file.appendText(format(occurredAtMillis, level, tag, message, stackTrace))
        return true
    }

    /** Deletes log files last modified before the retention window. Returns how many went. */
    fun prune(nowMillis: Long): Int {
        val cutoff = nowMillis - retentionDays * MILLIS_PER_DAY
        val stale = directory.listFiles()
            ?.filter { it.isFile && it.name.startsWith(FILE_PREFIX) && it.name.endsWith(FILE_SUFFIX) }
            ?.filter { it.lastModified() < cutoff }
            ?: return 0

        var deleted = 0
        for (file in stale) {
            if (file.delete()) {
                capped.remove(file.path)
                deleted++
            }
        }
        return deleted
    }

    companion object {
        const val FILE_PREFIX = "pos-"
        const val FILE_SUFFIX = ".log"
        const val DEFAULT_MAX_FILE_BYTES = 2L * 1024 * 1024
        const val DEFAULT_RETENTION_DAYS = 7L
        private const val MILLIS_PER_DAY = 24L * 60 * 60 * 1000
    }
}
