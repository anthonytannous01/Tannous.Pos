# Step 1: Admin User Management - Verification Checklist

## Implementation Summary

✅ **Completed:**
- Fixed `DevSeeder.cs` to properly set all required fields (FirstName, LastName, NormalizedUsername, NormalizedEmail)
- Enabled seeding in `Program.cs` with Development-only safety gate
- Added environment variable checks for safe seeding
- Updated documentation with PowerShell examples
- Seeding is idempotent (won't create duplicate users)

## Quick Verification Steps

### 1. Start Database

```powershell
# Option A: Using Docker Compose
docker-compose up -d db

# Option B: Using provided script
.\scripts\start-db.ps1

# Verify database is running
docker ps | Select-String postgres
```

### 2. Apply Migrations

```powershell
cd Tannous.Pos.Infrastructure
dotnet ef database update --startup-project ../Tannous.Pos.WebApi
```

**Expected Output:**
```
Applying migration '20251223145411_AddUserNormalizedFields'.
Done.
```

### 3. Set Environment Variables

```powershell
# Required seeding variables
$env:SEED_ADMIN_USERNAME = "admin"
$env:SEED_ADMIN_PASSWORD = "SecurePass123!"
$env:SEED_ADMIN_FIRSTNAME = "Admin"
$env:SEED_ADMIN_LASTNAME = "User"
$env:SEED_ADMIN_EMAIL = "admin@tannouspos.com"

# Required application variables
$env:JWT_KEY = "your-super-secret-key-with-at-least-32-characters-minimum"
$env:DB_CONNECTION_STRING = "Host=localhost;Database=TannousPOS;Username=postgres;Password=password"
$env:ASPNETCORE_ENVIRONMENT = "Development"

# Verify variables are set
Write-Host "SEED_ADMIN_USERNAME: $env:SEED_ADMIN_USERNAME"
Write-Host "SEED_ADMIN_PASSWORD: $env:SEED_ADMIN_PASSWORD"
Write-Host "SEED_ADMIN_FIRSTNAME: $env:SEED_ADMIN_FIRSTNAME"
Write-Host "SEED_ADMIN_LASTNAME: $env:SEED_ADMIN_LASTNAME"
Write-Host "SEED_ADMIN_EMAIL: $env:SEED_ADMIN_EMAIL"
```

### 4. Run WebApi

```powershell
cd Tannous.Pos.WebApi
dotnet run
```

**Expected Log Output:**
```
info: Tannous.Pos.Infrastructure.Persistence.Seed.DevSeeder[0]
      Admin user seeded successfully: Username 'admin', Email 'admin@tannouspos.com'
```

**OR if user already exists:**
```
info: Tannous.Pos.Infrastructure.Persistence.Seed.DevSeeder[0]
      Admin user seeding skipped: Owner user with username 'admin' already exists.
```

**OR if env vars not set:**
```
info: Tannous.Pos.Infrastructure.Persistence.Seed.DevSeeder[0]
      Admin user seeding skipped: Required environment variables (SEED_ADMIN_USERNAME, SEED_ADMIN_PASSWORD, SEED_ADMIN_FIRSTNAME, SEED_ADMIN_LASTNAME) are not set.
```

### 5. Login as Seeded Owner User

```powershell
# Login request
$response = Invoke-RestMethod -Uri "http://localhost:5000/api/v1.0/auth/login" `
  -Method POST `
  -ContentType "application/json" `
  -Body (@{
    username = "admin"
    password = "SecurePass123!"
  } | ConvertTo-Json)

# Display response
$response | ConvertTo-Json -Depth 10

# Save access token for subsequent requests
$accessToken = $response.accessToken
Write-Host "Access Token: $accessToken"
```

**Expected Response:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "base64-encoded-refresh-token",
  "expiresIn": 900,
  "user": {
    "id": "guid",
    "username": "admin",
    "email": "admin@tannouspos.com",
    "firstName": "Admin",
    "lastName": "User",
    "role": "Owner"
  }
}
```

### 6. Verify User Management Endpoints

#### List Users (as Owner)

```powershell
$headers = @{
  "Authorization" = "Bearer $accessToken"
}

$users = Invoke-RestMethod -Uri "http://localhost:5000/api/v1.0/users?page=1&pageSize=20" `
  -Method GET `
  -Headers $headers

$users | ConvertTo-Json -Depth 10
```

**Expected:** List of users including the seeded admin user

#### Create a Cashier User

```powershell
$newUser = @{
  username = "cashier1"
  email = "cashier1@tannouspos.com"
  password = "SecurePass123!"
  firstName = "John"
  lastName = "Doe"
  role = "Cashier"
} | ConvertTo-Json

$createdUser = Invoke-RestMethod -Uri "http://localhost:5000/api/v1.0/users" `
  -Method POST `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $newUser

$createdUser | ConvertTo-Json -Depth 10
```

**Expected:** `201 Created` with user object

#### Disable User

```powershell
$userId = $createdUser.id  # From previous step

$statusUpdate = @{
  isActive = $false
} | ConvertTo-Json

$updatedUser = Invoke-RestMethod -Uri "http://localhost:5000/api/v1.0/users/$userId/status" `
  -Method PATCH `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $statusUpdate

$updatedUser | ConvertTo-Json -Depth 10
```

**Expected:** User with `isActive: false`

#### Reset Password

```powershell
$passwordReset = @{
  newPassword = "NewSecurePass456!"
} | ConvertTo-Json

$resetResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/v1.0/users/$userId/reset-password" `
  -Method POST `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $passwordReset

$resetResponse | ConvertTo-Json
```

**Expected:** `{ "message": "Password reset successfully" }`

## Verification Checklist

- [ ] Database is running and accessible
- [ ] Migrations are applied (including NormalizedUsername/NormalizedEmail indexes)
- [ ] Environment variables are set correctly
- [ ] Application starts without errors
- [ ] Seeding log message appears (success or skip)
- [ ] Can login with seeded credentials
- [ ] Login returns JWT token with Owner role
- [ ] Can list users via GET /api/v1.0/users
- [ ] Can create new user via POST /api/v1.0/users
- [ ] Can disable user via PATCH /api/v1.0/users/{id}/status
- [ ] Can reset password via POST /api/v1.0/users/{id}/reset-password
- [ ] Disabled user cannot login (returns 401)

## Troubleshooting

### Seeding Doesn't Run

1. Check `ASPNETCORE_ENVIRONMENT=Development`
2. Verify all required env vars are set
3. Check application logs for seeding messages

### "User Already Exists"

- This is expected if you've run seeding before
- Either use existing credentials or delete the user from database

### Database Connection Errors

- Verify PostgreSQL is running: `docker ps`
- Check connection string format
- Ensure database exists: `docker exec -it <container> psql -U postgres -l`

### Login Fails After Seeding

1. Verify user was created: Check database or list users endpoint
2. Check password matches what you set in `SEED_ADMIN_PASSWORD`
3. Verify user `IsActive` is `true` in database

## Next Steps

After verifying Step 1 is complete:
1. ✅ Admin user can be created without SQL scripts
2. ✅ All user management endpoints work
3. ✅ Authorization is enforced (Owner-only for mutations)
4. ✅ Ready to proceed to next development step

