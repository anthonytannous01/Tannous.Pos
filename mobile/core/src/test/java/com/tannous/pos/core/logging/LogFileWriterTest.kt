package com.tannous.pos.core.logging

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test
import org.junit.rules.TemporaryFolder
import java.io.File
import java.util.concurrent.TimeUnit

/**
 * The point of these tests is that the release logger cannot be silently broken. FileLogTree
 * swallows every exception by design, so without coverage here a logger that writes nothing looks
 * exactly like a logger with nothing to write.
 */
class LogFileWriterTest {

    @get:Rule
    val temp = TemporaryFolder()

    private val now = 1_756_000_000_000L // fixed instant; the tests only care about relative days

    private fun writerIn(dir: File, maxBytes: Long = 1024, retentionDays: Long = 7) =
        LogFileWriter(dir, maxFileBytes = maxBytes, retentionDays = retentionDays)

    @Test
    fun `writes the entry to today's file`() {
        val dir = temp.newFolder("logs")
        val writer = writerIn(dir)

        assertTrue(writer.write(now, "WARN", "SyncWorker", "outbox replay failed"))

        val contents = writer.fileFor(now).readText()
        assertTrue(contents.contains("WARN"))
        assertTrue(contents.contains("[SyncWorker]"))
        assertTrue(contents.contains("outbox replay failed"))
        assertTrue(contents.endsWith("\n"))
    }

    @Test
    fun `creates the log directory when it does not exist yet`() {
        val dir = File(temp.newFolder("root"), "logs")
        assertFalse(dir.exists())

        assertTrue(writerIn(dir).write(now, "ERROR", null, "printer unreachable"))
        assertTrue(dir.isDirectory)
    }

    @Test
    fun `includes the stack trace when one is supplied`() {
        val writer = writerIn(temp.newFolder("logs"))
        writer.write(now, "ERROR", "Print", "failed", "java.io.IOException: socket closed\n\tat Foo.bar(Foo.kt:1)")

        val contents = writer.fileFor(now).readText()
        assertTrue(contents.contains("java.io.IOException: socket closed"))
        assertTrue(contents.contains("at Foo.bar(Foo.kt:1)"))
    }

    @Test
    fun `omits the tag section when the tag is blank`() {
        val writer = writerIn(temp.newFolder("logs"))
        writer.write(now, "WARN", "   ", "no tag here")

        assertFalse(writer.fileFor(now).readText().contains("["))
    }

    @Test
    fun `entries on different days go to different files`() {
        val writer = writerIn(temp.newFolder("logs"))
        val twoDaysLater = now + TimeUnit.DAYS.toMillis(2)

        writer.write(now, "WARN", null, "day one")
        writer.write(twoDaysLater, "WARN", null, "day three")

        assertTrue(writer.fileFor(now).readText().contains("day one"))
        assertTrue(writer.fileFor(twoDaysLater).readText().contains("day three"))
        assertFalse(writer.fileFor(now).readText().contains("day three"))
    }

    @Test
    fun `entries on the same day append to one file`() {
        val writer = writerIn(temp.newFolder("logs"))
        writer.write(now, "WARN", null, "first")
        writer.write(now + 1_000, "WARN", null, "second")

        val contents = writer.fileFor(now).readText()
        assertTrue(contents.contains("first"))
        assertTrue(contents.contains("second"))
        assertEquals(2, contents.trim().lines().size)
    }

    @Test
    fun `stops writing once the file reaches the size cap and says so once`() {
        val writer = writerIn(temp.newFolder("logs"), maxBytes = 200)
        val filler = "x".repeat(120)

        assertTrue(writer.write(now, "WARN", null, filler))
        assertTrue(writer.write(now, "WARN", null, filler))
        // Third and fourth attempts land after the cap is reached.
        assertFalse(writer.write(now, "WARN", null, "dropped one"))
        assertFalse(writer.write(now, "WARN", null, "dropped two"))

        val contents = writer.fileFor(now).readText()
        assertFalse(contents.contains("dropped one"))
        assertEquals(1, contents.lines().count { it.contains("log capped") })
    }

    @Test
    fun `prune deletes logs past the retention window and keeps the rest`() {
        val dir = temp.newFolder("logs")
        val writer = writerIn(dir, retentionDays = 7)

        val old = File(dir, "pos-2020-01-01.log").apply {
            writeText("ancient\n")
            setLastModified(now - TimeUnit.DAYS.toMillis(30))
        }
        val recent = File(dir, "pos-2020-01-02.log").apply {
            writeText("recent\n")
            setLastModified(now - TimeUnit.DAYS.toMillis(2))
        }
        val unrelated = File(dir, "notes.txt").apply {
            writeText("keep me\n")
            setLastModified(now - TimeUnit.DAYS.toMillis(30))
        }

        assertEquals(1, writer.prune(now))
        assertFalse(old.exists())
        assertTrue(recent.exists())
        assertTrue(unrelated.exists())
    }

    @Test
    fun `reports failure instead of throwing when the directory cannot be created`() {
        val blocker = temp.newFile("not-a-directory")
        val writer = writerIn(File(blocker, "logs"))

        assertFalse(writer.write(now, "ERROR", null, "nowhere to go"))
    }
}
