pluginManagement {
    repositories {
        google()
        mavenCentral()
        gradlePluginPortal()
    }
}

dependencyResolutionManagement {
    repositoriesMode.set(RepositoriesMode.FAIL_ON_PROJECT_REPOS)
    repositories {
        google()
        mavenCentral()
    }
}

rootProject.name = "TannousPOS"
include(":app")
include(":core")
include(":feature:auth")
include(":feature:sell")
include(":feature:shifts")
include(":feature:customers")
include(":feature:reports")
include(":feature:settings")
include(":feature:printing")
