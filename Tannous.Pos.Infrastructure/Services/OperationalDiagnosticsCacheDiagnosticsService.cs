using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;
using Tannous.Pos.Infrastructure.Services.OperationalDiagnosticsProjections;

namespace Tannous.Pos.Infrastructure.Services;

/// <summary>
/// Read-only operational diagnostics cache introspection (metadata and telemetry only).
/// GOVERNANCE: no cached values, payloads, or envelope bodies exposed.
/// </summary>
public sealed class OperationalDiagnosticsCacheDiagnosticsService : IOperationalDiagnosticsCacheDiagnosticsService
{
    private readonly OperationalDiagnosticsCacheGovernanceProjectionCollaborator _governance;
    private readonly OperationalDiagnosticsCacheSurvivabilityProjectionCollaborator _survivability;
    private readonly OperationalDiagnosticsCacheInvalidationProjectionCollaborator _invalidation;
    private readonly OperationalDiagnosticsCacheConsistencyProjectionCollaborator _consistency;
    private readonly OperationalDiagnosticsCachePressureProjectionCollaborator _pressure;
    private readonly OperationalDiagnosticsCacheRuntimeProtectionProjectionCollaborator _runtimeProtection;
    private readonly OperationalGovernanceSnapshotProjectionCollaborator _snapshot;
    private readonly OperationalGovernanceFingerprintProjectionCollaborator _fingerprint;

    public OperationalDiagnosticsCacheDiagnosticsService(
        OperationalGovernanceSnapshotStore snapshotStore,
        OperationalGovernanceFingerprintHistoryStore fingerprintHistory,
        IOperationalDiagnosticsCache cache,
        IOperationalDiagnosticsCacheTelemetry telemetry,
        IOperationalResiliencePressureState pressureState,
        IOperationalPressureLifecycleTracker pressureLifecycle,
        ILogger<OperationalDiagnosticsCacheDiagnosticsService> logger)
    {
        var memoizer = new OperationalGovernanceProjectionMemoizer();
        var contextFactory = new OperationalDiagnosticsCacheProjectionContextFactory(
            snapshotStore,
            memoizer,
            cache,
            telemetry,
            pressureState);

        _governance = new OperationalDiagnosticsCacheGovernanceProjectionCollaborator(contextFactory, logger);
        _survivability = new OperationalDiagnosticsCacheSurvivabilityProjectionCollaborator(contextFactory, logger);
        _invalidation = new OperationalDiagnosticsCacheInvalidationProjectionCollaborator(contextFactory, logger);
        _consistency = new OperationalDiagnosticsCacheConsistencyProjectionCollaborator(contextFactory, logger);
        _pressure = new OperationalDiagnosticsCachePressureProjectionCollaborator(
            contextFactory,
            pressureLifecycle,
            logger);
        _runtimeProtection = new OperationalDiagnosticsCacheRuntimeProtectionProjectionCollaborator(
            contextFactory,
            telemetry,
            logger);
        _snapshot = new OperationalGovernanceSnapshotProjectionCollaborator(contextFactory, telemetry, logger);
        _fingerprint = new OperationalGovernanceFingerprintProjectionCollaborator(
            contextFactory,
            fingerprintHistory,
            telemetry,
            logger);
    }

