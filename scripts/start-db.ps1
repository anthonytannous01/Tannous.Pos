#!/usr/bin/env pwsh
# Tannous POS - Start Database Only
# Usage: .\scripts\start-db.ps1

Write-Host "📦 Starting PostgreSQL database..." -ForegroundColor Yellow

# Check if Docker is running
try {
    docker version | Out-Null
} catch {
    Write-Host "❌ Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# Start database service
docker compose up -d db

# Wait for database to be ready
Write-Host "⏳ Waiting for database to be ready..." -ForegroundColor Yellow
$maxAttempts = 30
$attempt = 0
$ready = $false

while ($attempt -lt $maxAttempts -and -not $ready) {
    Start-Sleep -Seconds 1
    $health = docker inspect --format='{{.State.Health.Status}}' tannous-pos-db 2>$null
    if ($health -eq "healthy") {
        $ready = $true
        Write-Host "✅ Database is ready!" -ForegroundColor Green
    } else {
        $attempt++
        Write-Host "." -NoNewline -ForegroundColor Gray
    }
}

if ($ready) {
    Write-Host "`n✅ PostgreSQL is running and healthy" -ForegroundColor Green
    Write-Host "   Container: tannous-pos-db" -ForegroundColor Cyan
    Write-Host "   Port: 5432" -ForegroundColor Cyan
    Write-Host "   Database: TannousPOS" -ForegroundColor Cyan
    Write-Host "   Username: postgres" -ForegroundColor Cyan
    Write-Host "`nTo stop: docker compose stop db" -ForegroundColor Gray
    Write-Host "To view logs: docker compose logs -f db" -ForegroundColor Gray
} else {
    Write-Host "`n❌ Database did not become ready in time." -ForegroundColor Red
    Write-Host "Check logs with: docker compose logs db" -ForegroundColor Yellow
    exit 1
}

