using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Collections.Generic;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.WebApi.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly PosDbContext _context;

    public DatabaseHealthCheck(PosDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test database connection
            await _context.Database.CanConnectAsync(cancellationToken);
            
            // Check if migrations are applied
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
            if (pendingMigrations.Any())
            {
                return HealthCheckResult.Degraded(
                    "Database is accessible but has pending migrations",
                    data: new Dictionary<string, object>
                    {
                        ["pendingMigrationsCount"] = pendingMigrations.Count()
                    });
            }

            // Check if business settings exist (prerequisite for operations)
            var businessSettings = await _context.BusinessSettings.FirstOrDefaultAsync(cancellationToken);
            if (businessSettings == null)
            {
                return HealthCheckResult.Degraded("Database is accessible but business settings are missing");
            }

            // Check if at least one device exists
            var deviceExists = await _context.Devices.AnyAsync(cancellationToken);
            if (!deviceExists)
            {
                return HealthCheckResult.Degraded("Database is accessible but no devices are configured");
            }

            return HealthCheckResult.Healthy("Database is accessible, up to date, and has required data");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is not accessible", ex);
        }
    }
}
