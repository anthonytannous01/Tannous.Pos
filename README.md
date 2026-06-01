# Tannous POS Backend

A modern Point of Sale (POS) system built with .NET 8, following Clean Architecture principles. This backend provides a comprehensive API for restaurant management, including order processing, inventory tracking, customer management, and reporting.

## Features

### Core Functionality
- **Order Management**: Create, finalize, and void orders with support for add-ons and customizations
- **Inventory Tracking**: Real-time stock management with automatic cost calculations
- **Customer Management**: Customer profiles with order history and preferences
- **Shift Management**: Cash drawer tracking with opening/closing procedures
- **Reporting**: End-of-day reports, COGS analysis, and CSV exports
- **Printing**: Receipt and kitchen ticket rendering with customizable templates

### Advanced Features
- **Sync API**: Offline-capable synchronization for mobile POS devices
- **Security**: JWT authentication, role-based authorization, rate limiting
- **Idempotency**: Safe retry mechanisms for all mutation operations
- **Device Management**: Multi-device support with device validation
- **Health Monitoring**: Database connectivity and migration status checks
- **Structured Logging**: JSON logging with Serilog and request enrichment
- **API Versioning**: Versioned API endpoints with backward compatibility
- **Production Hardening**: Database indexes, constraints, and performance optimizations

## Technology Stack

- **.NET 8**: Latest LTS version with performance improvements
- **Entity Framework Core**: ORM with PostgreSQL support
- **PostgreSQL**: Robust relational database
- **MediatR**: CQRS pattern implementation
- **FluentValidation**: Request validation
- **AutoMapper**: Object mapping
- **JWT**: Token-based authentication
- **Docker**: Containerized deployment
- **Serilog**: Structured logging with JSON output
- **Testcontainers**: Integration testing with real databases
- **GitHub Actions**: CI/CD pipeline with automated testing

## Architecture

The solution follows Clean Architecture principles with clear separation of concerns:

```
Tannous.Pos/
├── Domain/           # Entities, interfaces, domain logic
├── Application/      # Use cases, DTOs, CQRS handlers
├── Infrastructure/   # Data access, external services
└── WebApi/          # API controllers, configuration
```

## Quick Start

### Prerequisites
- .NET 8 SDK
- Docker Desktop (for local PostgreSQL)
- EF Core tools: `dotnet tool install --global dotnet-ef --version 8.0.0`

### Configuration
Before running, create a `.env` file from `.env.example` and configure your environment variables:
```powershell
Copy-Item .env.example .env
# Edit .env with your values (see DEPLOYMENT.md for details)
```

### One-Command Development Setup

**Windows (PowerShell):**
```powershell
.\scripts\dev-up.ps1
```

This script will:
1. ✅ Check Docker is running
2. 📦 Start PostgreSQL database container
3. ⏳ Wait for database to be healthy
4. 🗄️ Run EF Core migrations
5. 🌐 Start the API server

**Access the API:**
- Swagger UI: http://localhost:8080
- Health Check: http://localhost:8080/health/ready

**For detailed setup instructions, see [DEPLOYMENT.md](DEPLOYMENT.md)**

**For database-specific instructions, see [DATABASE_SETUP.md](DATABASE_SETUP.md)**

### Manual Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd Tannous.Pos
   ```

2. **Set up the database**
   ```bash
   # Update connection string in appsettings.json
   # Run migrations
   cd Tannous.Pos.Infrastructure
   dotnet ef database update --startup-project ../Tannous.Pos.WebApi
   ```

3. **Run the application**
   ```bash
   cd Tannous.Pos.WebApi
   dotnet run
   ```

4. **Access the API**
   - Swagger UI: http://localhost:8080
   - Health Check: http://localhost:8080/health/ready

### Docker Deployment

1. **Build and run with Docker Compose**
   ```bash
   docker-compose up --build
   ```

2. **Access the application**
   - API: http://localhost:5000
   - Swagger UI: http://localhost:5000/swagger

## API Endpoints

### Authentication
- `POST /api/auth/login` - User authentication
- `POST /api/auth/refresh` - Token refresh

### Orders
- `POST /api/orders` - Create new order
- `GET /api/orders` - List orders
- `GET /api/orders/{id}` - Get order details
- `POST /api/orders/{id}/finalize` - Finalize order
- `POST /api/orders/{id}/void` - Void order

### Inventory
- `GET /api/inventory` - List inventory items
- `GET /api/inventory/low-stock` - Low stock alerts
- `GET /api/inventory/ingredients` - List ingredients
- `POST /api/inventory/ingredients` - Create ingredient

### Reports
- `GET /api/reports/eod` - End-of-day report
- `GET /api/reports/cogs` - Cost of goods sold
- `GET /api/reports/export/eod.csv` - CSV export

### Printing
- `GET /api/print/receipt-template` - Get receipt template
- `POST /api/print/receipt/render` - Render receipt
- `POST /api/print/kitchen/render` - Render kitchen ticket

### Sync
- `GET /api/sync/pull` - Pull data updates
- `POST /api/sync/push` - Push local changes

## Configuration

### Environment Variables
```bash
# Database
ConnectionStrings__DefaultConnection=Host=localhost;Database=tannous_pos;Username=postgres;Password=password

