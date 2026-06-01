using Tannous.Pos.Application.Audit.Governance;

namespace Tannous.Pos.Application.Audit;

/// <summary>Read-only operational diagnostics cache introspection (metadata and telemetry only).</summary>
public interface IOperationalDiagnosticsCacheDiagnosticsService
{
    Task<OperationalDiagnosticsCacheDiagnosticsSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<OperationalDiagnosticsCacheDiagnosticsPressureDto> GetPressureAsync(CancellationToken cancellationToken = default);
    Task<OperationalDiagnosticsCacheDiagnosticsStaleRiskDto> GetStaleRiskAsync(CancellationToken cancellationToken = default);
    Task<OperationalDiagnosticsCacheDiagnosticsEffectivenessDto> GetEffectivenessAsync(CancellationToken cancellationToken = default);
    Task<OperationalCacheAdaptiveSummaryDto> GetAdaptiveSummaryAsync(CancellationToken cancellationToken = default);
    Task<OperationalCacheWarmCandidatesDiagnosticsDto> GetWarmCandidatesAsync(CancellationToken cancellationToken = default);
    Task<OperationalCacheStabilityDto> GetStabilityAsync(CancellationToken cancellationToken = default);
    Task<OperationalCacheGovernanceOverviewDto> GetGovernanceOverviewAsync(CancellationToken cancellationToken = default);
    Task<OperationalCacheCardinalitySnapshotDto> GetCardinalitySnapshotAsync(CancellationToken cancellationToken = default);
    Task<OperationalCacheScopeDiagnosticsDto> GetScopeDiagnosticsAsync(CancellationToken cancellationToken = default);
    Task<OperationalCacheDegradationDto> GetDegradationAsync(CancellationToken cancellationToken = default);
    Task<OperationalCacheGovernanceAuditDto> GetGovernanceAuditAsync(CancellationToken cancellationToken = default);
    Task<OperationalCacheGovernanceConsistencyDto> GetGovernanceConsistencyAsync(CancellationToken cancellationToken = default);
    Task<OperationalCacheSurvivabilityDto> GetSurvivabilityAsync(CancellationToken cancellationToken = default);
    Task<OperationalCacheInvalidationAuditDto> GetInvalidationAuditAsync(CancellationToken cancellationToken = default);
    Task<OperationalCacheFreshnessRecoveryDto> GetFreshnessRecoveryAsync(CancellationToken cancellationToken = default);
    Task<OperationalCacheInvalidationConsistencyDto> GetInvalidationConsistencyAsync(CancellationToken cancellationToken = default);
    Task<OperationalCacheInvalidationPressureDto> GetInvalidationPressureAsync(CancellationToken cancellationToken = default);
    Task<OperationalCacheConsistencyRecoveryDto> GetConsistencyRecoveryAsync(CancellationToken cancellationToken = default);
    Task<OperationalCacheContainmentAuditDto> GetContainmentAuditAsync(CancellationToken cancellationToken = default);
    Task<OperationalCachePropagationDiagnosticsDto> GetPropagationDiagnosticsAsync(CancellationToken cancellationToken = default);
    Task<OperationalCacheConsistencyConfidenceDto> GetConsistencyConfidenceAsync(CancellationToken cancellationToken = default);
    Task<OperationalPressureLifecycleDto> GetPressureLifecycleAsync(CancellationToken cancellationToken = default);
    Task<OperationalPressureRecoveryDto> GetPressureRecoveryAsync(CancellationToken cancellationToken = default);
    Task<OperationalPressureConvergenceDto> GetPressureConvergenceAsync(CancellationToken cancellationToken = default);
    Task<OperationalGovernanceRuntimeProtectionDto> GetRuntimeProtectionAsync(CancellationToken cancellationToken = default);
    Task<OperationalGovernanceExecutionDiagnosticsDto> GetExecutionDiagnosticsAsync(CancellationToken cancellationToken = default);
    Task<OperationalGovernanceTelemetrySaturationDto> GetTelemetrySaturationAsync(CancellationToken cancellationToken = default);
    Task<OperationalGovernanceSnapshotDto> GetGovernanceSnapshotAsync(CancellationToken cancellationToken = default);
    Task<OperationalGovernanceProjectionReuseDto> GetProjectionReuseAsync(CancellationToken cancellationToken = default);
    Task<OperationalGovernanceProjectionConsistencyDto> GetProjectionConsistencyAsync(CancellationToken cancellationToken = default);
    Task<OperationalGovernanceFingerprintDto> GetGovernanceFingerprintAsync(CancellationToken cancellationToken = default);
    Task<OperationalGovernanceDriftAnalysisDto> GetGovernanceDriftAnalysisAsync(CancellationToken cancellationToken = default);
    Task<OperationalGovernanceReplayConsistencyDto> GetReplayConsistencyAsync(CancellationToken cancellationToken = default);
}
