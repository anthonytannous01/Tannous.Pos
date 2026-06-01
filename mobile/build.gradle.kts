// Top-level build file where you can add configuration options common to all sub-projects/modules.
// Plugins are now managed through the version catalog in gradle/libs.versions.toml

plugins {
    alias(libs.plugins.android.application) apply false
    alias(libs.plugins.android.library) apply false
    alias(libs.plugins.kotlin.android) apply false
    alias(libs.plugins.kotlin.serialization) apply false
    alias(libs.plugins.hilt) apply false
    alias(libs.plugins.ksp) apply false
}

tasks.register("clean", Delete::class) {
    delete(rootProject.buildDir)
}

// Apply versioning automation
apply(from = "versioning.gradle.kts")

// Work around JDK image transformation issues
allprojects {
    tasks.withType<JavaCompile> {
        options.isFork = false
    }
}