# JWT
Jwt__Key=your-super-secret-key-with-at-least-32-characters
Jwt__Issuer=TannousPOS
Jwt__Audience=TannousPOS
Jwt__ExpiryInMinutes=480

# CORS
Cors__AllowedOrigins__0=http://localhost:3000
Cors__AllowedOrigins__1=https://tannous-pos.com
```

### Rate Limiting
- **Auth endpoints**: 5 requests per minute per IP
- **Mutation endpoints**: 60 requests per minute per Device-Id

## Security

### Headers Required
- `Authorization`: Bearer token for authentication
- `Device-Id`: Device identifier for POS terminals
- `Idempotency-Key`: Unique key for safe retries

### Authorization Policies
- `Owner`: Full system access
- `Cashier`: Order and shift operations
- `CashierOrOwner`: Read access + cashier operations

## Development

### Adding New Features
1. Define entities in `Domain/Entities/`
2. Create DTOs in `Application/DTOs/`
3. Implement CQRS handlers in `Application/`
4. Add repository methods in `Infrastructure/Repositories/`
5. Create controllers in `WebApi/Controllers/`

### Database Migrations
```bash
# Create migration
dotnet ef migrations add MigrationName --project Tannous.Pos.Infrastructure --startup-project Tannous.Pos.WebApi

# Update database
dotnet ef database update --project Tannous.Pos.Infrastructure --startup-project Tannous.Pos.WebApi
```

### Testing
```bash
# Run all tests
.\scripts\test.ps1

# Or manually:
dotnet test tests/Tannous.Pos.Integration/
dotnet test
```

### OpenAPI Generation
```bash
# Generate OpenAPI specification
.\scripts\openapi.ps1

# Output: artifacts/openapi.json
```

## Sample Data

The development environment includes sample data:
- **Users**: owner/password123, cashier/password123
- **Categories**: Appetizers, Main Courses, Desserts
- **Menu Items**: Hummus, Falafel, Shawarma, Kebab, Baklava, Kunafa
- **Add-ons**: Extra Sauce, Extra Meat, Cheese, Vegetables
- **Ingredients**: Chickpeas, Chicken Breast, Beef, Flour, Olive Oil

## Monitoring

### Health Checks
- `/health/live` - Application liveness
- `/health/ready` - Database connectivity and migrations

### Logging
Structured logging with Serilog includes:
- Request/response logging
- Performance metrics
- Error tracking
- Device and user context

## Admin & Operations

### Database Management
- **Stats**: `GET /api/v1.0/admin/db/stats` - Database statistics and row counts
- **Maintenance**: `POST /api/v1.0/admin/db/vacuum-analyze` - Log maintenance requests

### Receipt Reconciliation
- **Reconcile**: `GET /api/v1.0/admin/receipts/reconcile` - Fix missing receipt numbers
- **Reprint**: `POST /api/v1.0/admin/orders/{id}/reprint` - Reprint receipts with official numbers

### Data Cleanup
- **Purge**: `POST /api/v1.0/admin/purge?days=30` - Remove soft-deleted records older than N days

## API Features

### Pagination
All list endpoints support standardized pagination:
```bash
GET /api/v1.0/customers?page=1&pageSize=20&q=john&sort=name&dir=asc
```

**Parameters:**
- `page`: Page number (default: 1)
- `pageSize`: Items per page (default: 20, max: 100)
- `q`: Search query
- `sort`: Sort field (name, email, phone, createdAt, updatedAt)
- `dir`: Sort direction (asc, desc)

### Response Caching
Static master data supports ETag caching:
```bash
GET /api/v1.0/catalog/menu-items
# Returns ETag header

GET /api/v1.0/catalog/menu-items
If-None-Match: "abc123..."
# Returns 304 Not Modified if unchanged
```

### Sync Conflict Resolution
Customer and catalog entities support version-based conflict detection:
```json
{
  "conflict": true,
  "serverEntity": {
    "id": "guid",
    "firstName": "Current Server Value",
    "version": "base64-encoded-version"
  }
}
```

## Troubleshooting

### Common Issues

**Port Already in Use**
```bash
# Check what's using the port
netstat -ano | findstr :8080

# Kill the process
taskkill /PID <process-id> /F
```

**Database Connection Issues**
```bash
# Check if PostgreSQL is running
docker ps | grep postgres

# Restart the container
docker-compose restart postgres
```

**Migration Errors**
```bash
# Remove and recreate database
docker-compose down -v
docker-compose up -d postgres
dotnet ef database update
```

### SSL Certificate Issues (Development)
```bash
# Trust the development certificate
dotnet dev-certs https --trust
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For support and questions:
- Create an issue in the repository
- Contact the development team
- Check the documentation in `/docs`
