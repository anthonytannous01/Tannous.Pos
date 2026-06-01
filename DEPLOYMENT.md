# Tannous POS - Local Development Runbook

This guide provides step-by-step instructions for setting up and running the Tannous POS backend and Android app locally.

## Prerequisites

- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)
- **EF Core Tools** - `dotnet tool install --global dotnet-ef --version 8.0.0`
- **Android Studio** (for Android development) - [Download](https://developer.android.com/studio)
- **PostgreSQL Client** (optional, for direct database access)

## Quick Start (One Command)

**Windows (PowerShell):**
```powershell
.\scripts\dev-up.ps1
```

This script automates the entire setup. For manual setup, follow the steps below.

---

## Backend Setup

### Step 1: Configure Environment Variables

**Option A: Create .env file (Recommended)**

1. Copy the example file:
   ```powershell
   Copy-Item .env.example .env
   ```

2. Edit `.env` and set your values:
   ```env
   DB_CONNECTION_STRING=Host=localhost;Port=5432;Database=TannousPOS;Username=postgres;Password=postgres
   JWT_KEY=your-super-secret-key-with-at-least-32-characters-minimum-for-jwt-signing
   ASPNETCORE_ENVIRONMENT=Development
   ASPNETCORE_URLS=http://0.0.0.0:7000
   SEED_ADMIN_USERNAME=admin
   SEED_ADMIN_PASSWORD=SecurePass123!
   SEED_ADMIN_FIRSTNAME=Admin
   SEED_ADMIN_LASTNAME=User
   SEED_ADMIN_EMAIL=admin@tannouspos.com
   ```

**Option B: Set environment variables directly (PowerShell)**
```powershell
$env:DB_CONNECTION_STRING = "Host=localhost;Port=5432;Database=TannousPOS;Username=postgres;Password=postgres"
$env:JWT_KEY = "your-super-secret-key-with-at-least-32-characters-minimum-for-jwt-signing"
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://0.0.0.0:7000"
$env:SEED_ADMIN_USERNAME = "admin"
$env:SEED_ADMIN_PASSWORD = "SecurePass123!"
$env:SEED_ADMIN_FIRSTNAME = "Admin"
$env:SEED_ADMIN_LASTNAME = "User"
$env:SEED_ADMIN_EMAIL = "admin@tannouspos.com"
```

**⚠️ Important:** Never commit `.env` to version control. It contains secrets.

### Step 2: Start PostgreSQL Database

```powershell
docker compose up -d db
```

**Verify database is healthy:**
```powershell
docker compose ps db
# Should show "healthy" status
```

**View database logs:**
```powershell
docker compose logs -f db
```

### Step 3: Run Database Migrations

```powershell
# Navigate to Infrastructure project
cd Tannous.Pos.Infrastructure

# Run migrations
dotnet ef database update --startup-project ..\Tannous.Pos.WebApi

# Return to root
cd ..
```

**Verify migrations:**
```powershell
# List applied migrations
cd Tannous.Pos.Infrastructure
dotnet ef migrations list --startup-project ..\Tannous.Pos.WebApi
cd ..
```

### Step 4: Start the Web API

```powershell
cd Tannous.Pos.WebApi
dotnet run
```

**Or with explicit URL:**
```powershell
cd Tannous.Pos.WebApi
dotnet run --urls "http://0.0.0.0:7000"
```

The API will start on **http://localhost:7000**

### Step 5: Verify Setup

**Health Check:**
```powershell
Invoke-RestMethod -Uri "http://localhost:7000/health/ready"
```

**Swagger UI:**
Open browser to: **http://localhost:7000**

**Test Login:**
```powershell
$body = @{
    username = "admin"
    password = "SecurePass123!"
} | ConvertTo-Json

$headers = @{
    "Content-Type" = "application/json"
    "Device-Id" = "test-device-123"
}

Invoke-RestMethod -Uri "http://localhost:7000/api/v1.0/auth/login" -Method POST -Headers $headers -Body $body
```

### Step 6: Seed Admin User (If Not Auto-Seeded)

Admin user is automatically seeded in Development environment if `SEED_ADMIN_*` environment variables are set.

**Verify admin user exists:**
```powershell
# Login with seeded credentials
$loginResponse = Invoke-RestMethod -Uri "http://localhost:7000/api/v1.0/auth/login" -Method POST -Headers @{"Content-Type"="application/json";"Device-Id"="test-device"} -Body (@{username="admin";password="SecurePass123!"} | ConvertTo-Json)
$loginResponse | ConvertTo-Json -Depth 10
```

---

## Android Setup

### Step 1: Configure Base URL

The Android app uses different base URLs depending on the environment:

**For Android Emulator:**
- Base URL: `http://10.0.2.2:7000/api/v1.0/`
- `10.0.2.2` is the special IP that maps to the host machine's `localhost` from the emulator

**For Physical Device:**
- Base URL: `http://<YOUR_MACHINE_IP>:7000/api/v1.0/`
- Find your machine's IP:
  - **Windows:** `ipconfig` (look for IPv4 Address)
  - **macOS/Linux:** `ifconfig` or `ip addr`

**Configuration Method:**

The base URL is configured in `mobile/core/build.gradle.kts`:

```kotlin
buildConfigField("String", "BASE_URL", "\"http://10.0.2.2:7000/api/v1.0/\"")
```

**To change for physical device:**

1. Find your machine's LAN IP (e.g., `192.168.1.100`)
2. Edit `mobile/core/build.gradle.kts`:
   ```kotlin
   buildConfigField("String", "BASE_URL", "\"http://192.168.1.100:7000/api/v1.0/\"")
   ```
3. Rebuild the app: `./gradlew clean assembleDebug`

**Alternative: Using local.properties (Recommended for Team Development)**

1. Add to `mobile/local.properties` (this file is gitignored):
   ```properties
   # API Base URL Configuration
   # For emulator: http://10.0.2.2:7000/api/v1.0/
   # For physical device: http://<YOUR_IP>:7000/api/v1.0/
   API_BASE_URL=http://10.0.2.2:7000/api/v1.0/
   ```

2. Update `mobile/core/build.gradle.kts` to read from local.properties:
   ```kotlin
   val apiBaseUrl = project.findProperty("API_BASE_URL") as String? 
       ?: "\"http://10.0.2.2:7000/api/v1.0/\""
   buildConfigField("String", "BASE_URL", apiBaseUrl)
   ```

3. Load in `mobile/core/build.gradle.kts`:
   ```kotlin
   val localProperties = java.util.Properties()
   val localPropertiesFile = rootProject.file("local.properties")
   if (localPropertiesFile.exists()) {
       localProperties.load(java.io.FileInputStream(localPropertiesFile))
   }
   val apiBaseUrl = localProperties.getProperty("API_BASE_URL", "http://10.0.2.2:7000/api/v1.0/")
   buildConfigField("String", "BASE_URL", "\"$apiBaseUrl\"")
   ```

### Step 2: Build and Run Android App

```bash
cd mobile
./gradlew assembleDebug
```

**Run on emulator:**
```bash
./gradlew installDebug
adb shell am start -n com.tannous.pos/.MainActivity
```

**Or use Android Studio:**
1. Open `mobile` folder in Android Studio
2. Select an emulator or connected device
3. Click "Run" (green play button)

### Step 3: Verify Android Connection

1. Start the backend API (see Backend Setup above)
2. Run the Android app
3. Attempt to login
4. Check backend logs for incoming requests

**Troubleshooting Android Connection:**

- **Emulator can't connect:** Ensure backend is running on `0.0.0.0:7000` (not just `localhost:7000`)
- **Physical device can't connect:** 
  - Ensure device and computer are on same network
  - Check Windows Firewall allows port 7000
  - Verify backend is listening on `0.0.0.0:7000`
  - Use machine's LAN IP, not `localhost` or `127.0.0.1`

---

## Environment Variables Reference

### Required Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `DB_CONNECTION_STRING` | PostgreSQL connection string | `Host=localhost;Port=5432;Database=TannousPOS;Username=postgres;Password=postgres` |
| `JWT_KEY` | JWT signing key (min 32 chars) | `your-super-secret-key-with-at-least-32-characters` |

### Optional Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Development` |
| `ASPNETCORE_URLS` | URLs to listen on | `http://0.0.0.0:7000` |
| `JWT_ISSUER` | JWT issuer | `TannousPOS` |
| `JWT_AUDIENCE` | JWT audience | `TannousPOS` |
| `JWT_ACCESS_TOKEN_EXPIRY_MINUTES` | Access token expiry | `15` |
| `JWT_REFRESH_TOKEN_EXPIRY_DAYS` | Refresh token expiry | `30` |
| `SEED_ADMIN_USERNAME` | Admin username for seeding | - |
| `SEED_ADMIN_PASSWORD` | Admin password for seeding | - |
| `SEED_ADMIN_FIRSTNAME` | Admin first name | - |
| `SEED_ADMIN_LASTNAME` | Admin last name | - |
| `SEED_ADMIN_EMAIL` | Admin email | - |

### Docker Compose Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `POSTGRES_DB` | Database name | `TannousPOS` |
| `POSTGRES_USER` | Database user | `postgres` |
| `POSTGRES_PASSWORD` | Database password | `postgres` |

---

## Common Commands

### Database Management

```powershell
# Start database
docker compose up -d db

# Stop database
docker compose stop db

# View database logs
docker compose logs -f db

# Check database status
docker compose ps db

# Connect to database (psql)
docker exec -it tannous-pos-db psql -U postgres -d TannousPOS

# Backup database
.\scripts\backup-db.ps1

# Restore database
.\scripts\restore-db.ps1 -BackupFile "backups\tannouspos_20250102_120000.dump"
```

### API Management

```powershell
# Run API
cd Tannous.Pos.WebApi
dotnet run

# Run with specific URL
dotnet run --urls "http://0.0.0.0:7000"

# Build API
dotnet build

# Run tests
dotnet test
```

### Migrations

```powershell
# Create new migration
cd Tannous.Pos.Infrastructure
dotnet ef migrations add MigrationName --startup-project ..\Tannous.Pos.WebApi

# Apply migrations
dotnet ef database update --startup-project ..\Tannous.Pos.WebApi

# List migrations
dotnet ef migrations list --startup-project ..\Tannous.Pos.WebApi

# Remove last migration (if not applied)
dotnet ef migrations remove --startup-project ..\Tannous.Pos.WebApi
```

### Android

```bash
# Build debug APK
cd mobile
./gradlew assembleDebug

# Install on connected device
./gradlew installDebug

# Run tests
./gradlew test

# Clean build
./gradlew clean
```

---

## Troubleshooting

### Backend Issues

**Error: "Docker is not running"**
- Start Docker Desktop and wait for it to fully initialize

**Error: "Failed to connect to database"**
- Verify database container is running: `docker compose ps db`
- Check connection string matches docker-compose credentials
- Ensure port 5432 is not in use by another service

**Error: "JWT signing key not configured"**
- Set `JWT_KEY` environment variable (min 32 characters)
- Verify it's set: `echo $env:JWT_KEY` (PowerShell)

**Error: "Database connection string not configured"**
- Set `DB_CONNECTION_STRING` environment variable
- Or create `.env` file from `.env.example`

**Error: "dotnet-ef command not found"**
```powershell
dotnet tool install --global dotnet-ef --version 8.0.0
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"
```

**API not accessible from Android emulator**
- Ensure API is listening on `0.0.0.0:7000` (not just `localhost:7000`)
- Check `ASPNETCORE_URLS` environment variable
- Verify firewall allows port 7000

### Android Issues

**App can't connect to backend (Emulator)**
- Verify backend is running on port 7000
- Check base URL is `http://10.0.2.2:7000/api/v1.0/`
- Ensure backend listens on `0.0.0.0:7000`

**App can't connect to backend (Physical Device)**
- Find your machine's LAN IP: `ipconfig` (Windows) or `ifconfig` (macOS/Linux)
- Update base URL in `build.gradle.kts` to use LAN IP
- Ensure device and computer are on same network
- Check Windows Firewall allows port 7000

**Build fails with "BASE_URL not found"**
- Verify `buildConfigField` is set in `mobile/core/build.gradle.kts`
- Clean and rebuild: `./gradlew clean assembleDebug`

---

## Security Notes

1. **Never commit secrets:**
   - `.env` file (contains real passwords/keys)
   - `local.properties` (may contain API keys)
   - `appsettings.Production.json` (should only have placeholders)

2. **Use strong passwords in production:**
   - Generate secure JWT keys: `openssl rand -base64 32`
   - Use strong database passwords
   - Rotate secrets regularly

3. **Environment-specific configuration:**
   - Development: Use `.env` file locally
   - Staging/Production: Use environment variables or secret management (Azure Key Vault, AWS Secrets Manager)

---

## Next Steps

- **Backend API:** See [README.md](README.md) for API documentation
- **Database Setup:** See [DATABASE_SETUP.md](DATABASE_SETUP.md) for detailed database instructions
- **Authentication:** See [AUTHENTICATION_SETUP.md](AUTHENTICATION_SETUP.md) for auth configuration
- **Authorization:** See [AUTHORIZATION_POLICIES.md](AUTHORIZATION_POLICIES.md) for policy details


