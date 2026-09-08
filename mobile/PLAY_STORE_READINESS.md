# Play Store Readiness Checklist - Tannous POS

> **Current plan: direct APK install, not the Play Store.** Decided 2026-09-05. The app runs on
> tablets in one restaurant that we own, so the store buys nothing and costs a privacy policy, a
> data safety declaration, a store listing, screenshots, a content rating and a review cycle on
> every update. Builds are signed with the release keystore and installed by hand. See **Direct
> APK Install** below for that route.
>
> The Play developer account is already paid for (a one-time 25 USD registration, not a
> subscription) and does not lapse, so nothing is lost by leaving it unused. Everything else in
> this document stays valid as the route to take when the POS is sold to a second restaurant.
>
> Two facts that will matter then. Internal testing is exempt from the Data safety section, so it
> is the cheap way to reach a handful of restaurants; closed, open and production tracks all
> require the form plus a published privacy policy. And Google now requires developer verification
> for sideloaded apps too - enforcement began 30 September 2026 in Brazil, Indonesia, Singapore and
> Thailand, with global expansion planned from 2027. A Play developer account satisfies that
> requirement, which is the other reason to keep the account rather than close it.

## ✅ App Store Listing Requirements

### App Icon
- [x] **App Icon**: 512x512 PNG (created: `ic_launcher.xml`)
- [x] **Adaptive Icon**: Android 8.0+ support
- [x] **Icon Background**: Brand color (#1976D2)

### App Information
- [x] **App Name**: "Tannous POS"
- [x] **Short Description**: "Point of Sale system for restaurants and retail"
- [x] **Full Description**: "Professional POS system with offline-first design, shift management, and real-time sync"
- [x] **Category**: Business
- [x] **Content Rating**: Everyone (no violence, adult content, etc.)

### Screenshots & Graphics
- [ ] **Phone Screenshots** (1080x1920 minimum):
  - [ ] Login Screen
  - [ ] Main Dashboard
  - [ ] Sell Screen with items
  - [ ] Receipt Preview
  - [ ] Shift Management
  - [ ] Settings
- [ ] **Tablet Screenshots** (if targeting tablets)
- [ ] **Feature Graphic**: 1024x500 PNG
- [ ] **Promo Graphic**: 180x120 PNG

## ✅ Technical Requirements

### Build Configuration
- [x] **Target SDK**: 34 (Android 14)
- [x] **Min SDK**: 26 (Android 8.0)
- [x] **Version Code**: generated from commit count by `versioning.gradle.kts`
- [x] **Version Name**: generated from the latest git tag
- [x] **Package Name**: `com.tannous.pos`

### Signing & Security
- [x] **Release Signing**: Configured with Proguard/R8
- [x] **Proguard Rules**: Comprehensive rules for all dependencies
- [x] **Code Obfuscation**: Enabled for release builds
- [x] **APK/AAB**: Both formats supported

### Permissions
- [x] **Internet**: Required for sync
- [x] **Bluetooth**: For printer connectivity
- [x] **Location**: Not required
- [x] **Camera**: Not required
- [x] **Storage**: Not required

## ✅ Content Rating

### Content Descriptors
- [x] **Violence**: None
- [x] **Sex**: None
- [x] **Language**: None
- [x] **Controlled Substances**: None
- [x] **User Generated Content**: None

### Interactive Elements
- [x] **Digital Purchases**: None
- [x] **User Communication**: None
- [x] **Location Sharing**: None

## ✅ Privacy & Legal

### Privacy Policy
- [ ] **Privacy Policy URL**: Required
- [ ] **Data Collection**: Document what data is collected. Staff credentials, and customer
      name / email / phone / address / notes. **No health data:** the customer allergies field
      was removed in Step 129, so "Health info" does not apply and no GDPR special category is
      involved. Notes is free text, so the policy should say it is staff-entered and not
      intended for sensitive details. No card numbers anywhere: `PaymentMethod` is a string.
- [ ] **Third-party Services**: none. No analytics or crash-reporting SDK ships in the app.
- [ ] **Data Usage**: How data is used and stored

### Terms of Service
- [ ] **Terms URL**: Recommended
- [ ] **User Agreement**: App usage terms

### GDPR Compliance
- [ ] **Data Processing**: Document data processing activities
- [ ] **User Rights**: Right to access, delete, export data
- [ ] **Consent**: User consent for data collection

## ✅ Store Listing Content

### App Description
```
Tannous POS - Professional Point of Sale System

Transform your business with our powerful, offline-first POS solution designed for restaurants, cafes, and retail stores.

✨ KEY FEATURES:
• Offline-First Design - Works without internet
• Real-Time Sync - Automatic data synchronization
• Shift Management - Complete cash register control
• Receipt Printing - Bluetooth & LAN printer support
• Customer Management - Track customer preferences
• Inventory Tracking - Real-time stock management
• Multi-Device Support - Use on phones and tablets
• Secure Authentication - JWT-based security

🚀 PERFECT FOR:
• Restaurants & Cafes
• Retail Stores
• Food Trucks
• Small Businesses
• Multi-location Operations

💡 WHY CHOOSE TANNOUS POS?
• No monthly fees
• Works offline
• Easy to use
• Professional features
• Reliable sync
• Secure data

Download now and streamline your business operations!
```

### Keywords
```
pos,point of sale,restaurant pos,retail pos,cash register,inventory management,shift management,receipt printer,offline pos,business management,restaurant management,retail management,pos system,point of sale system
```

## ✅ Testing & Quality

### Pre-Launch Testing
- [ ] **Google Play Console Pre-launch Report**: Run automated tests
- [ ] **Device Testing**: Test on multiple Android versions
- [ ] **Screen Size Testing**: Various phone and tablet sizes
- [ ] **Performance Testing**: Memory usage, battery consumption

### Quality Assurance
- [ ] **Crash Testing**: Verify crash reporting works
- [ ] **Offline Testing**: Test offline functionality
- [ ] **Sync Testing**: Verify data synchronization
- [ ] **Printer Testing**: Test receipt printing
- [ ] **UI/UX Testing**: Verify all screens work correctly

## ✅ Launch Preparation

### Store Listing
- [ ] **App Title**: "Tannous POS"
- [ ] **Short Description**: "Professional POS system for restaurants and retail"
- [ ] **Full Description**: Complete description with features
- [ ] **Screenshots**: High-quality screenshots of all major features
- [ ] **Feature Graphic**: Eye-catching promotional image

### Pricing & Distribution
- [ ] **Pricing**: Free (with in-app purchases if applicable)
- [ ] **Distribution**: Available in all countries
- [ ] **Release Type**: Production release
- [ ] **Release Track**: Production track

### Marketing
- [ ] **App Store Optimization**: Optimize for relevant keywords
- [ ] **Social Media**: Prepare social media announcements
- [ ] **Press Release**: If applicable
- [ ] **Website**: Update website with app information

## ✅ Post-Launch

### Monitoring
- [ ] **Crash Reports**: no remote reporting. Pull `pos-*.log` off the tablet (see Diagnostic Logs).
- [ ] **Analytics**: none shipped. Reports come from the POS's own data, not an SDK.
- [ ] **Reviews**: Monitor user reviews and ratings
- [ ] **Performance**: Monitor app performance metrics

### Updates
- [ ] **Bug Fixes**: Address reported issues
- [ ] **Feature Updates**: Plan future enhancements
- [ ] **Version Updates**: Regular app updates
- [ ] **User Feedback**: Respond to user feedback

## 🔧 Technical Setup Commands

### Build Commands
```bash
# Development builds
./gradlew assembleDevDebug
./gradlew assembleDevRelease

# Staging builds
./gradlew assembleStagingDebug
./gradlew assembleStagingRelease

# Production builds
./gradlew assembleProdDebug
./gradlew assembleProdRelease

# Bundle for Play Store
./gradlew bundleProdRelease
```

### Direct APK Install (the current route)

```bash
cd mobile
./gradlew.bat assembleProdRelease
```

The APK lands in `app/build/outputs/apk/prod/release/`. Copy it to the tablet and open it; Android
asks once for permission to install from that source. `adb install -r <apk>` does the same over
USB.

- **Install release builds, not debug.** Debug plants Timber's `DebugTree` and writes no log file,
  so a tablet that misbehaves during service leaves nothing to read. See Diagnostic Logs below.
- **Install over the existing app, do not uninstall first.** Uninstalling drops the Room database,
  which means any queued outbox operations are lost and the Room migrations never run - so a
  migration bug stays hidden until it hits a tablet that did upgrade in place.
- **`versionCode` must increase** or Android refuses the update. It is generated from the commit
  count by `versioning.gradle.kts`, so this takes care of itself as long as builds come from
  committed work.
- The same keystore signs every build. An APK signed with a different key cannot update an
  installed one; Android rejects it and the only way through is uninstall, which loses the
  database.

### Signing Setup

**The release keystore is deliberately not in this repository.** `mobile/keystore/` and every
`*.jks` / `*.keystore` file are gitignored, and the keystore was purged from git history after
being committed by mistake in step-101. A fresh clone will not build a signed release until the
keystore is copied in by hand from its offline backup.

**There is exactly one release keystore:** `mobile/keystore/tannous-pos-release.jks`, alias
`tannous-pos-key`, referenced from `local.properties` as `RELEASE_STORE_FILE`. Nothing else signs a
release build. Verify a backup is the same file by comparing SHA-256:

```powershell
Get-FileHash mobile\keystore\tannous-pos-release.jks -Algorithm SHA256
```

Losing it means losing the ability to update the app - on Play Store and on a sideloaded install
alike, since Android will not replace an app with one signed by a different key. There is no
recovery. Keep at least one backup outside this machine (password manager or encrypted storage),
and never place it inside the working tree of a repository. It currently sits under
`mobile/keystore/`, which is inside the tree: gitignored, but a `git clean -xdf` would delete it.

`app/build.gradle.kts` also declares a second signing config, `ciRelease`, pointing at
`ci/keystore.jks` with passwords from environment variables. That file does not exist and the CI
pipeline is not active (see `CI_CD_SUMMARY.md`). If CI is ever revived, point `ciRelease` at this
same keystore rather than generating a new one - a second key would produce APKs that cannot update
the installed app.

1. Create keystore (first time only): `keytool -genkey -v -keystore tannous-pos.keystore -alias tannous-pos -keyalg RSA -keysize 2048 -validity 10000`
2. Place it at `mobile/keystore/` (gitignored) or anywhere outside the repo.
3. Add to `local.properties` (also gitignored — never commit it):
   ```
   RELEASE_STORE_FILE=path/to/tannous-pos.keystore
   RELEASE_STORE_PASSWORD=your_password
   RELEASE_KEY_ALIAS=tannous-pos
   RELEASE_KEY_PASSWORD=your_password
   ```

### Diagnostic Logs

There is no crash reporting service. Release builds write warnings and errors to a file on the
tablet instead, via `FileLogTree`. Crashlytics was considered and dropped: it would mean a Firebase
project, a `google-services.json` that must never be committed, and a privacy disclosure covering
uploaded breadcrumbs - for one restaurant whose tablet is within arm's reach.

- Location: app-scoped external storage, `Android/data/com.tannous.pos/files/logs/`.
- One file per day, `pos-YYYY-MM-DD.log`; 7-day retention; capped at 2 MB per file so a log storm
  cannot fill the tablet during service.
- WARN and above only. Debug builds keep Timber's `DebugTree` and write nothing to disk.
- Pull them over USB (`adb pull /sdcard/Android/data/com.tannous.pos/files/logs`) or through the
  device's own file manager. No root needed. Uninstalling the app removes them.

Naming, rotation and retention live in `LogFileWriter`, which is plain JVM code covered by
`LogFileWriterTest`. That matters because `FileLogTree` swallows every exception on purpose - a
logger writing nothing would otherwise look exactly like a logger with nothing to write.

**Do not log customer data.** These files sit on a tablet in a restaurant and can be read by anyone
with physical access. The app's log statements use identifiers - order id, device id - rather than
names, phone numbers or the free-text notes on a customer record. Keep it that way.

If crash reporting is ever wanted (selling this to another restaurant would be the reason), it is
an additive change: add the SDK and plant a second tree beside this one.

## 📱 App Store Assets Checklist

### Required Assets
- [ ] App Icon (512x512)
- [ ] Feature Graphic (1024x500)
- [ ] Phone Screenshots (minimum 2)
- [ ] App Description
- [ ] Privacy Policy URL

### Optional Assets
- [ ] Tablet Screenshots
- [ ] Promo Graphic (180x120)
- [ ] Video Preview
- [ ] App Category
- [ ] Content Rating

## 🚀 Launch Day Checklist

### Pre-Launch (24 hours before)
- [ ] Final app testing
- [ ] Screenshots and graphics ready
- [ ] Description and keywords finalized
- [ ] Privacy policy published
- [ ] Team notifications sent

### Launch Day
- [ ] Monitor app store listing
- [ ] Check for any issues
- [ ] Monitor crash reports
- [ ] Respond to initial feedback
- [ ] Social media announcements

### Post-Launch (First week)
- [ ] Monitor user feedback
- [ ] Address critical issues
- [ ] Track download numbers
- [ ] Monitor app performance
- [ ] Plan first update

---

**Status**: 🟡 In Progress (80% Complete)
**Next Steps**: Create screenshots, finalize privacy policy, complete pre-launch testing
