using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Minimal WebApplicationFactory for governance tests that need the real MVC application model (filter metadata).
/// Registers <see cref="DbContext"/> as a scope alias to <see cref="PosDbContext"/> so full DI validation matches production resolution (handlers use the abstract type).
/// </summary>
public sealed class GovernanceApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(
            "ConnectionStrings:Default",
            "Host=127.0.0.1;Port=65433;Database=governance_scan;Username=g;Password=g;Timeout=3;Maximum Pool Size=1");
        builder.UseEnvironment("Development");
        builder.UseSetting("Seed:RunOnceOnStartup", "false");

        builder.ConfigureTestServices(services =>
        {
            services.AddScoped<DbContext>(sp => sp.GetRequiredService<PosDbContext>());
        });
    }
}
