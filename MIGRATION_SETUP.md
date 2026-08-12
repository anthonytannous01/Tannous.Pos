# EF Core Migration Setup Guide

## ⚠️ GOVERNANCE RULE — never hand-write migration files

**All migrations MUST be generated with `dotnet ef migrations add <Name>`. Never write a
migration `.cs` file by hand (this applies to AI-generated code too).**

Why: every migration from Step 87 (June 2026) through Step 110 was hand-written without a
`.Designer.cs` file, which means `PosDbContextModelSnapshot.cs` was never updated. The
database stayed correct, but the snapshot drifted ~20 migrations behind. The first generated
migration afterwards (`DualCurrencyDrawer`, Aug 2026) diffed against the stale snapshot,
tried to recreate half the schema, and failed against the live DB. The snapshot was
re-baselined in that migration — this rule keeps the drift from returning.

How to tell a migration was generated correctly: it has a matching `.Designer.cs` file and
the same commit touches `PosDbContextModelSnapshot.cs`. A migration commit missing either of
those is drift in the making — reject it in review.

Optional CI guard (EF Core 8+): `dotnet ef migrations has-pending-model-changes` exits
non-zero when the model has changes not captured by a migration.

## Overview

The `PosDbContextFactory` has been created to enable design-time migrations. It loads the connection string from multiple sources in priority order.

## Connection String Priority

1. **Environment Variable `DB_CONNECTION_STRING`** (Highest Priority)
2. User Secrets (from WebApi project, if available)
3. `appsettings.Development.json` → `ConnectionStrings:Default`
4. `appsettings.json` → `ConnectionStrings:Default`

## PowerShell Commands

### Option 1: Using Environment Variable (Recommended)

```powershell
# 1. Set the connection string environment variable
# For local PostgreSQL (default docker-compose credentials):
$env:DB_CONNECTION_STRING = "Host=localhost;Port=5432;Database=TannousPOS;Username=postgres;Password=postgres"

# Or if using a different port (e.g., 5120):
# $env:DB_CONNECTION_STRING = "Host=localhost;Port=5120;Database=TannousPOS;Username=postgres;Password=your_password"

# 2. Add dotnet tools to PATH (if not already added)
$env:PATH += ";C:\Users\user\.dotnet\tools"

# 3. Navigate to Infrastructure directory
Set-Location "C:\Users\user\Tannous.Pos\Tannous.Pos.Infrastructure"

# 4. Run the migration
dotnet ef database update --startup-project ..\Tannous.Pos.WebApi
```

### Option 2: Using User Secrets

```powershell
# 1. Navigate to WebApi project
Set-Location "C:\Users\user\Tannous.Pos\Tannous.Pos.WebApi"

# 2. Set user secret
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=TannousPOS;Username=postgres;Password=postgres"

# 3. Navigate to Infrastructure directory
Set-Location "..\Tannous.Pos.Infrastructure"

# 4. Add dotnet tools to PATH
$env:PATH += ";C:\Users\user\.dotnet\tools"

# 5. Run the migration
dotnet ef database update --startup-project ..\Tannous.Pos.WebApi
```

### Option 3: Using appsettings.Development.json (Not Recommended for Production)

**Note:** This method requires hardcoding the password in the file, which is not recommended. Use environment variables or user secrets instead.

If you must use this method, update `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=TannousPOS;Username=postgres;Password=postgres"
  }
}
```

Then run:
```powershell
$env:PATH += ";C:\Users\user\.dotnet\tools"
Set-Location "C:\Users\user\Tannous.Pos\Tannous.Pos.Infrastructure"
dotnet ef database update --startup-project ..\Tannous.Pos.WebApi
```

## Docker Compose Database

If using `docker-compose.yml`, the default PostgreSQL credentials are:
- **Host:** localhost
- **Port:** 5432
- **Database:** TannousPOS
- **Username:** postgres
- **Password:** postgres

To start the database:
```powershell
docker-compose up -d db
```

## Troubleshooting

### Error: "Database connection string not found"

**Solution:** Set the `DB_CONNECTION_STRING` environment variable:
```powershell
$env:DB_CONNECTION_STRING = "Host=localhost;Port=5432;Database=TannousPOS;Username=postgres;Password=postgres"
```

### Error: "password authentication failed"

**Solution:** Verify:
1. PostgreSQL is running (`docker-compose ps` or check service)
2. Credentials are correct
3. Database exists (will be created automatically if user has permissions)

### Error: "dotnet-ef command not found"

**Solution:** Install or add to PATH:
```powershell
# Install
dotnet tool install --global dotnet-ef --version 8.0.0

# Add to PATH for current session
$env:PATH += ";C:\Users\user\.dotnet\tools"

# Or permanently (requires new PowerShell session)
setx PATH "$env:PATH;C:\Users\user\.dotnet\tools"
```

## Files Changed

1. **`Tannous.Pos.Infrastructure/Data/PosDbContextFactory.cs`** (NEW)
   - Implements `IDesignTimeDbContextFactory<PosDbContext>`
   - Loads connection string from environment variables, user secrets, and appsettings files
   - Provides clear error messages if connection string is not found

2. **`Tannous.Pos.Infrastructure/Tannous.Pos.Infrastructure.csproj`**
   - Added `Microsoft.EntityFrameworkCore.Design` package
   - Added `Microsoft.Extensions.Configuration.Json` package
   - Added `Microsoft.Extensions.Configuration.EnvironmentVariables` package
   - Added `Microsoft.Extensions.Configuration.UserSecrets` package

3. **`Tannous.Pos.WebApi/appsettings.Development.json`**
   - Updated connection string to placeholder (no hardcoded passwords)

## Verification

After running the migration, verify it succeeded:
```powershell
# Check migration status
$env:PATH += ";C:\Users\user\.dotnet\tools"
Set-Location "C:\Users\user\Tannous.Pos\Tannous.Pos.Infrastructure"
dotnet ef migrations list --startup-project ..\Tannous.Pos.WebApi
```

You should see all migrations including `AddUserNormalizedFields`.


