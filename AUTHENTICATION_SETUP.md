# Authentication Setup Guide

## Overview

The authentication system has been updated to use real JWT-based authentication with refresh tokens. The mock authentication has been removed.

## Environment Variables

The following environment variables must be set:

### Required Variables

- `JWT_KEY`: Secret key for signing JWT tokens (minimum 32 characters, recommended 64+)
- `DB_CONNECTION_STRING`: PostgreSQL connection string

### Optional Variables (with defaults)

- `JWT_ISSUER`: JWT issuer (default: "TannousPOS")
- `JWT_AUDIENCE`: JWT audience (default: "TannousPOS")
- `JWT_ACCESS_TOKEN_EXPIRY_MINUTES`: Access token expiry in minutes (default: 15)
- `JWT_REFRESH_TOKEN_EXPIRY_DAYS`: Refresh token expiry in days (default: 30)

## Local Development Setup

### Option 1: Using .NET User Secrets (Recommended for local dev)

```bash
cd Tannous.Pos.WebApi
dotnet user-secrets set "JWT_KEY" "your-super-secret-key-with-at-least-32-characters"
dotnet user-secrets set "DB_CONNECTION_STRING" "Host=localhost;Database=TannousPOS;Username=postgres;Password=password"
```

### Option 2: Using Environment Variables

**Windows (PowerShell):**
```powershell
$env:JWT_KEY="your-super-secret-key-with-at-least-32-characters"
$env:DB_CONNECTION_STRING="Host=localhost;Database=TannousPOS;Username=postgres;Password=password"
```

**Linux/macOS (Bash):**
```bash
export JWT_KEY="your-super-secret-key-with-at-least-32-characters"
export DB_CONNECTION_STRING="Host=localhost;Database=TannousPOS;Username=postgres;Password=password"
```

### Option 3: Using Docker Compose

Add to `docker-compose.yml`:
```yaml
services:
  api:
    environment:
      - JWT_KEY=your-super-secret-key-with-at-least-32-characters
      - DB_CONNECTION_STRING=Host=db;Database=TannousPOS;Username=postgres;Password=postgres
```

## Database Migration

Run the migration to create the RefreshToken table:

```bash
cd Tannous.Pos.Infrastructure
dotnet ef database update --startup-project ../Tannous.Pos.WebApi
```

## Creating the First Admin User

### Option 1: Development Seeding (Recommended for Local Development)

The application includes a safe, development-only seeding mechanism. See [DEV_SEEDING.md](./DEV_SEEDING.md) for complete instructions.

**Quick Start:**
```powershell
# Set environment variables
$env:SEED_ADMIN_USERNAME = "admin"
$env:SEED_ADMIN_PASSWORD = "SecurePass123!"
$env:SEED_ADMIN_FIRSTNAME = "Admin"
$env:SEED_ADMIN_LASTNAME = "User"
$env:SEED_ADMIN_EMAIL = "admin@tannouspos.com"

# Run the application (seeding happens automatically in Development)
cd Tannous.Pos.WebApi
dotnet run
```

### Option 2: SQL Script (For Production or Manual Setup)

If seeding is not available, create a user manually via SQL:

```sql
INSERT INTO "Users" ("Id", "Username", "NormalizedUsername", "Email", "NormalizedEmail", "PasswordHash", "FirstName", "LastName", "Role", "IsActive", "CreatedAt")
VALUES (
    gen_random_uuid(),
    'admin',
    'ADMIN',  -- Normalized username (uppercase)
    'admin@tannouspos.com',
    'ADMIN@TANNOUSPOS.COM',  -- Normalized email (uppercase, or NULL if email is empty)
    '$2a$11$YourBCryptHashedPasswordHere', -- Use BCrypt to hash password
    'Admin',
    'User',
    1, -- Owner role (1 = Owner, 2 = Manager, 3 = Cashier, 4 = Kitchen, 5 = Waiter)
    true,
    NOW()
);
```

**To hash a password using BCrypt in C#:**
```csharp
var hashedPassword = BCrypt.Net.BCrypt.HashPassword("your-password");
```

**Important Notes:**
- The `NormalizedUsername` field is required and must be the uppercase version of `Username`
- The `NormalizedEmail` field should be the uppercase version of `Email`, or `NULL` if email is empty
- `FirstName` and `LastName` are required fields
- Role values: 1=Owner, 2=Manager, 3=Cashier, 4=Kitchen, 5=Waiter

### Option 3: Via User Management API (After First Admin Exists)

Once you have at least one Owner user, you can create additional users via the User Management API:

```bash
POST /api/v1.0/users
Authorization: Bearer {OwnerAccessToken}
Content-Type: application/json

{
  "username": "newuser",
  "email": "newuser@tannouspos.com",
  "password": "SecurePass123!",
  "firstName": "New",
  "lastName": "User",
  "role": "Cashier"
}
```

## Role Naming

**Note:** The system uses "Owner" as the administrative role. In API responses and code:
- "Owner" = Admin role (full system access)
- "Manager" = Manager role (read-only user management)
- Other roles: Cashier, Kitchen, Waiter

## API Endpoints

### POST /api/v1.0/auth/login

**Request:**
```json
{
  "username": "admin",
  "password": "your-password"
}
```

**Response:**
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

### POST /api/v1.0/auth/refresh

**Request:**
```json
{
  "refreshToken": "base64-encoded-refresh-token"
}
```

**Response:** Same as login response (new access and refresh tokens)

### POST /api/v1.0/auth/logout

**Headers:**
```
Authorization: Bearer {accessToken}
```

**Request:**
```json
{
  "refreshToken": "base64-encoded-refresh-token"
}
```

**Response:**
```json
{
  "message": "Logged out successfully"
}
```

### GET /api/v1.0/auth/profile

**Headers:**
```
Authorization: Bearer {accessToken}
```

**Response:**
```json
{
  "userId": "guid",
  "username": "admin",
  "email": "admin@tannouspos.com",
  "role": "Owner"
}
```

## Testing with cURL

### 1. Login
```bash
curl -X POST http://localhost:5000/api/v1.0/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "your-password"
  }'
```

Save the `accessToken` and `refreshToken` from the response.

### 2. Use Access Token
```bash
curl -X GET http://localhost:5000/api/v1.0/auth/profile \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN_HERE"
```

### 3. Refresh Token
```bash
curl -X POST http://localhost:5000/api/v1.0/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "YOUR_REFRESH_TOKEN_HERE"
  }'
```

### 4. Logout
```bash
curl -X POST http://localhost:5000/api/v1.0/auth/logout \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "YOUR_REFRESH_TOKEN_HERE"
  }'
```

## Security Notes

1. **JWT Key:** Must be at least 32 characters. Use a cryptographically secure random string in production.
2. **Access Tokens:** Short-lived (15 minutes) to minimize exposure if compromised.
3. **Refresh Tokens:** Long-lived (30 days) but revocable. Stored server-side.
4. **Token Rotation:** Refresh tokens are rotated on each refresh (old token revoked, new one issued).
5. **Password Hashing:** Uses BCrypt with automatic salt generation.

## Troubleshooting

### "JWT signing key not configured"
- Ensure `JWT_KEY` environment variable is set
- Check that it's at least 32 characters long

### "Database connection string not configured"
- Ensure `DB_CONNECTION_STRING` environment variable is set
- Verify PostgreSQL is running and accessible

### "Invalid username or password"
- Verify user exists in database
- Check that password hash matches (use BCrypt.Verify)
- Ensure user `IsActive` is true

### Migration errors
- Ensure PostgreSQL is running
- Check connection string is correct
- Verify user has permissions to create tables


