using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Tannous.Pos.Infrastructure.Data;

/// <summary>
/// Design-time factory for EF Core migrations.
/// Loads connection string from (in priority order):
/// 1. DB_CONNECTION_STRING environment variable (highest priority)
/// 2. User secrets (from WebApi project if available)
/// 3. appsettings.Development.json (from WebApi project)
/// 4. appsettings.json (from WebApi project)
/// </summary>
public class PosDbContextFactory : IDesignTimeDbContextFactory<PosDbContext>
{
    public PosDbContext CreateDbContext(string[] args)
    {
        // Determine the WebApi project directory (where appsettings.json lives)
        // When running from Infrastructure, WebApi is one level up
        var currentDir = Directory.GetCurrentDirectory();
        var webApiPath = Path.Combine(currentDir, "..", "Tannous.Pos.WebApi");
        
        // If not found, try alternative path (when running from solution root)
        if (!Directory.Exists(webApiPath))
        {
            webApiPath = Path.Combine(currentDir, "Tannous.Pos.WebApi");
        }
        
        // If still not found, use current directory (when running from WebApi)
        if (!Directory.Exists(webApiPath))
        {
            webApiPath = currentDir;
        }

        // Build configuration with priority: Environment > User Secrets > appsettings files
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(webApiPath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();

        // Try to add user secrets from WebApi project
        // Note: User secrets require the project to be built, so this is optional
        try
        {
            // User secrets are stored per-project, try to load from WebApi project
            // The user secrets ID is typically the project's assembly name or a GUID
            // For simplicity, we'll try the common pattern
            var userSecretsId = "Tannous.Pos.WebApi";
            var userSecretsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".microsoft",
                "usersecrets",
                userSecretsId,
                "secrets.json"
            );
            
            if (File.Exists(userSecretsPath))
            {
                configurationBuilder.AddJsonFile(userSecretsPath, optional: true, reloadOnChange: false);
            }
        }
        catch
        {
            // User secrets not available, continue without them
        }

        var configuration = configurationBuilder.Build();

        // Priority 1: Environment variable (highest priority)
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

        // Priority 2: User secrets
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = configuration["ConnectionStrings:Default"] 
                            ?? configuration["DB_CONNECTION_STRING"];
        }

        // Priority 3: appsettings files (already loaded above)
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = configuration.GetConnectionString("Default");
        }

        // Fail with clear error if no connection string found
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string not found. " +
                "Please set one of the following:\n" +
                "  1. Environment variable: DB_CONNECTION_STRING (highest priority)\n" +
                "  2. User secret: ConnectionStrings:Default or DB_CONNECTION_STRING\n" +
                "  3. appsettings.Development.json: ConnectionStrings:Default\n" +
                "  4. appsettings.json: ConnectionStrings:Default\n\n" +
                "PowerShell example:\n" +
                "  $env:DB_CONNECTION_STRING = 'Host=localhost;Database=TannousPOS;Username=postgres;Password=postgres'\n\n" +
                "Or using docker-compose (default credentials):\n" +
                "  $env:DB_CONNECTION_STRING = 'Host=localhost;Port=5432;Database=TannousPOS;Username=postgres;Password=postgres'"
            );
        }

        // Create options builder
        var optionsBuilder = new DbContextOptionsBuilder<PosDbContext>();
        
        optionsBuilder.UseNpgsql(connectionString, npg =>
        {
            // Ensure migrations assembly points to Infrastructure
            npg.MigrationsAssembly(typeof(PosDbContext).Assembly.FullName);
            npg.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
        });

        // Enable detailed errors and sensitive data logging for development
        optionsBuilder.EnableDetailedErrors();
        optionsBuilder.EnableSensitiveDataLogging();

        return new PosDbContext(optionsBuilder.Options);
    }
}

