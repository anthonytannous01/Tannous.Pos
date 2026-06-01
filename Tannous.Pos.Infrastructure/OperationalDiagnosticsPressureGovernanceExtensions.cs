using Microsoft.Extensions.DependencyInjection;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Infrastructure.Services;

namespace Tannous.Pos.Infrastructure;

public static class OperationalDiagnosticsPressureGovernanceExtensions
{
    /// <summary>Registers governance-only pressure reset coordinator (not exposed via HTTP).</summary>
    public static IServiceCollection AddOperationalDiagnosticsPressureResetCoordinator(this IServiceCollection services)
    {
        services.AddSingleton<IOperationalDiagnosticsPressureResetCoordinator, OperationalDiagnosticsPressureResetCoordinator>();
        return services;
    }
}
