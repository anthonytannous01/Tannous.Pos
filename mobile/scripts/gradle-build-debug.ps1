# gradle-build-debug.ps1
# Script to build Android debug APK with safe flags to avoid lock issues
# Usage: .\scripts\gradle-build-debug.ps1

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Gradle Debug Build (Safe Mode)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get the project root (mobile directory)
$ProjectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $ProjectRoot

# Check if gradlew.bat exists
if (-not (Test-Path "gradlew.bat")) {
    Write-Host "ERROR: gradlew.bat not found in $ProjectRoot" -ForegroundColor Red
    Write-Host "Make sure you're running this from the mobile directory" -ForegroundColor Yellow
    exit 1
}

Write-Host "Project root: $ProjectRoot" -ForegroundColor Gray
Write-Host ""

# Optional: Check for locks first
Write-Host "Checking for existing Gradle processes..." -ForegroundColor Yellow
$javaProcesses = Get-Process -Name "java" -ErrorAction SilentlyContinue
$gradleProcesses = @()
if ($javaProcesses) {
    foreach ($proc in $javaProcesses) {
        try {
            $cmdLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $($proc.Id)" -ErrorAction SilentlyContinue).CommandLine
            if ($cmdLine -and ($cmdLine -like "*gradle*" -or $cmdLine -like "*GradleDaemon*")) {
                $gradleProcesses += $proc
            }
        } catch {
            # Ignore WMI errors
        }
    }
}

if ($gradleProcesses.Count -gt 0) {
    Write-Host "   ⚠ Warning: Found $($gradleProcesses.Count) Gradle process(es) running" -ForegroundColor Yellow
    Write-Host "   Consider running .\scripts\gradle-unlock.ps1 first if you encounter lock issues" -ForegroundColor Yellow
    Write-Host ""
}

Write-Host "Starting build with safe flags:" -ForegroundColor Yellow
Write-Host "  --no-daemon        (prevents daemon lock issues)" -ForegroundColor Gray
Write-Host "  --no-parallel      (prevents parallel execution conflicts)" -ForegroundColor Gray
Write-Host "  --max-workers=1    (single worker to avoid contention)" -ForegroundColor Gray
Write-Host ""

# Build command with safe flags
$buildCommand = ".\gradlew.bat assembleDebug --no-daemon --no-parallel --max-workers=1"

Write-Host "Executing: $buildCommand" -ForegroundColor Cyan
Write-Host ""

try {
    # Execute the build
    & .\gradlew.bat assembleDebug --no-daemon --no-parallel --max-workers=1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "Build completed successfully!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "APK location:" -ForegroundColor Yellow
        Write-Host "  app\build\outputs\apk\dev\debug\app-dev-debug.apk" -ForegroundColor White
        Write-Host ""
    } else {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Red
        Write-Host "Build failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        Write-Host "========================================" -ForegroundColor Red
        Write-Host ""
        Write-Host "If you see lock timeout errors:" -ForegroundColor Yellow
        Write-Host "  1. Run: .\scripts\gradle-unlock.ps1" -ForegroundColor White
        Write-Host "  2. Close Android Studio" -ForegroundColor White
        Write-Host "  3. Try building again" -ForegroundColor White
        Write-Host ""
        exit $LASTEXITCODE
    }
} catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Build error occurred!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting steps:" -ForegroundColor Yellow
    Write-Host "  1. Run: .\scripts\gradle-unlock.ps1" -ForegroundColor White
    Write-Host "  2. Close Android Studio completely" -ForegroundColor White
    Write-Host "  3. Check Java installation: java -version" -ForegroundColor White
    Write-Host "  4. Verify ANDROID_HOME is set correctly" -ForegroundColor White
    Write-Host ""
    exit 1
}

