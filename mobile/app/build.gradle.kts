plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.android)
    alias(libs.plugins.kotlin.serialization)
    alias(libs.plugins.hilt)
    alias(libs.plugins.ksp)
    // Temporarily commented out for basic build
    // alias(libs.plugins.firebase.crashlytics)
    // alias(libs.plugins.google.services)
    // alias(libs.plugins.gradle.play.publisher)
    // alias(libs.plugins.firebase.app.distribution)
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
            val keystoreFile = project.findProperty("RELEASE_STORE_FILE") as String?
            val keystorePassword = project.findProperty("RELEASE_STORE_PASSWORD") as String?
            val keyAlias = project.findProperty("RELEASE_KEY_ALIAS") as String?
            val keyPassword = project.findProperty("RELEASE_KEY_PASSWORD") as String?
            
            if (keystoreFile != null && keystorePassword != null && keyAlias != null && keyPassword != null) {
                storeFile = file(keystoreFile)
                storePassword = keystorePassword
                this.keyAlias = keyAlias
                this.keyPassword = keyPassword
            }
        }
        
        create("ciRelease") {
            storeFile = file("${rootDir}/ci/keystore.jks")
            storePassword = System.getenv("ANDROID_KEYSTORE_PASSWORD") ?: ""
            keyAlias = System.getenv("ANDROID_KEY_ALIAS") ?: ""
            keyPassword = System.getenv("ANDROID_KEY_PASSWORD") ?: ""
        }
    }
    
    buildTypes {
        release {
            isMinifyEnabled = true
            resValue("bool", "cleartext_permitted", "false")
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

    flavorDimensions += "environment"
    productFlavors {
        create("dev") {
            dimension = "environment"
            applicationIdSuffix = ".dev"
            buildConfigField("String", "BASE_URL", "\"http://192.168.0.121:7000/api/v1.0/\"")
            buildConfigField("String", "ENVIRONMENT", "\"development\"")
        }
        create("staging") {
            dimension = "environment"
            applicationIdSuffix = ".staging"
            buildConfigField("String", "BASE_URL", "\"https://staging-api.tannouspos.com/api/v1.0/\"")
            buildConfigField("String", "ENVIRONMENT", "\"staging\"")
        }
        create("prod") {
            dimension = "environment"
            buildConfigField("String", "BASE_URL", "\"https://api.tannouspos.com/api/v1.0/\"")
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

// Firebase App Distribution Configuration - Commented out for now
// firebaseAppDistribution {
//     serviceCredentialsFile = "${rootDir}/ci/firebase-service-account.json"
//     groups = "qa" // or set testers via CI
//     releaseNotesFile = "${rootDir}/ci/release-notes.txt"
// }

dependencies {
    implementation(project(":core"))
    implementation(project(":feature:auth"))
    implementation(project(":feature:sell"))
    implementation(project(":feature:shifts"))
    implementation(project(":feature:customers"))
    implementation(project(":feature:reports"))
    implementation(project(":feature:settings"))
    implementation(project(":feature:printing"))
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
    
    // Firebase - Temporarily commented out for basic build
    // implementation(platform(libs.firebase.bom))
    // implementation(libs.firebase.crashlytics)
    // implementation(libs.firebase.analytics)
    
    // Testing
    testImplementation(libs.androidx.test.ext.junit)
    androidTestImplementation(libs.androidx.test.espresso.core)
    androidTestImplementation(platform(libs.androidx.compose.bom))
    androidTestImplementation(libs.androidx.compose.ui.test)
    debugImplementation(libs.androidx.compose.ui.tooling)
    debugImplementation(libs.androidx.compose.ui.test.manifest)
}
