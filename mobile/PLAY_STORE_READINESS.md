# Play Store Readiness Checklist - Tannous POS

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
- [x] **Min SDK**: 24 (Android 7.0)
- [x] **Version Code**: 1
- [x] **Version Name**: "1.0.0"
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
- [ ] **Data Collection**: Document what data is collected
- [ ] **Third-party Services**: Firebase Analytics, Crashlytics
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
- [ ] **Crash Reports**: Monitor Firebase Crashlytics
- [ ] **Analytics**: Track user engagement with Firebase Analytics
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

### Signing Setup

**The release keystore is deliberately not in this repository.** `mobile/keystore/` and every
`*.jks` / `*.keystore` file are gitignored, and the keystore was purged from git history after
being committed by mistake in step-101. A fresh clone will not build a signed release until the
keystore is copied in by hand from its offline backup.

Losing the keystore means losing the ability to update the app on Play Store — there is no
recovery. Keep at least one backup outside this machine (password manager or encrypted storage),
and never place it inside the working tree of a repository.

1. Create keystore (first time only): `keytool -genkey -v -keystore tannous-pos.keystore -alias tannous-pos -keyalg RSA -keysize 2048 -validity 10000`
2. Place it at `mobile/keystore/` (gitignored) or anywhere outside the repo.
3. Add to `local.properties` (also gitignored — never commit it):
   ```
   RELEASE_STORE_FILE=path/to/tannous-pos.keystore
   RELEASE_STORE_PASSWORD=your_password
   RELEASE_KEY_ALIAS=tannous-pos
   RELEASE_KEY_PASSWORD=your_password
   ```

### Firebase Setup
1. Create Firebase project
2. Add `google-services.json` to `app/` directory
3. Enable Crashlytics and Analytics
4. Test crash reporting

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
