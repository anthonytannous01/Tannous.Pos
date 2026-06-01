#!/usr/bin/env pwsh

Write-Host "📚 Generating OpenAPI Documentation..." -ForegroundColor Green
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Create artifacts directory if it doesn't exist
if (!(Test-Path "artifacts")) {
    New-Item -ItemType Directory -Path "artifacts"
}

# Build the project
Write-Host "🔨 Building project..." -ForegroundColor Yellow
dotnet build Tannous.Pos.WebApi/Tannous.Pos.WebApi.csproj --configuration Release

# Start the API in background
Write-Host "🌐 Starting API for OpenAPI generation..." -ForegroundColor Yellow
$process = Start-Process -FilePath "dotnet" -ArgumentList "run --project Tannous.Pos.WebApi/Tannous.Pos.WebApi.csproj --configuration Release --urls http://localhost:5000" -PassThru -WindowStyle Hidden

# Wait for API readiness instead of fixed sleep
Write-Host "⏳ Waiting for API readiness..." -ForegroundColor Yellow
$ready = $false
for ($attempt = 1; $attempt -le 30; $attempt++) {
    try {
        $health = Invoke-WebRequest -Uri "http://localhost:5000/health/ready" -UseBasicParsing -TimeoutSec 2
        if ($health.StatusCode -ge 200 -and $health.StatusCode -lt 500) {
            $ready = $true
            break
        }
    } catch {
        Start-Sleep -Seconds 1
    }
}

try {
    if (-not $ready) {
        throw "API did not become ready on http://localhost:5000/health/ready within timeout."
    }

    # Download OpenAPI spec
    Write-Host "📥 Downloading OpenAPI specification..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri "http://localhost:5000/swagger/v1/swagger.json" -OutFile "artifacts/openapi.json"
    
    Write-Host "✅ OpenAPI specification saved to: artifacts/openapi.json" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to download OpenAPI specification" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
} finally {
    # Stop the API
    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
        Write-Host "🛑 API stopped" -ForegroundColor Yellow
    }
}
