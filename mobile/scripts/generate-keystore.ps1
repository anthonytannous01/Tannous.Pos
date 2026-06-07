# ============================================================
# Tannous POS — Release Keystore Generator
# Run this ONCE to generate the signing keystore.
# Store the keystore and passwords somewhere safe (NOT in git).
# ============================================================

param(
    [string]$KeystoreDir   = "$PSScriptRoot\..\keystore",
    [string]$KeystoreFile  = "tannous-pos-release.jks",
    [string]$StorePassword = "",   # leave blank to be prompted
    [string]$KeyAlias      = "tannous-pos-key",
    [string]$KeyPassword   = ""    # leave blank to be prompted
)

# Prompt for passwords if not provided
if (-not $StorePassword) {
    $StorePassword = Read-Host "Enter keystore password (min 6 chars)" -AsSecureString |
        ForEach-Object { [Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [Runtime.InteropServices.Marshal]::SecureStringToBSTR($_)) }
}
if (-not $KeyPassword) {
    $KeyPassword = Read-Host "Enter key password (min 6 chars)" -AsSecureString |
        ForEach-Object { [Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [Runtime.InteropServices.Marshal]::SecureStringToBSTR($_)) }
}

# Create keystore directory
New-Item -ItemType Directory -Force -Path $KeystoreDir | Out-Null
$keystorePath = Join-Path $KeystoreDir $KeystoreFile

# Find keytool
$keytool = Get-Command keytool -ErrorAction SilentlyContinue
if (-not $keytool) {
    # Try common Java locations
    $javaPaths = @(
        "$env:JAVA_HOME\bin\keytool.exe",
        "C:\Program Files\Java\jdk*\bin\keytool.exe",
        "C:\Program Files\Android\Android Studio\jbr\bin\keytool.exe"
    )
    foreach ($p in $javaPaths) {
        $found = Get-Item $p -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) { $keytool = $found.FullName; break }
    }
}

if (-not $keytool) {
    Write-Error "keytool not found. Install Java JDK or set JAVA_HOME."
    exit 1
}

Write-Host "Generating keystore at: $keystorePath"

& $keytool -genkeypair `
    -v `
    -keystore $keystorePath `
    -alias $KeyAlias `
    -keyalg RSA `
    -keysize 2048 `
    -validity 10000 `
    -storepass $StorePassword `
    -keypass $KeyPassword `
    -dname "CN=Tannous POS, OU=Mobile, O=Tannous, L=Beirut, ST=Beirut, C=LB"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Keystore generation failed."
    exit 1
}

Write-Host ""
Write-Host "=== Keystore generated successfully! ==="
Write-Host "File: $keystorePath"
Write-Host ""
Write-Host "=== Add these to mobile\local.properties (NEVER commit this file) ==="
Write-Host "RELEASE_STORE_FILE=../keystore/$KeystoreFile"
Write-Host "RELEASE_STORE_PASSWORD=$StorePassword"
Write-Host "RELEASE_KEY_ALIAS=$KeyAlias"
Write-Host "RELEASE_KEY_PASSWORD=$KeyPassword"
Write-Host ""
Write-Host "=== Build release AAB ==="
Write-Host "cd mobile"
Write-Host "./gradlew :app:bundleProdRelease"
Write-Host ""
Write-Host "Output: mobile/app/build/outputs/bundle/prodRelease/app-prod-release.aab"
