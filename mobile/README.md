# Tannous POS - Android App

A production-ready Android POS application with offline-first design, real-time sync, and comprehensive shift management.

## 🚀 Features

### Core POS Functionality
- **Offline Sales**: Complete sales flow works without internet connection
- **Real-time Sync**: Bidirectional sync with conflict resolution
- **Shift Management**: Open, cash drop, and close shifts with variance tracking
- **Inventory Management**: Real-time stock updates and wastage tracking
- **Customer Management**: Paged customer list with search and conflict resolution
- **Receipt Printing**: Preview, copy, share, and print functionality

### Technical Features
- **MVVM Architecture**: Clean separation of concerns with ViewModels
- **Room Database**: Local data persistence with offline-first approach
- **WorkManager**: Background sync with network constraints and retry logic
- **Hilt DI**: Dependency injection for maintainable code
- **Jetpack Compose**: Modern UI with Material 3 design
- **Retrofit + OkHttp**: Network layer with custom interceptors

### Network Features
- **JWT Authentication**: Secure API access with token management
- **Device Identification**: Unique device ID for multi-device support
- **Idempotency**: Prevents duplicate operations with unique keys
- **ETag Caching**: Efficient data synchronization for master data
- **Rate Limiting**: Handles 429 responses with exponential backoff
- **Conflict Resolution**: UI for resolving data conflicts between local and server

## 🏗️ Architecture

```
app/                    # Main application module
├── src/main/
│   ├── java/com/tannous/pos/
│   │   ├── MainActivity.kt
│   │   ├── TannousPosApplication.kt
│   │   └── TannousPosApp.kt
│   └── res/            # Resources and themes
├── build.gradle.kts    # App-level dependencies

core/                   # Core functionality module
├── data/
│   ├── local/          # Room database, entities, DAOs
│   ├── remote/         # Retrofit services, interceptors
│   ├── repository/     # Repository implementations
│   └── model/          # Data models and DTOs
├── di/                 # Hilt modules
├── sync/               # WorkManager workers and sync logic
└── ui/                 # Shared UI components

feature/                # Feature modules
├── auth/               # Login and authentication
├── sell/               # Main sales interface
├── shifts/             # Shift management
├── customers/          # Customer management
├── settings/           # App settings and configuration
└── printing/           # Receipt preview and printing
```

## 🛠️ Setup Instructions

### Prerequisites
- Android Studio Hedgehog or later
- Android SDK 34
- Kotlin 1.9+
- Java 17+

### CI/CD Setup
The project includes a complete CI/CD pipeline for automated builds, testing, and distribution. See [CI Setup Guide](docs/ci/ANDROID_CI_SETUP.md) for detailed configuration instructions.

**Quick Setup:**
1. Add required GitHub secrets (see CI setup guide)
2. Push to `main` branch for QA distribution
3. Push to `release/*` branch for Play Console publishing

**Available Commands:**
```bash
# Development
make dev-build          # Development debug build
make staging-aab        # Staging release AAB
make prod-aab           # Production release AAB

# Distribution
make distribute-staging # Upload to Firebase App Distribution
make publish-internal   # Publish to Play Console internal track

# CI/CD
make changelog          # Generate release notes
make ci-setup           # Setup CI environment
```

### Environment Configuration

#### 1. Emulator Setup
```bash
# Start Android emulator
emulator -avd <your_avd_name>

# The app automatically uses: http://10.0.2.2:7000/api/v1.0
```

#### 2. Physical Device Setup
```bash
# Enable USB debugging on device
# Connect device and run:
adb reverse tcp:7000 tcp:7000

# The app will use: http://localhost:7000/api/v1.0
```

#### 3. Backend Requirements
- Backend must be running on `http://localhost:7000`
- Database must be seeded with production-like data
- All API endpoints must be available and functional

### Build Variants

#### Development (dev)
```bash
./gradlew assembleDevDebug
# Uses: http://10.0.2.2:7000/api/v1.0
```

