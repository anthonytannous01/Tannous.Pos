using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Infrastructure.Services.OperationalDiagnosticsProjections;

internal sealed class OperationalDiagnosticsCacheSurvivabilityProjectionCollaborator
{
    private readonly OperationalDiagnosticsCacheProjectionContextFactory _contextFactory;
    private readonly ILogger _logger;

    public OperationalDiagnosticsCacheSurvivabilityProjectionCollaborator(
        OperationalDiagnosticsCacheProjectionContextFactory contextFactory,
        ILogger logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public OperationalCacheScopeDiagnosticsDto GetScopeDiagnostics()
    {
        var context = _contextFactory.BuildFullContext();
        var scopeDiagnostics = OperationalCacheScopeSurvivabilityBuilder.Build(
            context.Entries,
            context.Telemetry);

        _logger.LogInformation(
            "Operational cache survivability: scope diagnostics queried. ScopedKeys={ScopedKeys}, ScopeChurnRatio={ScopeChurnRatio}",
            scopeDiagnostics.ActiveScopedKeyCount,
            scopeDiagnostics.ScopeChurnRatio);

        return scopeDiagnostics;
    }

    public OperationalCacheSurvivabilityDto GetSurvivability()
    {
        var survivability = _contextFactory.BuildFullContext().Survivability;

        _logger.LogInformation(
            "Operational cache survivability scoring: survivability queried. Score={Score}, Classification={Classification}",
            survivability.SurvivabilityScore,
            survivability.ClassificationLabel);

        return survivability;
    }
}
