# Development Seeding Guide

## Overview

The Tannous POS system includes a safe, development-only seeding mechanism that allows you to bootstrap the first Owner (Admin) user without manual SQL scripts. Seeding only runs in Development environment and requires explicit environment variables.

## Role Naming

**Important:** The system uses "Owner" as the admin role. In the codebase and API:
- "Owner" = Admin role (full system access)
- "Manager" = Manager role (read-only user management, operational access)
- "Cashier", "Kitchen", "Waiter" = Operational roles

When you see "Owner" in the code or API responses, it refers to the administrative role.

## Prerequisites

1. PostgreSQL database is running and accessible
2. Database migrations have been applied
3. Application is running in Development environment (`ASPNETCORE_ENVIRONMENT=Development`)

## Required Environment Variables

The seeding mechanism requires the following environment variables to be set:

| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `SEED_ADMIN_USERNAME` | Yes | Username for the Owner (Admin) user | `admin` |
| `SEED_ADMIN_PASSWORD` | Yes | Password for the Owner (Admin) user | `SecurePass123!` |
| `SEED_ADMIN_FIRSTNAME` | Yes | First name of the Owner user | `Admin` |
| `SEED_ADMIN_LASTNAME` | Yes | Last name of the Owner user | `User` |
| `SEED_ADMIN_EMAIL` | No | Email address (optional) | `admin@tannouspos.com` |

**Note:** You can also use `SEED_OWNER_USERNAME` as an alias for `SEED_ADMIN_USERNAME`.

## Setting Environment Variables

### Windows PowerShell

```powershell
# Set required variables
$env:SEED_ADMIN_USERNAME = "admin"
$env:SEED_ADMIN_PASSWORD = "SecurePass123!"
$env:SEED_ADMIN_FIRSTNAME = "Admin"
$env:SEED_ADMIN_LASTNAME = "User"
$env:SEED_ADMIN_EMAIL = "admin@tannouspos.com"

# Verify they are set
$env:SEED_ADMIN_USERNAME
$env:SEED_ADMIN_PASSWORD
$env:SEED_ADMIN_FIRSTNAME
$env:SEED_ADMIN_LASTNAME
$env:SEED_ADMIN_EMAIL
```

### Windows Command Prompt (CMD)

```cmd
set SEED_ADMIN_USERNAME=admin
set SEED_ADMIN_PASSWORD=SecurePass123!
set SEED_ADMIN_FIRSTNAME=Admin
set SEED_ADMIN_LASTNAME=User
set SEED_ADMIN_EMAIL=admin@tannouspos.com
```

### Linux/macOS (Bash)

```bash
export SEED_ADMIN_USERNAME="admin"
export SEED_ADMIN_PASSWORD="SecurePass123!"
export SEED_ADMIN_FIRSTNAME="Admin"
export SEED_ADMIN_LASTNAME="User"
export SEED_ADMIN_EMAIL="admin@tannouspos.com"
```

### Docker Compose

Add to your `docker-compose.yml`:

```yaml
services:
  api:
    environment:
      - SEED_ADMIN_USERNAME=admin
      - SEED_ADMIN_PASSWORD=SecurePass123!
      - SEED_ADMIN_FIRSTNAME=Admin
      - SEED_ADMIN_LASTNAME=User
      - SEED_ADMIN_EMAIL=admin@tannouspos.com
```

## How Seeding Works

1. **Environment Check:** Seeding only runs when `ASPNETCORE_ENVIRONMENT=Development`
2. **Variable Check:** Seeding only runs if all required environment variables are set
3. **Idempotency:** If an Owner user with the same normalized username already exists, seeding is skipped
4. **Safety:** Seeding never runs in Production, Staging, or any non-Development environment

## Complete Setup Steps

### 1. Start PostgreSQL Database

```powershell
# Using Docker Compose
docker-compose up -d db

# Or using the provided script
.\scripts\start-db.ps1
```

### 2. Apply Database Migrations

```powershell
cd Tannous.Pos.Infrastructure
dotnet ef database update --startup-project ../Tannous.Pos.WebApi
```

### 3. Set Environment Variables

```powershell
$env:SEED_ADMIN_USERNAME = "admin"
$env:SEED_ADMIN_PASSWORD = "SecurePass123!"
$env:SEED_ADMIN_FIRSTNAME = "Admin"
$env:SEED_ADMIN_LASTNAME = "User"
$env:SEED_ADMIN_EMAIL = "admin@tannouspos.com"
```

### 4. Set Required Application Environment Variables

```powershell
$env:JWT_KEY = "your-super-secret-key-with-at-least-32-characters"
$env:DB_CONNECTION_STRING = "Host=localhost;Database=TannousPOS;Username=postgres;Password=password"
```

### 5. Run the Application

```powershell
cd Tannous.Pos.WebApi
dotnet run
```

### 6. Verify Seeding

Check the application logs. You should see one of these messages:

**Success:**
```
Admin user seeded successfully: Username 'admin', Email 'admin@tannouspos.com'
```

**Skipped (user exists):**
```
Admin user seeding skipped: Owner user with username 'admin' already exists.
```

**Skipped (env vars not set):**
```
Admin user seeding skipped: Required environment variables (SEED_ADMIN_USERNAME, SEED_ADMIN_PASSWORD, SEED_ADMIN_FIRSTNAME, SEED_ADMIN_LASTNAME) are not set.
```

## Login After Seeding

Once seeding is complete, you can login using the seeded credentials:

```powershell
curl -X POST http://localhost:5000/api/v1.0/auth/login `
  -H "Content-Type: application/json" `
  -d '{
    "username": "admin",
    "password": "SecurePass123!"
  }'
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

## Troubleshooting

### Seeding Doesn't Run

1. **Check Environment:** Ensure `ASPNETCORE_ENVIRONMENT=Development`
   ```powershell
   $env:ASPNETCORE_ENVIRONMENT
   # Should output: Development
   ```

2. **Check Variables:** Verify all required environment variables are set
   ```powershell
   $env:SEED_ADMIN_USERNAME
   $env:SEED_ADMIN_PASSWORD
   $env:SEED_ADMIN_FIRSTNAME
   $env:SEED_ADMIN_LASTNAME
   ```

3. **Check Logs:** Look for seeding-related log messages in the application output

### "User Already Exists" Error

If you see "Owner user with username 'X' already exists", it means:
- The user was already seeded in a previous run
- You can either:
  - Use the existing user credentials to login
  - Manually delete the user from the database and re-run seeding
  - Use a different username

### Database Connection Errors

Ensure PostgreSQL is running and the connection string is correct:
```powershell
$env:DB_CONNECTION_STRING = "Host=localhost;Database=TannousPOS;Username=postgres;Password=password"
```

### Email Already Registered

If the email address is already in use by another user, seeding will be skipped. Either:
- Use a different email address
- Remove the existing user with that email
- Leave `SEED_ADMIN_EMAIL` unset (email is optional)

## Security Notes

1. **Never commit environment variables** to version control
2. **Never set seeding variables in Production** - seeding is disabled in non-Development environments
3. **Use strong passwords** - the seeded admin user has full system access
4. **Change default passwords** after first login in production-like environments

## Production Deployment

**Important:** Seeding is completely disabled in Production. For production deployments:

1. Create the first Owner user manually via SQL script, or
2. Use a one-time admin setup endpoint (if implemented), or
3. Import users from a trusted source

The seeding mechanism will not run in Production, Staging, or any environment other than Development.

