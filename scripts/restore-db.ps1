#!/usr/bin/env pwsh
# Tannous POS Database Restore Script
# Usage: .\scripts\restore-db.ps1 <path-to-dump-file>
#
# Expects backups from backup-db.ps1 (pg_dump -Fc custom format, binary).
# Uses docker cp + pg_restore inside the container (reliable on Windows; avoids piping binary through Get-Content).

param(
    [Parameter(Mandatory = $true)]
    [string]$BackupFile,
    [string]$ContainerName = "tannous-pos-db",
    [string]$DatabaseName = "TannousPOS",
    [string]$Username = "postgres"
)

$ErrorActionPreference = "Stop"

try {
    if (-not (Test-Path $BackupFile)) {
        Write-Host "Backup file not found: $BackupFile" -ForegroundColor Red
        exit 1
    }

    $fileSize = (Get-Item $BackupFile).Length
    $fileSizeMB = [math]::Round($fileSize / 1MB, 2)

    Write-Host "Starting database restore..." -ForegroundColor Green
    Write-Host "Container: $ContainerName" -ForegroundColor Yellow
    Write-Host "Database: $DatabaseName" -ForegroundColor Yellow
    Write-Host "Backup file: $BackupFile" -ForegroundColor Yellow
    Write-Host "File size: $fileSizeMB MB" -ForegroundColor Yellow

    $confirmation = Read-Host "This will overwrite objects in the existing database. Continue? (y/N)"
    if ($confirmation -ne "y" -and $confirmation -ne "Y") {
        Write-Host "Restore cancelled by user" -ForegroundColor Yellow
        exit 0
    }

    $remotePath = "/tmp/tannouspos_restore.dump"
    Write-Host "Copying dump into container..." -ForegroundColor Yellow
    docker cp $BackupFile "${ContainerName}:${remotePath}"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "docker cp failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }

    Write-Host "Running pg_restore..." -ForegroundColor Yellow
    docker exec $ContainerName pg_restore -U $Username -d $DatabaseName --clean --if-exists $remotePath
    $restoreExit = $LASTEXITCODE

    docker exec $ContainerName rm -f $remotePath 2>$null

    if ($restoreExit -eq 0) {
        Write-Host "Restore completed successfully." -ForegroundColor Green
        Write-Host "Restored from: $BackupFile" -ForegroundColor Cyan
        Write-Host "Completed at: $(Get-Date)" -ForegroundColor Cyan
    }
    else {
        Write-Host "pg_restore failed with exit code: $restoreExit" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "Error during restore: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
