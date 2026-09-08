import java.io.File
import java.io.FileInputStream
import java.util.Properties

plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.android)
    alias(libs.plugins.kotlin.serialization)
    alias(libs.plugins.hilt)
    alias(libs.plugins.ksp)
    // alias(libs.plugins.gradle.play.publisher)
}

android {
    namespace = "com.tannous.pos"
    compileSdk = 34

    defaultConfig {
        applicationId = "com.tannous.pos"
        minSdk = 26
        targetSdk = 34
        versionCode = (rootProject.extra["versionCode"] as? Long)?.toInt() ?: 1
        versionName = rootProject.extra["versionName"] as? String ?: "1.0.0"

        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
        vectorDrawables {
            useSupportLibrary = true
        }
    }

    signingConfigs {
        create("release") {
            // Signing values come from local.properties (gitignored, per project) or from a Gradle
            // property, with the Gradle property winning so -P and CI can override.
            //
            // Reading local.properties has to be explicit: Gradle does NOT expose it as project
            // properties. The Android plugin reads sdk.dir out of it and core/build.gradle.kts
            // parses it by hand for API_BASE_URL, which made it look as though findProperty would
            // see RELEASE_STORE_FILE too. It never did. PLAY_STORE_READINESS.md documented
            // local.properties as the place to put these for months while the build silently read
            // ~/.gradle/gradle.properties instead, and nobody noticed because no release build had
            // ever been attempted.
            val localPropsFile = rootProject.file("local.properties")
            val localProps = Properties().apply {
                if (localPropsFile.exists()) FileInputStream(localPropsFile).use { load(it) }
            }
            fun signingValue(key: String): String? =
                (project.findProperty(key) as String?) ?: localProps.getProperty(key)

            val keystorePath     = signingValue("RELEASE_STORE_FILE")
            val keystorePassword = signingValue("RELEASE_STORE_PASSWORD")
            val releaseKeyAlias  = signingValue("RELEASE_KEY_ALIAS")
            val releaseKeyPass   = signingValue("RELEASE_KEY_PASSWORD")

            if (keystorePath != null && keystorePassword != null &&
                releaseKeyAlias != null && releaseKeyPass != null
            ) {
                // Relative paths resolve against the repo's mobile/ root, not app/, so
                // "keystore/tannous-pos-release.jks" means the same thing wherever it is written.
                val resolved = File(keystorePath).let {
                    if (it.isAbsolute) it else rootProject.file(keystorePath)
                }
                if (!resolved.exists()) {
                    // Plain concatenation, no string templates: this message names three file
                    // paths and template syntax is one more thing to get wrong in a build script
                    // that only fails at configuration time.
                    throw GradleException(
                        "Release keystore not found at " + resolved.absolutePath + ". " +
                        "RELEASE_STORE_FILE resolved to that path. Set it in " +
                        localPropsFile.absolutePath +
                        ", or check whether a stale entry in ~/.gradle/gradle.properties is " +
                        "overriding it - a Gradle property wins over local.properties. " +
                        "If the keystore itself is missing, stop: no installed copy of this app " +
                        "can ever be updated without it."
                    )
                }
                storeFile     = resolved
                storePassword = keystorePassword
                keyAlias      = releaseKeyAlias
                keyPassword   = releaseKeyPass
            } else {
                // Debug builds do not need signing config, so this is not fatal here. The release
                // build fails at validateSigning instead, which is the correct moment.
                logger.lifecycle(
                    "Release signing not configured: set RELEASE_STORE_FILE, " +
                    "RELEASE_STORE_PASSWORD, RELEASE_KEY_ALIAS and RELEASE_KEY_PASSWORD in " +
                    "mobile/local.properties. Debug builds are unaffected."
                )
            }
        }
    }
    
    buildTypes {
        release {
            isMinifyEnabled = true
            // Cleartext HTTP is permitted in release because the POS talks to a server on the
            // restaurant's own LAN (http://<host>:7000, set as API_BASE_URL in local.properties)
            // and that server has no certificate. This was false until Step 130, which would have
            // made every network call in a release build fail: the release APK would install
            // cleanly and then do nothing.
            //
            // Be clear about what this costs: traffic between the tablets and the server is
            // unencrypted, and that traffic includes staff login credentials. Anyone on the same
            // Wi-Fi can read them. Scoping the exception to one host would not change that. The
            // fix is HTTPS on the backend; until then, keep the POS devices and the server on a
            // network separate from the guest Wi-Fi.
            resValue("bool", "cleartext_permitted", "true")
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
            signingConfig = signingConfigs.getByName("release")
        }
        debug {
            isDebuggable = true
            resValue("bool", "cleartext_permitted", "true")
        }
    }

    // NOTE: these flavours deliberately do NOT define BASE_URL. NetworkModule reads
    // com.tannous.pos.core.BuildConfig.BASE_URL, which core/build.gradle.kts fills from
    // API_BASE_URL in local.properties. Flavour BASE_URL fields existed here for months
    // (including https://api.tannouspos.com for prod) and were never read by anything.
    // ENVIRONMENT below is different - TannousPosApplication does read the app BuildConfig.
    flavorDimensions += "environment"
    productFlavors {
        create("dev") {
            dimension = "environment"
            applicationIdSuffix = ".dev"
            buildConfigField("String", "ENVIRONMENT", "\"development\"")
        }
        create("staging") {
            dimension = "environment"
            applicationIdSuffix = ".staging"
            buildConfigField("String", "ENVIRONMENT", "\"staging\"")
        }
        create("prod") {
            dimension = "environment"
            buildConfigField("String", "ENVIRONMENT", "\"production\"")
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = "17"
    }

    buildFeatures {
        compose = true
        buildConfig = true
    }

    composeOptions {
        kotlinCompilerExtensionVersion = libs.versions.compose.compiler.get()
    }

    packaging {
        resources {
            excludes += "/META-INF/{AL2.0,LGPL2.1}"
        }
    }
    
    lint {
        // Report lint issues in release builds but don't block — tighten after QA pass
        abortOnError = false
        checkReleaseBuilds = true
        htmlReport = true
        xmlReport = true
    }
}

// Play Publisher Configuration - Commented out for now
// play {
//     serviceAccountCredentials.set(file("${rootDir}/ci/play-service-account.json"))
//     defaultToAppBundles.set(true)
//     track.set("internal") // default; overridden via CI inputs
//     releaseStatus.set("completed")
// }

dependencies {
    implementation(project(":core"))
    implementation(project(":feature:auth"))
    implementation(project(":feature:sell"))
    implementation(project(":feature:shifts"))
    implementation(project(":feature:customers"))
    implementation(project(":feature:reports"))
    implementation(project(":feature:settings"))
    implementation(project(":feature:inventory"))

    implementation(libs.androidx.core.ktx)
    implementation(libs.androidx.lifecycle.runtime.ktx)
    implementation(libs.androidx.activity.compose)
    implementation(libs.androidx.core.splashscreen)
    implementation(platform(libs.androidx.compose.bom))
    implementation(libs.androidx.compose.ui)
    implementation(libs.androidx.compose.ui.graphics)
    implementation(libs.androidx.compose.ui.tooling.preview)
    implementation(libs.androidx.compose.material3)
    implementation(libs.androidx.compose.material)
    implementation(libs.navigation.compose)
    implementation(libs.androidx.material3)

    // Hilt
    implementation(libs.hilt.android)
    ksp(libs.hilt.compiler)
    implementation(libs.hilt.navigation.compose)
    implementation(libs.androidx.lifecycle.runtime.compose)

    implementation("com.github.DantSu:ESCPOS-ThermalPrinter-Android:3.3.0")

    // Hilt WorkManager integration (needed in app module for HiltWorkerFactory)
    implementation(libs.androidx.hilt.work)
    ksp(libs.androidx.hilt.compiler)

    // WorkManager (needed in app module for Configuration.Provider)
    implementation(libs.workmanager.ktx)

    // Logging
    implementation(libs.timber)

    // Testing
    testImplementation(libs.androidx.test.ext.junit)
    androidTestImplementation(libs.androidx.test.espresso.core)
    androidTestImplementation(platform(libs.androidx.compose.bom))
    androidTestImplementation(libs.androidx.compose.ui.test)
    debugImplementation(libs.androidx.compose.ui.tooling)
    debugImplementation(libs.androidx.compose.ui.test.manifest)
}
