import java.time.LocalDate
import java.time.temporal.ChronoUnit

// Versioning automation for Tannous POS Android App
// This script automatically calculates versionName and versionCode from Git tags

// Try to get Git tag version, fallback to default if no tags exist
val gitTagVersion = try {
    providers.exec {
        commandLine("git", "describe", "--tags", "--abbrev=0")
    }.standardOutput.asText.get().trim()
} catch (e: Exception) {
    "v0.0.1" // Fallback version if no tags exist
}

val gitCommitCount = try {
    providers.exec {
        commandLine("git", "rev-list", "--count", "HEAD")
    }.standardOutput.asText.get().trim().toInt()
} catch (e: Exception) {
    0 // Fallback if git command fails
}

val gitLastTagCommitCount = try {
    providers.exec {
        commandLine("git", "rev-list", "--count", gitTagVersion)
    }.standardOutput.asText.get().trim().toInt()
} catch (e: Exception) {
    0 // Fallback if git command fails
}

val hasNewCommits = gitCommitCount > gitLastTagCommitCount

// Parse version from Git tag (e.g., "v1.2.3" -> "1.2.3")
val versionName = if (gitTagVersion.startsWith("v")) {
    gitTagVersion.substring(1)
} else {
    gitTagVersion
}

// Calculate version code based on Unix epoch days since 2024-01-01
val epochStart = LocalDate.of(2024, 1, 1)
val currentDate = LocalDate.now()
val epochDays = ChronoUnit.DAYS.between(epochStart, currentDate)

// If there are new commits since last tag, increment patch version
val finalVersionName = if (hasNewCommits) {
    val versionParts = versionName.split(".")
    if (versionParts.size >= 3) {
        val major = versionParts[0]
        val minor = versionParts[1]
        val patch = versionParts[2].toInt() + 1
        "$major.$minor.$patch"
    } else {
        versionName
    }
} else {
    versionName
}

// Version code: epoch days * 1000 + commit count (ensures uniqueness)
val versionCode = epochDays * 1000 + gitCommitCount

// Set project properties for use in build.gradle.kts
project.extra["versionName"] = finalVersionName
project.extra["versionCode"] = versionCode

// Log version information
println("=== Version Information ===")
println("Git Tag: $gitTagVersion")
println("Version Name: $finalVersionName")
println("Version Code: $versionCode")
println("Epoch Days: $epochDays")
println("Commit Count: $gitCommitCount")
println("Has New Commits: $hasNewCommits")
println("========================")

// Create version info file for CI
file("${project.rootDir}/ci/version-info.txt").apply {
    parentFile.mkdirs()
    writeText("""
        VERSION_NAME=$finalVersionName
        VERSION_CODE=$versionCode
        GIT_TAG=$gitTagVersion
        COMMIT_COUNT=$gitCommitCount
        EPOCH_DAYS=$epochDays
        BUILD_DATE=${LocalDate.now()}
    """.trimIndent())
}
