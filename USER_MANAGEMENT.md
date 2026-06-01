# User Management API Documentation

## Overview

The User Management API allows administrators (Owner role) and managers to create, list, enable/disable, and reset passwords for users in the Tannous POS system.

## Authorization

- **Owner**: Full access to all user management endpoints
- **Manager**: Can list and view users (read-only)
- **Other roles**: No access

## Endpoints

### POST /api/v1.0/users

Create a new user. **Owner only.**

**Request Body:**
```json
{
  "username": "cashier1",
  "email": "cashier1@tannouspos.com",
  "password": "SecurePass123",
  "firstName": "John",
  "lastName": "Doe",
  "role": "Cashier"
}
```

**Validation Rules:**
- Username: 3-50 characters, alphanumeric + dots/underscores/hyphens only
- Email: Valid email format (optional)
- Password: Minimum 8 characters, must contain uppercase, lowercase, and number
- FirstName/LastName: Required, max 100 characters
- Role: Must be one of: Owner, Manager, Cashier, Kitchen, Waiter

**Response:** `201 Created`
```json
{
  "id": "guid",
  "username": "cashier1",
  "email": "cashier1@tannouspos.com",
  "firstName": "John",
  "lastName": "Doe",
  "role": "Cashier",
  "isActive": true,
  "lastLoginDate": null,
  "createdAt": "2025-01-02T10:00:00Z",
  "updatedAt": null
}
```

**Error Responses:**
- `400 Bad Request`: Validation errors
- `409 Conflict`: Username or email already exists
- `401 Unauthorized`: Not authenticated
- `403 Forbidden`: Not Owner role

### GET /api/v1.0/users

List users with pagination and search. **Owner and Manager.**

**Query Parameters:**
- `page` (int, default: 1): Page number
- `pageSize` (int, default: 20, max: 100): Items per page
- `search` (string, optional): Search term (searches username, email, first name, last name)

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "username": "cashier1",
      "email": "cashier1@tannouspos.com",
      "firstName": "John",
      "lastName": "Doe",
      "role": "Cashier",
      "isActive": true,
      "lastLoginDate": "2025-01-01T08:00:00Z",
      "createdAt": "2025-01-01T00:00:00Z",
      "updatedAt": "2025-01-01T08:00:00Z"
    }
  ],
  "total": 10,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

### GET /api/v1.0/users/{id}

Get a specific user by ID. **Owner and Manager.**

**Response:** `200 OK` (same structure as user in list)

**Error Responses:**
- `404 Not Found`: User not found

### PATCH /api/v1.0/users/{id}/status

Enable or disable a user. **Owner only.**

**Request Body:**
```json
{
  "isActive": false
}
```

**Response:** `200 OK` (updated user object)

**Error Responses:**
- `404 Not Found`: User not found

### POST /api/v1.0/users/{id}/reset-password

Reset a user's password. **Owner only.**

**Request Body:**
```json
{
  "newPassword": "NewSecurePass123"
}
```

**Response:** `200 OK`
```json
{
  "message": "Password reset successfully"
}
```

**Error Responses:**
- `400 Bad Request`: Password validation failed
- `404 Not Found`: User not found

## Testing with cURL

### 1. Create a User (Owner only)

```bash
curl -X POST http://localhost:5000/api/v1.0/users \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "cashier1",
    "email": "cashier1@tannouspos.com",
    "password": "SecurePass123",
    "firstName": "John",
    "lastName": "Doe",
    "role": "Cashier"
  }'
```

### 2. List Users

```bash
curl -X GET "http://localhost:5000/api/v1.0/users?page=1&pageSize=20&search=cashier" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

### 3. Get User by ID

```bash
curl -X GET http://localhost:5000/api/v1.0/users/{user-id} \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

### 4. Disable User

```bash
curl -X PATCH http://localhost:5000/api/v1.0/users/{user-id}/status \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "isActive": false
  }'
```

### 5. Reset Password

```bash
curl -X POST http://localhost:5000/api/v1.0/users/{user-id}/reset-password \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "newPassword": "NewSecurePass123"
  }'
```

## Database Migration

After deploying, run the migration to add normalized fields:

```bash
cd Tannous.Pos.Infrastructure
dotnet ef database update --startup-project ../Tannous.Pos.WebApi
```

This migration will:
- Add `NormalizedUsername` and `NormalizedEmail` fields
- Populate them from existing data
- Create unique indexes on normalized fields
- Remove old indexes on non-normalized fields

## Security Notes

1. **Password Requirements**: Minimum 8 characters with uppercase, lowercase, and number
2. **Username Uniqueness**: Case-insensitive (e.g., "Admin" and "admin" are considered duplicates)
3. **Email Uniqueness**: Case-insensitive, optional
4. **Inactive Users**: Cannot log in (checked during authentication)
5. **Role-Based Access**: Only Owner can create/modify users; Manager can view

## Normalized Fields

The system uses normalized (uppercase) versions of username and email for:
- Case-insensitive uniqueness checks
- Faster lookups
- Consistent data integrity

These fields are automatically populated when creating/updating users.


