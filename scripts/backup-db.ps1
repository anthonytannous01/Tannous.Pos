#!/usr/bin/env pwsh
# Tannous POS Database Backup Script
# Usage: .\scripts\backup-db.ps1
# Output: custom-format (-Fc) binary dump for pg_restore (see restore-db.ps1).

param(
    [string]$ContainerName = "tannous-pos-db",
    [string]$DatabaseName = "TannousPOS",
    [string]$Username = "postgres"
)

$ErrorActionPreference = "Stop"

try {
    # Create backups directory if it doesn't exist
    $backupsDir = "backups"
    if (-not (Test-Path $backupsDir)) {
        New-Item -ItemType Directory -Path $backupsDir -Force | Out-Null
    }

    # Generate timestamp for filename
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupFile = "$backupsDir/tannouspos_${timestamp}.dump"

    Write-Host "Starting database backup..." -ForegroundColor Green
    Write-Host "Container: $ContainerName" -ForegroundColor Yellow
    Write-Host "Database: $DatabaseName" -ForegroundColor Yellow
    Write-Host "Backup file: $backupFile" -ForegroundColor Yellow

    # Execute pg_dump
    docker exec $ContainerName pg_dump -U $Username -d $DatabaseName -Fc > $backupFile

    if ($LASTEXITCODE -eq 0) {
        $fileSize = (Get-Item $backupFile).Length
        $fileSizeMB = [math]::Round($fileSize / 1MB, 2)
        Write-Host "✅ Backup completed successfully!" -ForegroundColor Green
        Write-Host "📁 File: $backupFile" -ForegroundColor Cyan
        Write-Host "📊 Size: $fileSizeMB MB" -ForegroundColor Cyan
        Write-Host "🕒 Timestamp: $timestamp" -ForegroundColor Cyan
    } else {
        Write-Host "❌ Backup failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "❌ Error during backup: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
