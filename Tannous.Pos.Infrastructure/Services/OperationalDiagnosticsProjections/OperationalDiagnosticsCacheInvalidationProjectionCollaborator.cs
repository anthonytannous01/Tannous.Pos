using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Infrastructure.Services.OperationalDiagnosticsProjections;

internal sealed class OperationalDiagnosticsCacheInvalidationProjectionCollaborator
{
    private readonly OperationalDiagnosticsCacheProjectionContextFactory _contextFactory;
    private readonly ILogger _logger;

    public OperationalDiagnosticsCacheInvalidationProjectionCollaborator(
        OperationalDiagnosticsCacheProjectionContextFactory contextFactory,
        ILogger logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public OperationalCacheInvalidationAuditDto GetInvalidationAudit()
    {
        var context = _contextFactory.BuildFullContext();
        var audit = OperationalCacheInvalidationProjectionBuilder.BuildAudit(
            context.Entries,
            context.Telemetry,
            context.StaleRisk);

        _logger.LogInformation(
            "Operational invalidation governance: invalidation audit queried. Severity={Severity}, Recovery={Recovery}, Drift={Drift}, CrossCategory={CrossCategory}",
            audit.InvalidationSeverity,
            audit.FreshnessRecoveryState,
            audit.InvalidationDriftClassification,
            audit.CrossCategoryInvalidations);

        foreach (var recommendation in audit.Recommendations.Take(3))
        {
            _logger.LogInformation(
                "Operational cache recovery guidance: recommendation emitted. Code={Code}, Priority={Priority}",
                recommendation.Code,
                recommendation.Priority);
        }

        return audit;
    }

    public OperationalCacheFreshnessRecoveryDto GetFreshnessRecovery()
    {
        var context = _contextFactory.BuildFullContext();
        var recovery = OperationalCacheInvalidationProjectionBuilder.BuildFreshnessRecovery(
            context.Telemetry,
            context.StaleRisk,
            context.Entries.Count);

        _logger.LogInformation(
            "Operational freshness recovery: freshness recovery queried. State={State}, Recoveries={Recoveries}, Expired={Expired}",
            recovery.RecoveryState,
            recovery.FreshnessRecoveryCount,
            recovery.ExpiredEntryCount);

        return recovery;
    }

    public OperationalCacheInvalidationConsistencyDto GetInvalidationConsistency()
    {
        var context = _contextFactory.BuildFullContext();
        var consistency = OperationalCacheInvalidationProjectionBuilder.BuildConsistency(
            context.Entries,
            context.Telemetry,
            context.StaleRisk);

        _logger.LogInformation(
            "Operational invalidation drift: invalidation consistency queried. IsConsistent={IsConsistent}, Drift={Drift}, SignalCount={SignalCount}",
            consistency.IsConsistent,
            consistency.InvalidationDriftClassification,
            consistency.InconsistencySignals.Count);

        return consistency;
    }

    public OperationalCacheInvalidationPressureDto GetInvalidationPressure()
    {
        var context = _contextFactory.BuildFullContext();
        var pressure = OperationalCacheInvalidationProjectionBuilder.BuildPressure(
            context.Entries,
            context.Telemetry);

        _logger.LogWarning(
            "Operational invalidation pressure: invalidation pressure queried. Severity={Severity}, TotalInvalidations={TotalInvalidations}, CrossCategory={CrossCategory}, ScopeChurn={ScopeChurn}",
            pressure.InvalidationSeverity,
            pressure.TotalInvalidations,
            pressure.CrossCategoryInvalidations,
            pressure.ScopeChurnRatio);

        return pressure;
    }
}
