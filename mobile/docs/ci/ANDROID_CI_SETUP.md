# Android CI/CD Setup Guide - Tannous POS

This guide covers setting up the complete CI/CD pipeline for the Tannous POS Android app, including Google Play Console publishing and Firebase App Distribution.

## 🔐 Required Secrets

### 1. Google Play Console Service Account

#### Create Service Account
1. Go to [Google Play Console](https://play.google.com/console)
2. Navigate to **Setup** → **API access**
3. Click **Create new service account**
4. Fill in:
   - **Service account name**: `tannous-pos-ci`
   - **Service account ID**: `tannous-pos-ci@project-id.iam.gserviceaccount.com`
   - **Description**: `CI/CD service account for Android app publishing`

#### Grant Permissions
1. Click on the created service account
2. Click **Grant access**
3. Add the following roles:
   - **App Bundle Creator** (for uploading AABs)
   - **Release Manager** (for managing releases)
   - **Play Console Viewer** (for reading app info)
4. Click **Invite user**

#### Download JSON Key
1. In the service account details, click **Keys** tab
2. Click **Add key** → **Create new key**
3. Choose **JSON** format
4. Download the file and store securely

#### Store as GitHub Secret
- **Secret Name**: `PLAY_SERVICE_ACCOUNT_JSON`
- **Value**: Copy the entire content of the downloaded JSON file

### 2. Firebase Service Account

#### Create Service Account
1. Go to [Firebase Console](https://console.firebase.google.com)
2. Select your project
3. Go to **Project Settings** → **Service accounts**
4. Click **Generate new private key**
5. Choose **App Distribution Admin** role
6. Download the JSON file

#### Store as GitHub Secret
- **Secret Name**: `FIREBASE_SERVICE_ACCOUNT_JSON`
- **Value**: Copy the entire content of the downloaded JSON file

### 3. Android Keystore

#### Create Keystore
```bash
keytool -genkey -v \
  -keystore tannous-pos.keystore \
  -alias tannous-pos \
  -keyalg RSA \
  -keysize 2048 \
  -validity 10000 \
  -storepass your_keystore_password \
  -keypass your_key_password
```

#### Convert to Base64
```bash
# On macOS/Linux
base64 -i tannous-pos.keystore

# On Windows PowerShell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("tannous-pos.keystore"))
```

#### Store as GitHub Secrets
- **Secret Name**: `ANDROID_KEYSTORE_BASE64`
- **Value**: Base64 encoded keystore file

- **Secret Name**: `ANDROID_KEYSTORE_PASSWORD`
- **Value**: Your keystore password

- **Secret Name**: `ANDROID_KEY_ALIAS`
- **Value**: `tannous-pos`

- **Secret Name**: `ANDROID_KEY_PASSWORD`
- **Value**: Your key password

### 4. Testers Configuration

#### Store as GitHub Secret
- **Secret Name**: `QA_TESTERS_EMAILS`
- **Value**: Comma-separated list of tester emails
- **Example**: `tester1@company.com,tester2@company.com,qa@company.com`

## 📱 App Configuration

### App IDs by Flavor
| Flavor | Package Name | Purpose |
|--------|--------------|---------|
| **dev** | `com.tannous.pos.dev` | Development builds |
| **staging** | `com.tannous.pos.staging` | QA testing |
| **prod** | `com.tannous.pos` | Production release |

### Play Console Tracks
| Track | Purpose | Access |
|-------|---------|--------|
| **internal** | Internal testing | Development team |
| **beta** | Closed testing | QA team + stakeholders |
| **production** | Public release | All users (future) |

## 🚀 CI/CD Workflow

### Trigger Conditions
- **Build & Test**: All pushes to `main` and `release/*` branches
- **QA Distribution**: Only pushes to `main` branch
- **Play Publishing**: Only pushes to `release/*` branches

### Workflow Steps
1. **Build & Test**: Compile, test, and create AAB
2. **QA Distribution**: Upload staging AAB to Firebase App Distribution
3. **Play Publishing**: Upload production AAB to Google Play Console

## 🔧 Local Development

### Prerequisites
- JDK 17
- Android SDK
- Gradle 8.1+

### Build Commands
```bash
# Development builds
./gradlew assembleDevDebug
./gradlew assembleDevRelease

# Staging builds
./gradlew bundleStagingRelease

# Production builds
./gradlew bundleProdRelease

# Distribution
./gradlew appDistributionUploadStagingRelease

# Publishing
./gradlew publishProdRelease -PplayTrack=internal
```

### Makefile Shortcuts
```bash
make dev-build          # Development debug build
make staging-aab        # Staging release AAB
make prod-aab           # Production release AAB
make distribute-staging # Upload to Firebase App Distribution
make publish-internal   # Publish to Play Console internal track
```

## 📋 Setup Checklist

### Google Play Console
- [ ] Service account created with proper permissions
- [ ] Service account JSON downloaded and stored as secret
- [ ] App created in Play Console
- [ ] Internal testing track configured

### Firebase Console
- [ ] Project created and configured
- [ ] App Distribution enabled
- [ ] Service account created with App Distribution Admin role
- [ ] Service account JSON downloaded and stored as secret
- [ ] Test groups configured

### GitHub Repository
- [ ] All required secrets added to repository
- [ ] CI workflow file committed
- [ ] Keystore file created and encoded
- [ ] Versioning automation configured

### Local Environment
- [ ] Keystore file created
- [ ] Gradle properties configured
- [ ] Local build verification completed

## 🚨 Troubleshooting

### Common Issues

#### Build Failures
- Verify JDK 17 is installed and set as JAVA_HOME
- Check that all required secrets are properly set
- Ensure keystore file is valid and accessible

#### Publishing Failures
- Verify Play Console service account has proper permissions
- Check that app ID matches between build and Play Console
- Ensure AAB is properly signed

#### Distribution Failures
- Verify Firebase service account has App Distribution Admin role
- Check that test groups are properly configured
- Ensure release notes file is accessible

### Debug Commands
```bash
# Check Gradle configuration
./gradlew properties

# Verify signing configuration
./gradlew signingReport

# Test Play Publisher configuration
./gradlew publishProdRelease --dry-run

# Test Firebase App Distribution
./gradlew appDistributionUploadStagingRelease --dry-run
```

## 📚 Additional Resources

- [Google Play Console API Documentation](https://developers.google.com/android-publisher)
- [Firebase App Distribution Documentation](https://firebase.google.com/docs/app-distribution)
- [Gradle Play Publisher Plugin](https://github.com/Triple-T/gradle-play-publisher)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)

## 🔄 Maintenance

### Regular Tasks
- Rotate service account keys annually
- Update test group members as needed
- Monitor CI/CD pipeline performance
- Review and update release notes format

### Security Considerations
- Never commit keystore files or service account JSONs
- Use repository secrets for all sensitive data
- Regularly audit service account permissions
- Monitor for unauthorized access attempts

---

**Last Updated**: $(date)
**Version**: 1.0
**Maintainer**: Development Team
