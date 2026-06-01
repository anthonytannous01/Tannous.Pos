using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;

namespace Tannous.Pos.Infrastructure.Services.OperationalDiagnosticsProjections;

internal sealed class OperationalGovernanceFingerprintProjectionCollaborator
{
    private readonly OperationalDiagnosticsCacheProjectionContextFactory _contextFactory;
    private readonly OperationalGovernanceFingerprintHistoryStore _fingerprintHistory;
    private readonly IOperationalDiagnosticsCacheTelemetry _telemetry;
    private readonly ILogger _logger;

    public OperationalGovernanceFingerprintProjectionCollaborator(
        OperationalDiagnosticsCacheProjectionContextFactory contextFactory,
        OperationalGovernanceFingerprintHistoryStore fingerprintHistory,
        IOperationalDiagnosticsCacheTelemetry telemetry,
        ILogger logger)
    {
        _contextFactory = contextFactory;
        _fingerprintHistory = fingerprintHistory;
        _telemetry = telemetry;
        _logger = logger;
    }

    public OperationalGovernanceFingerprintDto GetGovernanceFingerprint()
    {
        var access = _contextFactory.AcquireSnapshot();
        var comparison = _fingerprintHistory.GetCurrentComparison(access.Composition);
        var stability = Enum.TryParse<OperationalGovernanceFingerprintStability>(
            comparison.FingerprintStability,
            out var parsed)
            ? parsed
            : OperationalGovernanceFingerprintStability.Transitional;

        var dto = OperationalGovernanceFingerprintBuilder.BuildDto(
            access.Composition,
            comparison,
            stability);

        _logger.LogInformation(
            "Operational governance fingerprint: fingerprint queried. SnapshotKey={SnapshotKey}, FingerprintHash={FingerprintHash}, Stability={Stability}, Changed={Changed}",
            dto.SnapshotKey,
            dto.FingerprintHash,
            dto.FingerprintStability,
            dto.FingerprintChanged);

        return dto;
    }

    public OperationalGovernanceDriftAnalysisDto GetGovernanceDriftAnalysis()
    {
        var access = _contextFactory.AcquireSnapshot();
        var comparison = _fingerprintHistory.GetCurrentComparison(access.Composition);
        var telemetrySnapshot = _contextFactory.GetTelemetry();
        var dto = OperationalGovernanceDriftAnalysisBuilder.Build(
            access.Composition,
            comparison,
            telemetrySnapshot);

        _logger.LogInformation(
            "Operational governance drift analysis: drift diagnostics queried. DriftDirection={DriftDirection}, Stability={Stability}, Changed={Changed}, DivergentSegments={DivergentSegments}",
            dto.DriftDirection,
            dto.FingerprintStability,
            dto.FingerprintChanged,
            comparison.DivergentSegmentCount);

        return dto;
    }

    public OperationalGovernanceReplayConsistencyDto GetReplayConsistency()
    {
        var access = _contextFactory.AcquireSnapshot();
        var comparison = _fingerprintHistory.GetCurrentComparison(access.Composition);
        var telemetrySnapshot = _contextFactory.GetTelemetry();
        _telemetry.RecordReplayConsistencyCheck();
        var checksAfter = _telemetry.GetSnapshot().ReplayConsistencyChecks;

        var stability = Enum.TryParse<OperationalGovernanceFingerprintStability>(
            comparison.FingerprintStability,
            out var parsedStability)
            ? parsedStability
            : OperationalGovernanceFingerprintStability.Transitional;

        var fingerprintStable = !comparison.FingerprintChanged
            || comparison.PreviousFingerprintHash == null;

        var replayLevel = OperationalGovernanceReplayConsistencyClassifier.Classify(
            access.WasReused,
            fingerprintStable,
            stability,
            telemetrySnapshot.ProjectionFragmentationSignals);

        var explainability = OperationalGovernanceFingerprintExplainabilityBuilder.Build(
            stability,
            comparison.DriftDirection,
            comparison.FingerprintChanged,
            comparison.PreviousFingerprintHash != null);

        if (replayLevel == OperationalGovernanceReplayConsistencyLevel.Low)
            explainability = OperationalGovernanceRuntimeBudget.ClampExplainabilityOrdered(
                explainability.Concat(new[] { "ReplayConsistencyLow" }),
                OperationalGovernanceRuntimeBudget.MaxExplainabilitySignals);

        var signals = BuildConsistencySignals(replayLevel, access.WasReused, fingerprintStable);

        var dto = new OperationalGovernanceReplayConsistencyDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            SnapshotKey = access.Composition.SnapshotKey,
            FingerprintHash = access.Composition.FingerprintHash,
            ReplayConsistencyLevel = replayLevel.ToString(),
            SnapshotWasReused = access.WasReused,
            FingerprintStableAcrossReuse = access.WasReused && fingerprintStable,
            ReplayConsistencyChecks = checksAfter,
            ProjectionFragmentationSignals = telemetrySnapshot.ProjectionFragmentationSignals,
            ConsistencySignals = signals,
            ExplainabilityCodes = explainability,
            GovernanceNotes = OperationalGovernanceRuntimeBudget.ClampOrdered(new[]
            {
                "Replay consistency is governance determinism only.",
                "Not business replay or reconciliation semantics."
            }, 2)
        };

        _logger.LogInformation(
            "Operational replay consistency: replay consistency queried. Level={Level}, WasReused={WasReused}, FingerprintStable={FingerprintStable}",
            dto.ReplayConsistencyLevel,
            dto.SnapshotWasReused,
            dto.FingerprintStableAcrossReuse);

        if (comparison.FingerprintChanged && comparison.PreviousFingerprintHash != null)
        {
            _logger.LogInformation(
                "Operational governance signature transition: fingerprint transition observed. PreviousHash={PreviousHash}, CurrentHash={CurrentHash}",
                comparison.PreviousFingerprintHash,
                comparison.CurrentFingerprintHash);
        }

        return dto;
    }

    private static IReadOnlyList<string> BuildConsistencySignals(
        OperationalGovernanceReplayConsistencyLevel level,
        bool wasReused,
        bool fingerprintStable)
    {
        var signals = new List<string> { $"ReplayConsistency:{level}" };

        if (wasReused && fingerprintStable)
            signals.Add("FingerprintStableAcrossReuse");

        if (level == OperationalGovernanceReplayConsistencyLevel.Low)
            signals.Add("ReplayConsistencyLow");

        return OperationalGovernanceRuntimeBudget.ClampExplainabilityOrdered(signals, 6);
    }
}
