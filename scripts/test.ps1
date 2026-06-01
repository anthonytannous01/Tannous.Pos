#!/usr/bin/env pwsh

Write-Host "🧪 Running Tannous POS Tests..." -ForegroundColor Green

# Stop on first error for reliable local feedback
$ErrorActionPreference = "Stop"

# Build first so test runs do not rely on stale local outputs
Write-Host "🔨 Building solution (Release)..." -ForegroundColor Yellow
dotnet build --configuration Release --verbosity minimal

# Run tests
Write-Host "📋 Running solution tests..." -ForegroundColor Yellow
dotnet test --no-build --configuration Release --verbosity normal

# Run integration tests
Write-Host "🔗 Running integration tests..." -ForegroundColor Yellow
dotnet test tests/Tannous.Pos.Integration/Tannous.Pos.Integration.csproj --no-build --configuration Release --verbosity normal

Write-Host "✅ Tests completed!" -ForegroundColor Green
