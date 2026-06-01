# Quick Start Guide - Tannous POS

## One-Command Setup (Recommended)

```powershell
.\scripts\dev-up.ps1
```

This single command will:
- ✅ Check Docker is running
- 📦 Start PostgreSQL database
- ⏳ Wait for database to be healthy
- 🗄️ Run all migrations
- 🌐 Start the WebApi server

**Then access:**
- Swagger UI: http://localhost:8080
- Health Check: http://localhost:8080/health/ready

---

## Manual Setup (Step-by-Step)

### Step 1: Start Database

```powershell
# Option A: Use helper script
.\scripts\start-db.ps1

# Option B: Direct Docker command
docker compose up -d db
```

### Step 2: Set Connection String

```powershell
$env:DB_CONNECTION_STRING = "Host=localhost;Port=5432;Database=TannousPOS;Username=postgres;Password=postgres"
```

### Step 3: Run Migrations

```powershell
# Add dotnet tools to PATH (if needed)
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"

# Run migrations
Set-Location Tannous.Pos.Infrastructure
dotnet ef database update --startup-project ..\Tannous.Pos.WebApi
Set-Location ..
```

### Step 4: Start WebApi

```powershell
Set-Location Tannous.Pos.WebApi
dotnet run --urls "http://localhost:8080"
```

### Step 5: Verify

```powershell
# Health check
curl http://localhost:8080/health/ready

# Or open in browser:
# http://localhost:8080
```

---

## All-in-One PowerShell Script

Copy and paste this entire block:

```powershell
# Start database
docker compose up -d db

# Wait for database (check health)
$maxAttempts = 30
$attempt = 0
while ($attempt -lt $maxAttempts) {
    Start-Sleep -Seconds 1
    $health = docker inspect --format='{{.State.Health.Status}}' tannous-pos-db 2>$null
    if ($health -eq "healthy") {
        Write-Host "✅ Database ready!" -ForegroundColor Green
        break
    }
    $attempt++
    Write-Host "." -NoNewline
}

# Set connection string
$env:DB_CONNECTION_STRING = "Host=localhost;Port=5432;Database=TannousPOS;Username=postgres;Password=postgres"

# Add dotnet tools to PATH
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"

# Run migrations
Set-Location Tannous.Pos.Infrastructure
dotnet ef database update --startup-project ..\Tannous.Pos.WebApi
Set-Location ..

# Start API
Set-Location Tannous.Pos.WebApi
dotnet run --urls "http://localhost:8080"
```

---

## Troubleshooting

**Docker not running?**
```powershell
# Start Docker Desktop, then wait 10-20 seconds
```

**Port 5432 already in use?**
```powershell
# Check what's using it
netstat -ano | findstr :5432

# Or use a different port in docker-compose.yml and connection string
```

**Migration fails?**
```powershell
# Check database is running
docker compose ps db

# View database logs
docker compose logs db
```

**For more details, see [DATABASE_SETUP.md](DATABASE_SETUP.md)**