    public Task<OperationalDiagnosticsCacheDiagnosticsSummaryDto> GetSummaryAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_governance.GetSummary());

    public Task<OperationalDiagnosticsCacheDiagnosticsPressureDto> GetPressureAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_governance.GetPressure());

    public Task<OperationalDiagnosticsCacheDiagnosticsStaleRiskDto> GetStaleRiskAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_governance.GetStaleRisk());

    public Task<OperationalDiagnosticsCacheDiagnosticsEffectivenessDto> GetEffectivenessAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_governance.GetEffectiveness());

    public Task<OperationalCacheAdaptiveSummaryDto> GetAdaptiveSummaryAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_governance.GetAdaptiveSummary());

    public Task<OperationalCacheWarmCandidatesDiagnosticsDto> GetWarmCandidatesAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_governance.GetWarmCandidates());

    public Task<OperationalCacheStabilityDto> GetStabilityAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_governance.GetStability());

    public Task<OperationalCacheGovernanceOverviewDto> GetGovernanceOverviewAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_governance.GetGovernanceOverview());

    public Task<OperationalCacheCardinalitySnapshotDto> GetCardinalitySnapshotAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_governance.GetCardinalitySnapshot());

    public Task<OperationalCacheScopeDiagnosticsDto> GetScopeDiagnosticsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_survivability.GetScopeDiagnostics());

    public Task<OperationalCacheDegradationDto> GetDegradationAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_governance.GetDegradation());

    public Task<OperationalCacheGovernanceAuditDto> GetGovernanceAuditAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_governance.GetGovernanceAudit());

    public Task<OperationalCacheGovernanceConsistencyDto> GetGovernanceConsistencyAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_governance.GetGovernanceConsistency());

    public Task<OperationalCacheSurvivabilityDto> GetSurvivabilityAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_survivability.GetSurvivability());

    public Task<OperationalCacheInvalidationAuditDto> GetInvalidationAuditAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_invalidation.GetInvalidationAudit());

    public Task<OperationalCacheFreshnessRecoveryDto> GetFreshnessRecoveryAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_invalidation.GetFreshnessRecovery());

    public Task<OperationalCacheInvalidationConsistencyDto> GetInvalidationConsistencyAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_invalidation.GetInvalidationConsistency());

    public Task<OperationalCacheInvalidationPressureDto> GetInvalidationPressureAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_invalidation.GetInvalidationPressure());

    public Task<OperationalCacheConsistencyRecoveryDto> GetConsistencyRecoveryAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_consistency.GetConsistencyRecovery());

    public Task<OperationalCacheContainmentAuditDto> GetContainmentAuditAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_consistency.GetContainmentAudit());

    public Task<OperationalCachePropagationDiagnosticsDto> GetPropagationDiagnosticsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_consistency.GetPropagationDiagnostics());

    public Task<OperationalCacheConsistencyConfidenceDto> GetConsistencyConfidenceAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_consistency.GetConsistencyConfidence());

    public Task<OperationalPressureLifecycleDto> GetPressureLifecycleAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_pressure.GetPressureLifecycle());

    public Task<OperationalPressureRecoveryDto> GetPressureRecoveryAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_pressure.GetPressureRecovery());

    public Task<OperationalPressureConvergenceDto> GetPressureConvergenceAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_pressure.GetPressureConvergence());

    public Task<OperationalGovernanceRuntimeProtectionDto> GetRuntimeProtectionAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_runtimeProtection.GetRuntimeProtection());

    public Task<OperationalGovernanceExecutionDiagnosticsDto> GetExecutionDiagnosticsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_runtimeProtection.GetExecutionDiagnostics());

    public Task<OperationalGovernanceTelemetrySaturationDto> GetTelemetrySaturationAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_runtimeProtection.GetTelemetrySaturation());

    public Task<OperationalGovernanceSnapshotDto> GetGovernanceSnapshotAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_snapshot.GetGovernanceSnapshot());

    public Task<OperationalGovernanceProjectionReuseDto> GetProjectionReuseAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_snapshot.GetProjectionReuse());

    public Task<OperationalGovernanceProjectionConsistencyDto> GetProjectionConsistencyAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_snapshot.GetProjectionConsistency());

    public Task<OperationalGovernanceFingerprintDto> GetGovernanceFingerprintAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_fingerprint.GetGovernanceFingerprint());

    public Task<OperationalGovernanceDriftAnalysisDto> GetGovernanceDriftAnalysisAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_fingerprint.GetGovernanceDriftAnalysis());

    public Task<OperationalGovernanceReplayConsistencyDto> GetReplayConsistencyAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_fingerprint.GetReplayConsistency());
}