#### Production (prod)
```bash
./gradlew assembleProdRelease
# Uses: https://api.tannouspos.com/api/v1.0
```

### Dependencies

The app uses the following key libraries:
- **AndroidX**: Core KTX, Lifecycle, Navigation
- **Compose**: UI, Material 3, Navigation
- **Hilt**: Dependency injection
- **Room**: Database and data persistence
- **WorkManager**: Background processing
- **Retrofit**: Network communication
- **OkHttp**: HTTP client with interceptors
- **Timber**: Logging
- **Coroutines**: Asynchronous programming

## 🧪 Testing

### Unit Tests
```bash
./gradlew test
```

### Instrumented Tests
```bash
./gradlew connectedAndroidTest
```

### Manual QA Checklist

#### 1. Initial Setup
- [ ] App installs successfully
- [ ] Login screen appears
- [ ] Backend connection established

#### 2. Authentication
- [ ] Login with valid credentials
- [ ] JWT token stored securely
- [ ] App navigates to Sell screen after login

#### 3. Shift Management
- [ ] Open shift dialog appears if no active shift
- [ ] Enter opening balance (e.g., 100.00)
- [ ] Shift opens successfully
- [ ] Sell screen becomes accessible

#### 4. Sales Flow
- [ ] Categories display correctly
- [ ] Menu items load by category
- [ ] Add items to cart
- [ ] Cart totals calculate correctly
- [ ] Finalize order button appears

#### 5. Order Finalization
- [ ] Click "Finalize Order"
- [ ] Cash payment dialog appears
- [ ] Enter cash amount (e.g., exact amount)
- [ ] Change calculation works
- [ ] Order finalizes successfully
- [ ] Receipt preview appears

#### 6. Receipt Preview
- [ ] Receipt displays in monospaced font
- [ ] Copy button works
- [ ] Share button works
- [ ] Print button available

#### 7. Offline Functionality
- [ ] Enable airplane mode
- [ ] Create 2-3 orders
- [ ] Orders save locally
- [ ] Disable airplane mode
- [ ] Sync processes automatically
- [ ] Orders appear on server

#### 8. Conflict Resolution
- [ ] Edit customer on backend
- [ ] Edit same customer in app
- [ ] Attempt to save customer
- [ ] Conflict dialog appears
- [ ] Choose "Keep Server" or "Keep Mine"
- [ ] Resolution works correctly

#### 9. Shift Operations
- [ ] Cash drop functionality
- [ ] Expected cash updates
- [ ] Close shift dialog
- [ ] Variance calculation
- [ ] Shift closes successfully

#### 10. Sync Verification
- [ ] Manual sync trigger works
- [ ] Pull sync updates local data
- [ ] Push sync sends local changes
- [ ] Network errors handled gracefully
- [ ] Retry logic works for failed operations

## 🔧 Configuration

### Build Configuration
```kotlin
// app/build.gradle.kts
android {
    compileSdk = 34
    defaultConfig {
        minSdk = 26
        targetSdk = 34
        versionCode = 1
        versionName = "1.0"
    }
    
    buildTypes {
        release {
            isMinifyEnabled = true
            proguardFiles(...)
        }
    }
    
    flavorDimensions += "environment"
    productFlavors {
        create("dev") {
            buildConfigField("String", "BASE_URL", "\"http://10.0.2.2:7000/api/v1.0\"")
        }
        create("prod") {
            buildConfigField("String", "BASE_URL", "\"https://api.tannouspos.com/api/v1.0\"")
        }
    }
}
```

### Network Configuration
```kotlin
// core/di/NetworkModule.kt
@Provides
@Singleton
fun provideOkHttpClient(
    authInterceptor: AuthInterceptor,
    deviceIdInterceptor: DeviceIdInterceptor,
    idempotencyKeyInterceptor: IdempotencyKeyInterceptor,
    etagInterceptor: EtagInterceptor,
    retryAfterInterceptor: RetryAfterInterceptor
): OkHttpClient {
    return OkHttpClient.Builder()
        .addInterceptor(authInterceptor)
        .addInterceptor(deviceIdInterceptor)
        .addInterceptor(idempotencyKeyInterceptor)
        .addInterceptor(etagInterceptor)
        .addInterceptor(retryAfterInterceptor)
        .connectTimeout(30, TimeUnit.SECONDS)
        .readTimeout(30, TimeUnit.SECONDS)
        .writeTimeout(30, TimeUnit.SECONDS)
        .build()
}
```

