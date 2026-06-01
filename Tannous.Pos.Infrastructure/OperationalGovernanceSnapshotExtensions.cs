using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Infrastructure.Services.OperationalDiagnosticsProjections;

namespace Tannous.Pos.Infrastructure;

public static class OperationalGovernanceSnapshotExtensions
{
    /// <summary>Registers process-local governance snapshot reuse store (not business cache).</summary>
    public static IServiceCollection AddOperationalGovernanceSnapshotReuse(this IServiceCollection services)
    {
        services.AddSingleton<OperationalGovernanceFingerprintHistoryStore>();
        services.AddSingleton(sp => new Lazy<IOperationalDiagnosticsCache>(
            () => sp.GetRequiredService<IOperationalDiagnosticsCache>()));
        services.AddSingleton<OperationalGovernanceSnapshotStore>();
        return services;
    }
}
