using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Integration.Infrastructure;

/// <summary>WebApplicationFactory configured for isolated PostgreSQL integration tests.</summary>
internal sealed class TannousPosIntegrationWebApplicationFactory : WebApplicationFactory<Program>
{
    internal const string IntegrationJwtKey = "TannousPosIntegrationTestSigningKeyMinimum32Bytes!";

    private readonly string _connectionString;

    public TannousPosIntegrationWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("JWT_KEY", IntegrationJwtKey);

        builder.UseEnvironment("Development");
        builder.UseSetting(WebHostDefaults.EnvironmentKey, "Development");
        builder.UseSetting("Seed:RunOnceOnStartup", "false");
        builder.UseSetting("Jwt:Key", IntegrationJwtKey);
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
        // Integration suites issue many auth/mutation calls per class; avoid 503 from default AuthBurst limits.
        builder.UseSetting("RateLimiting:DisableForIntegration", "true");

        builder.ConfigureServices(services =>
        {
            var descriptors = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<PosDbContext>) ||
                    d.ServiceType == typeof(PosDbContext))
                .ToList();

            foreach (var descriptor in descriptors)
                services.Remove(descriptor);

            services.AddSingleton<ByteaRowVersionSaveInterceptor>();
            services.AddDbContext<PosDbContext>((serviceProvider, options) =>
            {
                options.UseNpgsql(
                    _connectionString,
                    npgsql => npgsql.MigrationsAssembly(typeof(PosDbContext).Assembly.FullName));
                options.AddInterceptors(serviceProvider.GetRequiredService<ByteaRowVersionSaveInterceptor>());
            });

            services.AddScoped<DbContext>(sp => sp.GetRequiredService<PosDbContext>());
        });
    }
}
