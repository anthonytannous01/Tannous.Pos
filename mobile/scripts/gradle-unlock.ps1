# gradle-unlock.ps1
# Script to release Gradle locks on Windows
# Usage: .\scripts\gradle-unlock.ps1

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Gradle Lock Release Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get the project root (mobile directory)
$ProjectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $ProjectRoot

Write-Host "[1/6] Stopping all Gradle daemons..." -ForegroundColor Yellow
try {
    & .\gradlew.bat --stop 2>&1 | Out-Null
    Write-Host "   ✓ Gradle daemons stopped" -ForegroundColor Green
} catch {
    Write-Host "   ⚠ Could not stop Gradle daemons (may not be running)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "[2/6] Checking for running Java/Gradle processes..." -ForegroundColor Yellow
$javaProcesses = Get-Process -Name "java" -ErrorAction SilentlyContinue

if ($javaProcesses) {
    # Try to identify Gradle processes by checking command line via WMI
    $gradleProcesses = @()
    foreach ($proc in $javaProcesses) {
        try {
            $cmdLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $($proc.Id)" -ErrorAction SilentlyContinue).CommandLine
            if ($cmdLine -and ($cmdLine -like "*gradle*" -or $cmdLine -like "*GradleDaemon*")) {
                $gradleProcesses += $proc
            }
        } catch {
            # If WMI fails, check all Java processes (less precise but safer)
            $gradleProcesses += $proc
        }
    }
    
    if ($gradleProcesses.Count -gt 0) {
        Write-Host "   Found $($gradleProcesses.Count) potential Gradle-related Java process(es):" -ForegroundColor Yellow
        foreach ($proc in $gradleProcesses) {
            Write-Host "   - PID $($proc.Id): $($proc.ProcessName) (Memory: $([math]::Round($proc.WorkingSet64/1MB, 2)) MB)" -ForegroundColor Yellow
        }
        
        $kill = Read-Host "   Kill these processes? (Y/N)"
        if ($kill -eq "Y" -or $kill -eq "y") {
            foreach ($proc in $gradleProcesses) {
                try {
                    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
                    Write-Host "   ✓ Killed PID $($proc.Id)" -ForegroundColor Green
                } catch {
                    Write-Host "   ✗ Failed to kill PID $($proc.Id)" -ForegroundColor Red
                }
            }
        }
    } else {
        Write-Host "   ✓ No Gradle-related Java processes found" -ForegroundColor Green
    }
} else {
    Write-Host "   ✓ No Java processes found" -ForegroundColor Green
}

Write-Host ""
Write-Host "[3/6] Checking for Android Studio processes..." -ForegroundColor Yellow
$studioProcesses = Get-Process -Name "studio64","studio","idea64","idea" -ErrorAction SilentlyContinue
if ($studioProcesses) {
    Write-Host "   ⚠ Android Studio/IntelliJ is running. Close it to release locks." -ForegroundColor Yellow
    Write-Host "   Found processes: $($studioProcesses.Name -join ', ')" -ForegroundColor Yellow
} else {
    Write-Host "   ✓ No Android Studio processes found" -ForegroundColor Green
}

Write-Host ""
Write-Host "[4/6] Removing project .gradle folder..." -ForegroundColor Yellow
$projectGradleDir = Join-Path $ProjectRoot ".gradle"
if (Test-Path $projectGradleDir) {
    try {
        Remove-Item -Path $projectGradleDir -Recurse -Force -ErrorAction Stop
        Write-Host "   ✓ Removed $projectGradleDir" -ForegroundColor Green
    } catch {
        Write-Host "   ✗ Failed to remove .gradle folder (may be locked)" -ForegroundColor Red
        Write-Host "   Try closing Android Studio and running this script again" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ✓ No .gradle folder found (already clean)" -ForegroundColor Green
}

Write-Host ""
Write-Host "[5/6] Checking global Gradle cache..." -ForegroundColor Yellow
$userProfile = $env:USERPROFILE
$globalGradleDir = Join-Path $userProfile ".gradle"
$daemonDir = Join-Path $globalGradleDir "daemon"
$cachesDir = Join-Path $globalGradleDir "caches"

if (Test-Path $daemonDir) {
    Write-Host "   Found global Gradle daemon directory: $daemonDir" -ForegroundColor Yellow
    $clearGlobal = Read-Host "   Clear global Gradle daemon/caches? (Y/N) - WARNING: This will clear ALL Gradle caches"
    if ($clearGlobal -eq "Y" -or $clearGlobal -eq "y") {
        try {
            if (Test-Path $daemonDir) {
                Remove-Item -Path $daemonDir -Recurse -Force -ErrorAction SilentlyContinue
                Write-Host "   ✓ Cleared Gradle daemon directory" -ForegroundColor Green
            }
            if (Test-Path $cachesDir) {
                Remove-Item -Path $cachesDir -Recurse -Force -ErrorAction SilentlyContinue
                Write-Host "   ✓ Cleared Gradle caches directory" -ForegroundColor Green
            }
        } catch {
            Write-Host "   ✗ Failed to clear global cache (may be locked)" -ForegroundColor Red
        }
    } else {
        Write-Host "   ⏭ Skipped global cache cleanup" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ✓ No global Gradle daemon directory found" -ForegroundColor Green
}

Write-Host ""
Write-Host "[6/6] Verifying lock release..." -ForegroundColor Yellow
Start-Sleep -Seconds 2

# Check if any Java processes are still holding locks
$remainingJava = Get-Process -Name "java" -ErrorAction SilentlyContinue
$gradleStillRunning = $false
if ($remainingJava) {
    foreach ($proc in $remainingJava) {
        try {
            $cmdLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $($proc.Id)" -ErrorAction SilentlyContinue).CommandLine
            if ($cmdLine -and ($cmdLine -like "*gradle*")) {
                $gradleStillRunning = $true
                break
            }
        } catch {
            # Ignore WMI errors
        }
    }
}

if ($gradleStillRunning) {
    Write-Host "   ⚠ Warning: Some Gradle processes may still be running" -ForegroundColor Yellow
} else {
    Write-Host "   ✓ No Gradle processes detected" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Lock release complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Close Android Studio if it's still open" -ForegroundColor White
Write-Host "  2. Run: .\scripts\gradle-build-debug.ps1" -ForegroundColor White
Write-Host "     OR manually: .\gradlew.bat assembleDebug --no-daemon --no-parallel --max-workers=1" -ForegroundColor White
Write-Host ""

