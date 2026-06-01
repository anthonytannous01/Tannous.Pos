#!/usr/bin/env pwsh

Write-Host "🚀 Starting Tannous POS Development Environment..." -ForegroundColor Green

# Check if Docker is running
try {
    docker version | Out-Null
    Write-Host "✅ Docker is running" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# Set default connection string if not already set
if (-not $env:DB_CONNECTION_STRING) {
    $env:DB_CONNECTION_STRING = "Host=localhost;Port=5432;Database=TannousPOS;Username=postgres;Password=postgres"
    Write-Host "ℹ️  Using default connection string (set DB_CONNECTION_STRING env var to override)" -ForegroundColor Gray
}

# Start PostgreSQL database
Write-Host "📦 Starting PostgreSQL database..." -ForegroundColor Yellow
docker compose up -d db

# Wait for database to be ready (check healthcheck)
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

if (-not $ready) {
    Write-Host "`n❌ Database did not become ready in time. Check logs with: docker compose logs db" -ForegroundColor Red
    exit 1
}

# Add dotnet tools to PATH if needed
if (-not (Get-Command dotnet-ef -ErrorAction SilentlyContinue)) {
    $env:PATH += ";$env:USERPROFILE\.dotnet\tools"
}

# Update database
Write-Host "🗄️  Updating database schema..." -ForegroundColor Yellow
Set-Location "Tannous.Pos.Infrastructure"
dotnet ef database update --startup-project ..\Tannous.Pos.WebApi
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Migration failed. Check the error above." -ForegroundColor Red
    exit 1
}
Set-Location ".."

# Start the API
Write-Host "🌐 Starting API..." -ForegroundColor Yellow
Write-Host "API will be available at: http://localhost:8080" -ForegroundColor Cyan
Write-Host "Swagger UI will be available at: http://localhost:8080" -ForegroundColor Cyan
Write-Host "Health check: http://localhost:8080/health/ready" -ForegroundColor Cyan
Write-Host "`nPress Ctrl+C to stop the API" -ForegroundColor Gray

Set-Location "Tannous.Pos.WebApi"
dotnet run --urls "http://localhost:8080"