### Sync Configuration
```kotlin
// core/sync/SyncManager.kt
fun schedulePeriodicSync() {
    // Pull sync every 15 minutes
    val pullWorkRequest = PeriodicWorkRequestBuilder<PullWorker>(
        repeatInterval = 15,
        repeatIntervalTimeUnit = TimeUnit.MINUTES
    )
    
    // Push sync every 5 minutes
    val pushWorkRequest = PeriodicWorkRequestBuilder<PushWorker>(
        repeatInterval = 5,
        repeatIntervalTimeUnit = TimeUnit.MINUTES
    )
}
```

## 🚨 Troubleshooting

### Common Issues

#### 1. Build Errors
```bash
# Clean and rebuild
./gradlew clean
./gradlew build
```

#### 2. Network Connection Issues
- Verify backend is running on port 7000
- Check emulator network configuration
- Use `adb reverse tcp:7000 tcp:7000` for physical devices

#### 3. Sync Issues
- Check network connectivity
- Verify WorkManager constraints
- Review outbox operation status in database

#### 4. Database Issues
- Clear app data if schema changes
- Check Room migration logs
- Verify entity relationships

### Debug Information
```bash
# View logs
adb logcat | grep "TannousPOS"

# Check WorkManager status
adb shell dumpsys jobscheduler | grep "TannousPOS"

# Database inspection
adb shell run-as com.tannous.pos
cd databases
sqlite3 tannous_pos_database
.tables
SELECT * FROM outbox_operations;
```

## 📱 Release

### Production Build
```bash
# Build signed AAB
./gradlew bundleProdRelease

# Build signed APK
./gradlew assembleProdRelease
```

### Signing Configuration
```kotlin
// app/build.gradle.kts
android {
    signingConfigs {
        create("release") {
            storeFile = file(System.getenv("KEYSTORE_PATH") ?: "keystore.jks")
            storePassword = System.getenv("KEYSTORE_PASSWORD")
            keyAlias = System.getenv("KEY_ALIAS")
            keyPassword = System.getenv("KEY_PASSWORD")
        }
    }
    
    buildTypes {
        release {
            signingConfig = signingConfigs.getByName("release")
            isMinifyEnabled = true
            proguardFiles(...)
        }
    }
}
```

### Proguard Rules
```proguard
# Keep Retrofit and serialization models
-keep class com.tannous.pos.core.data.remote.** { *; }
-keep class com.tannous.pos.core.data.model.** { *; }

# Keep Room entities
-keep class com.tannous.pos.core.data.local.entity.** { *; }

# Keep Hilt
-keep class dagger.hilt.** { *; }
-keep class * extends dagger.hilt.android.internal.managers.ViewComponentManager { *; }
```

## 📊 Performance

### Optimization Features
- **Lazy Loading**: Categories and items load on demand
- **Efficient Caching**: ETag-based caching for master data
- **Background Sync**: Non-blocking data synchronization
- **Memory Management**: Proper lifecycle management in ViewModels

### Monitoring
- **Timber Logging**: Comprehensive logging for debugging
- **Performance Metrics**: Sync timing and success rates
- **Error Tracking**: Detailed error reporting for issues

## 🔒 Security

### Authentication
- JWT tokens stored securely in DataStore
- Automatic token refresh handling
- Secure logout with token cleanup

### Data Protection
- Local database encryption (if enabled)
- Secure network communication (HTTPS in production)
- Device ID validation

## 📞 Support

For technical support or feature requests:
- Create an issue in the repository
- Contact the development team
- Review the troubleshooting section above

---

**Tannous POS Android App** - Built with ❤️ using modern Android development practices
