using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Infrastructure.Services.OperationalDiagnosticsProjections;

internal sealed class OperationalDiagnosticsCacheConsistencyProjectionCollaborator
{
    private readonly OperationalDiagnosticsCacheProjectionContextFactory _contextFactory;
    private readonly ILogger _logger;

    public OperationalDiagnosticsCacheConsistencyProjectionCollaborator(
        OperationalDiagnosticsCacheProjectionContextFactory contextFactory,
        ILogger logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public OperationalCacheConsistencyRecoveryDto GetConsistencyRecovery()
    {
        var context = _contextFactory.BuildFullContext();
        var recovery = OperationalCacheConsistencyProjectionBuilder.BuildRecovery(
            context.Entries,
            context.Telemetry,
            context.StaleRisk,
            context.Overview,
            context.Stability,
            context.Survivability,
            context.PressureSignals);

        _logger.LogInformation(
            "Operational consistency recovery: consistency recovery queried. Containment={Containment}, Confidence={Confidence}, Cycles={Cycles}",
            recovery.ContainmentState,
            recovery.ConfidenceLevel,
            recovery.ConsistencyRecoveryCycles);

        foreach (var recommendation in recovery.Recommendations.Take(3))
        {
            _logger.LogInformation(
                "Operational recovery stabilization: recommendation emitted. Code={Code}, Priority={Priority}",
                recommendation.Code,
                recommendation.Priority);
        }

        return recovery;
    }

    public OperationalCacheContainmentAuditDto GetContainmentAudit()
    {
        var context = _contextFactory.BuildFullContext();
        var audit = OperationalCacheConsistencyProjectionBuilder.BuildContainmentAudit(
            context.Entries,
            context.Telemetry,
            context.StaleRisk,
            context.Overview,
            context.Stability);

        _logger.LogInformation(
            "Operational containment governance: containment audit queried. State={State}, Propagation={Propagation}, Escalations={Escalations}",
            audit.ContainmentState,
            audit.PropagationSeverity,
            audit.ContainmentEscalations);

        return audit;
    }

    public OperationalCachePropagationDiagnosticsDto GetPropagationDiagnostics()
    {
        var context = _contextFactory.BuildFullContext();
        var propagation = OperationalCacheConsistencyProjectionBuilder.BuildPropagationDiagnostics(
            context.Entries,
            context.Telemetry);

        _logger.LogInformation(
            "Operational propagation visibility: propagation diagnostics queried. Severity={Severity}, Detections={Detections}, CrossCategory={CrossCategory}",
            propagation.PropagationSeverity,
            propagation.PropagationDetections,
            propagation.CrossCategoryInvalidations);

        return propagation;
    }

    public OperationalCacheConsistencyConfidenceDto GetConsistencyConfidence()
    {
        var context = _contextFactory.BuildFullContext();
        var confidence = OperationalCacheConsistencyProjectionBuilder.BuildConfidence(
            context.Entries,
            context.Telemetry,
            context.StaleRisk,
            context.Overview,
            context.Stability,
            context.Survivability);

        _logger.LogInformation(
            "Operational consistency confidence: consistency confidence queried. Level={Level}, Score={Score}, Drops={Drops}",
            confidence.ConfidenceLevel,
            confidence.ConfidenceScore,
            confidence.ConsistencyConfidenceDrops);

        return confidence;
    }
}
