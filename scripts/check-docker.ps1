#!/usr/bin/env pwsh
# Check if Docker is running and ready

Write-Host "Checking Docker status..." -ForegroundColor Yellow

try {
    $dockerVersion = docker version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Docker is running" -ForegroundColor Green
        Write-Host ""
        
        # Check if Docker Compose is available
        try {
            docker compose version | Out-Null
            Write-Host "✅ Docker Compose is available" -ForegroundColor Green
            return 0
        } catch {
            Write-Host "⚠️  Docker Compose not found. Trying 'docker-compose'..." -ForegroundColor Yellow
            docker-compose version | Out-Null
            Write-Host "✅ docker-compose is available" -ForegroundColor Green
            return 0
        }
    } else {
        Write-Host "❌ Docker is not running" -ForegroundColor Red
        Write-Host ""
        Write-Host "Please start Docker Desktop:" -ForegroundColor Yellow
        Write-Host "  1. Open Docker Desktop from Start Menu" -ForegroundColor White
        Write-Host "  2. Wait for it to fully start (whale icon in system tray)" -ForegroundColor White
        Write-Host "  3. Look for 'Docker Desktop is running' message" -ForegroundColor White
        Write-Host ""
        Write-Host "Then run this script again." -ForegroundColor Cyan
        return 1
    }
} catch {
    Write-Host "❌ Docker is not installed or not running" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please:" -ForegroundColor Yellow
    Write-Host "  1. Install Docker Desktop from: https://www.docker.com/products/docker-desktop" -ForegroundColor White
    Write-Host "  2. Start Docker Desktop" -ForegroundColor White
    Write-Host "  3. Wait for it to fully start" -ForegroundColor White
    return 1
}

