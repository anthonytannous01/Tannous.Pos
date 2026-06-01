using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;

namespace Tannous.Pos.Infrastructure.Services.OperationalDiagnosticsProjections;

internal sealed class OperationalGovernanceSnapshotProjectionCollaborator
{
    private readonly OperationalDiagnosticsCacheProjectionContextFactory _contextFactory;
    private readonly IOperationalDiagnosticsCacheTelemetry _telemetry;
    private readonly ILogger _logger;

    public OperationalGovernanceSnapshotProjectionCollaborator(
        OperationalDiagnosticsCacheProjectionContextFactory contextFactory,
        IOperationalDiagnosticsCacheTelemetry telemetry,
        ILogger logger)
    {
        _contextFactory = contextFactory;
        _telemetry = telemetry;
        _logger = logger;
    }

    public OperationalGovernanceSnapshotDto GetGovernanceSnapshot()
    {
        var access = _contextFactory.AcquireSnapshot();
        var telemetrySnapshot = _contextFactory.GetTelemetry();
        var reuseLevel = OperationalGovernanceProjectionReuseClassifier.Classify(
            telemetrySnapshot.ProjectionReuseHits,
            telemetrySnapshot.ProjectionReuseMisses);
        var consistencyLevel = ClassifyConsistency(access, reuseLevel);

        var dto = OperationalGovernanceSnapshotBuilder.BuildSnapshotDto(
            access.Composition,
            access.Freshness,
            reuseLevel,
            consistencyLevel);

        _logger.LogInformation(
            "Operational governance snapshot: snapshot queried. SnapshotKey={SnapshotKey}, SnapshotState={SnapshotState}, ReuseLevel={ReuseLevel}, ProjectionCount={ProjectionCount}",
            dto.Metadata.SnapshotKey,
            dto.Metadata.SnapshotState,
            dto.Metadata.ReuseLevel,
            dto.Metadata.ProjectionCount);

        if (access.WasReused)
        {
            _logger.LogInformation(
                "Operational governance snapshot reuse: governance snapshot reused. SnapshotKey={SnapshotKey}, AgeSeconds={AgeSeconds}",
                dto.Metadata.SnapshotKey,
                dto.Metadata.SnapshotAgeSeconds);
        }

        return dto;
    }

    public OperationalGovernanceProjectionReuseDto GetProjectionReuse()
    {
        var access = _contextFactory.AcquireSnapshot();
        var telemetrySnapshot = _contextFactory.GetTelemetry();
        var reuseLevel = OperationalGovernanceProjectionReuseClassifier.Classify(
            telemetrySnapshot.ProjectionReuseHits,
            telemetrySnapshot.ProjectionReuseMisses);
        var dto = OperationalGovernanceSnapshotBuilder.BuildReuseDto(
            telemetrySnapshot,
            access.Freshness,
            reuseLevel);

        _logger.LogInformation(
            "Operational projection reuse: reuse diagnostics queried. ReuseLevel={ReuseLevel}, Hits={Hits}, Misses={Misses}, HitRatio={HitRatio}",
            dto.ReuseLevel,
            dto.ProjectionReuseHits,
            dto.ProjectionReuseMisses,
            dto.ReuseHitRatio);

        return dto;
    }

    public OperationalGovernanceProjectionConsistencyDto GetProjectionConsistency()
    {
        var access = _contextFactory.AcquireSnapshot();
        var telemetrySnapshot = _contextFactory.GetTelemetry();
        var reuseLevel = OperationalGovernanceProjectionReuseClassifier.Classify(
            telemetrySnapshot.ProjectionReuseHits,
            telemetrySnapshot.ProjectionReuseMisses);
        var consistencyLevel = ClassifyConsistency(access, reuseLevel);
        var dto = OperationalGovernanceSnapshotBuilder.BuildConsistencyDto(
            access.Composition,
            access.Freshness,
            reuseLevel,
            consistencyLevel,
            telemetrySnapshot.SnapshotConsistencyTransitions);

        _logger.LogInformation(
            "Operational projection consistency: consistency diagnostics queried. ConsistencyLevel={ConsistencyLevel}, SnapshotState={SnapshotState}, ProjectionCount={ProjectionCount}",
            dto.ConsistencyLevel,
            dto.SnapshotState,
            dto.ProjectionCount);

        return dto;
    }

    private static OperationalGovernanceSnapshotConsistencyLevel ClassifyConsistency(
        OperationalGovernanceSnapshotAccess access,
        OperationalGovernanceProjectionReuseLevel reuseLevel)
    {
        var snapshotState = OperationalGovernanceSnapshotFreshnessClassifier.Classify(
            access.AgeSeconds,
            access.WasReused,
            access.IsExpired);

        return OperationalGovernanceProjectionConsistencyClassifier.Classify(
            snapshotState,
            access.Composition.Context,
            reuseLevel,
            access.Composition.GovernanceConsistency);
    }
}
