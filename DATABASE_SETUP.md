# Database Setup Guide

## Quick Start (One Command)

```powershell
.\scripts\dev-up.ps1
```

This script will:
1. ✅ Check Docker is running
2. 📦 Start PostgreSQL database container
3. ⏳ Wait for database to be healthy
4. 🗄️ Run EF Core migrations
5. 🌐 Start the WebApi server

## Manual Setup (Step by Step)

### Prerequisites
- Docker Desktop installed and running
- .NET 8 SDK installed
- EF Core tools installed: `dotnet tool install --global dotnet-ef --version 8.0.0`

### Step 1: Start PostgreSQL Database

**Option A: Using the helper script (recommended)**
```powershell
.\scripts\start-db.ps1
```

**Option B: Using Docker Compose directly**
```powershell
docker compose up -d db
```

**Verify database is running:**
```powershell
docker compose ps db
# Should show "healthy" status
```

### Step 2: Set Connection String

The connection string is automatically set by `dev-up.ps1`. For manual setup:

```powershell
# Default connection string (matches docker-compose.yml defaults)
$env:DB_CONNECTION_STRING = "Host=localhost;Port=5432;Database=TannousPOS;Username=postgres;Password=postgres"
```

**Alternative: Using User Secrets**
```powershell
Set-Location Tannous.Pos.WebApi
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=TannousPOS;Username=postgres;Password=postgres"
Set-Location ..
```

### Step 3: Run Migrations

```powershell
# Add dotnet tools to PATH (if not already)
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"

# Navigate to Infrastructure project
Set-Location Tannous.Pos.Infrastructure

# Run migrations
dotnet ef database update --startup-project ..\Tannous.Pos.WebApi

# Return to root
Set-Location ..
```

### Step 4: Start WebApi

```powershell
Set-Location Tannous.Pos.WebApi
dotnet run --urls "http://localhost:8080"
```

### Step 5: Verify

**Health Check:**
```powershell
curl http://localhost:8080/health/ready
```

**Swagger UI:**
Open browser to: http://localhost:8080

## Database Configuration

### Default Credentials (docker-compose.yml)
- **Host:** localhost
- **Port:** 5432
- **Database:** TannousPOS
- **Username:** postgres
- **Password:** postgres

### Using Custom Credentials

**Option 1: Environment Variables (Recommended)**

Create a `.env` file in the project root (copy from `.env.example`):
```env
POSTGRES_DB=TannousPOS
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_custom_password
DB_CONNECTION_STRING=Host=localhost;Port=5432;Database=TannousPOS;Username=postgres;Password=your_custom_password
```

Then start with:
```powershell
docker compose --env-file .env up -d db
$env:DB_CONNECTION_STRING = "Host=localhost;Port=5432;Database=TannousPOS;Username=postgres;Password=your_custom_password"
```

**Option 2: Modify docker-compose.yml directly** (not recommended for production)

## Connection String Priority

The `PosDbContextFactory` loads connection strings in this order:

1. **Environment Variable `DB_CONNECTION_STRING`** (Highest Priority)
2. User Secrets (`ConnectionStrings:Default` or `DB_CONNECTION_STRING`)
3. `appsettings.Development.json` → `ConnectionStrings:Default`
4. `appsettings.json` → `ConnectionStrings:Default`

## Common Commands

### Start Database
```powershell
docker compose up -d db
```

### Stop Database
```powershell
docker compose stop db
```

### Stop and Remove Database (⚠️ Deletes data)
```powershell
docker compose down db
```

### View Database Logs
```powershell
docker compose logs -f db
```

### Check Database Status
```powershell
docker compose ps db
```

### Connect to Database (psql)
```powershell
docker exec -it tannous-pos-db psql -U postgres -d TannousPOS
```

### Backup Database
```powershell
.\scripts\backup-db.ps1
```

### Restore Database
```powershell
.\scripts\restore-db.ps1 -BackupFile "backups\tannouspos_20250102_120000.dump"
```

## Troubleshooting

### Error: "Docker is not running"
**Solution:** Start Docker Desktop and wait for it to fully start.

### Error: "Failed to connect to 127.0.0.1:5432"
**Solution:** 
1. Check if database container is running: `docker compose ps db`
2. Check if port 5432 is already in use: `netstat -ano | findstr :5432`
3. Start the database: `docker compose up -d db`
4. Wait for healthcheck: `docker compose logs db` (should show "database system is ready")

### Error: "password authentication failed"
**Solution:** 
1. Verify connection string matches docker-compose.yml credentials
2. Check if you're using custom password (update both docker-compose and connection string)
3. Restart container: `docker compose restart db`

### Error: "Database connection string not found"
**Solution:** Set the environment variable:
```powershell
$env:DB_CONNECTION_STRING = "Host=localhost;Port=5432;Database=TannousPOS;Username=postgres;Password=postgres"
```

### Error: "dotnet-ef command not found"
**Solution:** 
```powershell
# Install EF Core tools
dotnet tool install --global dotnet-ef --version 8.0.0

# Add to PATH for current session
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"

# Or permanently (requires new PowerShell session)
setx PATH "$env:PATH;$env:USERPROFILE\.dotnet\tools"
```

### Migration Fails: "relation already exists"
**Solution:** Database may have partial migration. Check applied migrations:
```powershell
Set-Location Tannous.Pos.Infrastructure
dotnet ef migrations list --startup-project ..\Tannous.Pos.WebApi
```

If needed, reset database (⚠️ **WARNING: Deletes all data**):
```powershell
docker compose down -v db  # Remove volume
docker compose up -d db     # Recreate
dotnet ef database update --startup-project ..\Tannous.Pos.WebApi
```

## Data Persistence

Database data is stored in a Docker named volume `pgdata`. This means:
- ✅ Data persists across container restarts
- ✅ Data persists when you stop/start the container
- ⚠️ Data is deleted when you run `docker compose down -v`

To backup data, use: `.\scripts\backup-db.ps1`

## Production Considerations

For production:
1. Use strong passwords (set via environment variables, never commit)
2. Use `.env` file (not committed to git) or Azure Key Vault / AWS Secrets Manager
3. Consider using `docker-compose.prod.yml` with proper networking and security
4. Enable SSL/TLS for database connections
5. Use managed database services (Azure Database for PostgreSQL, AWS RDS) instead of containers

