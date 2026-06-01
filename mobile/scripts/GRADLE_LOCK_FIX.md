# Gradle Lock Timeout Fix Guide (Windows)

This guide provides steps to resolve Gradle cache lock / file lock issues on Windows that cause builds to hang or fail.

## Quick Fix (Using Scripts)

### Option 1: Automated Scripts (Recommended)

1. **Release locks:**
   ```powershell
   cd mobile
   .\scripts\gradle-unlock.ps1
   ```

2. **Build with safe flags:**
   ```powershell
   .\scripts\gradle-build-debug.ps1
   ```

---

## Manual Steps

If you prefer to run commands manually or the scripts don't work:

### Step 1: Stop Gradle Daemons

```powershell
cd mobile
.\gradlew.bat --stop
```

**What this does:** Stops all running Gradle daemon processes that may be holding locks.

---

### Step 2: Identify Lock-Holding Processes

Check for Java processes that might be Gradle-related:

```powershell
# List all Java processes
Get-Process -Name "java" -ErrorAction SilentlyContinue | Format-Table Id, ProcessName, @{Label="Memory(MB)";Expression={[math]::Round($_.WorkingSet64/1MB, 2)}}

# Check if any are Gradle-related (requires WMI)
Get-Process -Name "java" | ForEach-Object {
    $cmdLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $($_.Id)").CommandLine
    if ($cmdLine -like "*gradle*") {
        Write-Host "PID $($_.Id): $cmdLine"
    }
}
```

**What to look for:**
- Processes with "gradle" or "GradleDaemon" in the command line
- High memory usage Java processes that don't respond

---

### Step 3: Kill Stuck Processes (if needed)

If you find Gradle-related Java processes:

```powershell
# Kill by PID (replace 12345 with actual PID)
Stop-Process -Id 12345 -Force

# Or kill all Java processes (USE WITH CAUTION - kills ALL Java processes)
Get-Process -Name "java" | Stop-Process -Force
```

**Warning:** Killing all Java processes will also stop Android Studio, IntelliJ IDEA, and other Java applications.

---

### Step 4: Close Android Studio

**IMPORTANT:** Android Studio may hold Gradle locks even when not actively building.

1. Close Android Studio completely
2. Check Task Manager to ensure `studio64.exe` or `idea64.exe` is not running
3. If it's stuck, kill it:
   ```powershell
   Get-Process -Name "studio64","studio","idea64","idea" -ErrorAction SilentlyContinue | Stop-Process -Force
   ```

---

### Step 5: Delete Project .gradle Folder

```powershell
cd mobile
Remove-Item -Path ".gradle" -Recurse -Force -ErrorAction SilentlyContinue
```

**What this does:** Removes the project-level Gradle cache and lock files.

**Location:** `mobile\.gradle\`

---

### Step 6: (Optional) Clear Global Gradle Cache

**Warning:** This clears ALL Gradle caches for ALL projects on your system.

```powershell
# Clear Gradle daemon directory
$userProfile = $env:USERPROFILE
Remove-Item -Path "$userProfile\.gradle\daemon" -Recurse -Force -ErrorAction SilentlyContinue

# Clear Gradle caches
Remove-Item -Path "$userProfile\.gradle\caches" -Recurse -Force -ErrorAction SilentlyContinue
```

**Locations:**
- Daemon: `%USERPROFILE%\.gradle\daemon\`
- Caches: `%USERPROFILE%\.gradle\caches\`

**When to do this:**
- If project-level cleanup didn't work
- If you're experiencing persistent lock issues across multiple projects
- As a last resort before reinstalling Gradle

---

### Step 7: Verify Locks Are Released

Wait a few seconds, then check:

```powershell
# Check for remaining Java processes
Get-Process -Name "java" -ErrorAction SilentlyContinue

# Check if .gradle folder is gone
Test-Path "mobile\.gradle"
```

---

### Step 8: Build with Safe Flags

Build using flags that prevent lock contention:

```powershell
cd mobile
.\gradlew.bat assembleDebug --no-daemon --no-parallel --max-workers=1
```

**Flag explanations:**
- `--no-daemon`: Prevents daemon from running (eliminates daemon lock issues)
- `--no-parallel`: Disables parallel project execution (reduces file contention)
- `--max-workers=1`: Uses only one worker thread (minimizes lock conflicts)

**Trade-off:** Builds will be slower but more reliable when locks are an issue.

---

## Troubleshooting

### Lock Still Exists After Cleanup

1. **Check file handles:**
   ```powershell
   # Requires Handle.exe from Sysinternals, or use Process Explorer
   # Download from: https://docs.microsoft.com/en-us/sysinternals/downloads/handle
   ```

2. **Restart Windows:**
   - Sometimes Windows file handles remain locked until reboot
   - This is the most reliable way to clear all locks

3. **Check antivirus:**
   - Some antivirus software locks files during scanning
   - Add `mobile\.gradle` and `%USERPROFILE%\.gradle` to exclusions

### Build Still Fails

1. **Check Java version:**
   ```powershell
   java -version
   ```
   - Ensure Java 11+ is installed
   - Verify `JAVA_HOME` is set correctly

2. **Check Android SDK:**
   ```powershell
   echo $env:ANDROID_HOME
   ```
   - Verify Android SDK path is correct
   - Check `mobile\local.properties` for SDK path

3. **Check disk space:**
   ```powershell
   Get-PSDrive C | Select-Object Used, Free
   ```
   - Gradle builds require significant disk space
   - Ensure at least 5-10 GB free

### Permission Errors

If you get "Access Denied" errors:

1. **Run PowerShell as Administrator:**
   - Right-click PowerShell → "Run as Administrator"

2. **Check folder permissions:**
   ```powershell
   icacls "mobile\.gradle"
   icacls "$env:USERPROFILE\.gradle"
   ```

---

## Prevention Tips

1. **Always stop daemons before closing Android Studio:**
   ```powershell
   .\gradlew.bat --stop
   ```

2. **Use safe flags for CI/CD:**
   - Add `--no-daemon --no-parallel --max-workers=1` to build scripts

3. **Avoid multiple Gradle builds simultaneously:**
   - Don't run builds in multiple terminals at once
   - Close Android Studio before running command-line builds

4. **Regular cleanup:**
   - Periodically delete `.gradle` folder if experiencing issues
   - Clear global cache monthly if disk space is limited

---

## Summary: Quick Reference

```powershell
# Complete manual fix sequence
cd mobile
.\gradlew.bat --stop
Get-Process -Name "java" | Where-Object { (Get-CimInstance Win32_Process -Filter "ProcessId = $($_.Id)").CommandLine -like "*gradle*" } | Stop-Process -Force
Get-Process -Name "studio64","studio" -ErrorAction SilentlyContinue | Stop-Process -Force
Remove-Item -Path ".gradle" -Recurse -Force -ErrorAction SilentlyContinue
.\gradlew.bat assembleDebug --no-daemon --no-parallel --max-workers=1
```

---

## Scripts Provided

- **`gradle-unlock.ps1`**: Automated lock release script
- **`gradle-build-debug.ps1`**: Safe build script with lock-prevention flags

Both scripts are located in `mobile\scripts\` and can be run from the `mobile` directory.



