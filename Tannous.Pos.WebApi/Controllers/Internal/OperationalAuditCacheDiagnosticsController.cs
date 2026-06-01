using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;

namespace Tannous.Pos.WebApi.Controllers.Internal;

// GOVERNANCE / INTERNAL: Operational diagnostics cache visibility — GET-only, Admin only, metadata/telemetry only.
// GOVERNANCE / NON-GOAL: no cache values, payloads, invalidation mutations, or distributed cache.
[ApiController]
[Route("api/v{version:apiVersion}/internal/operational-audit/cache")]
[ApiVersion("1.0")]
[Authorize(Policy = "Admin")]
public class OperationalAuditCacheDiagnosticsController : ControllerBase
{
    private readonly IOperationalDiagnosticsCacheDiagnosticsService _cacheDiagnostics;
    private readonly ILogger<OperationalAuditCacheDiagnosticsController> _logger;

    public OperationalAuditCacheDiagnosticsController(
        IOperationalDiagnosticsCacheDiagnosticsService cacheDiagnostics,
        ILogger<OperationalAuditCacheDiagnosticsController> logger)
    {
        _cacheDiagnostics = cacheDiagnostics;
        _logger = logger;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<OperationalDiagnosticsCacheDiagnosticsSummaryDto>> GetSummary(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("Summary");
        return Ok(await _cacheDiagnostics.GetSummaryAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("effectiveness")]
    public async Task<ActionResult<OperationalDiagnosticsCacheDiagnosticsEffectivenessDto>> GetEffectiveness(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("Effectiveness");
        return Ok(await _cacheDiagnostics.GetEffectivenessAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("stale-risk")]
    public async Task<ActionResult<OperationalDiagnosticsCacheDiagnosticsStaleRiskDto>> GetStaleRisk(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("StaleRisk");
        return Ok(await _cacheDiagnostics.GetStaleRiskAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("pressure")]
    public async Task<ActionResult<OperationalDiagnosticsCacheDiagnosticsPressureDto>> GetPressure(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("Pressure");
        return Ok(await _cacheDiagnostics.GetPressureAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("adaptive-summary")]
    public async Task<ActionResult<OperationalCacheAdaptiveSummaryDto>> GetAdaptiveSummary(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("AdaptiveSummary");
        return Ok(await _cacheDiagnostics.GetAdaptiveSummaryAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("warm-candidates")]
    public async Task<ActionResult<OperationalCacheWarmCandidatesDiagnosticsDto>> GetWarmCandidates(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("WarmCandidates");
        return Ok(await _cacheDiagnostics.GetWarmCandidatesAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("stability")]
    public async Task<ActionResult<OperationalCacheStabilityDto>> GetStability(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("Stability");
        return Ok(await _cacheDiagnostics.GetStabilityAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("governance-overview")]
    public async Task<ActionResult<OperationalCacheGovernanceOverviewDto>> GetGovernanceOverview(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("GovernanceOverview");
        return Ok(await _cacheDiagnostics.GetGovernanceOverviewAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("governance-audit")]
    public async Task<ActionResult<OperationalCacheGovernanceAuditDto>> GetGovernanceAudit(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("GovernanceAudit");
        return Ok(await _cacheDiagnostics.GetGovernanceAuditAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("governance-consistency")]
    public async Task<ActionResult<OperationalCacheGovernanceConsistencyDto>> GetGovernanceConsistency(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("GovernanceConsistency");
        return Ok(await _cacheDiagnostics.GetGovernanceConsistencyAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("survivability")]
    public async Task<ActionResult<OperationalCacheSurvivabilityDto>> GetSurvivability(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("Survivability");
        return Ok(await _cacheDiagnostics.GetSurvivabilityAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("invalidation-audit")]
    public async Task<ActionResult<OperationalCacheInvalidationAuditDto>> GetInvalidationAudit(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("InvalidationAudit");
        return Ok(await _cacheDiagnostics.GetInvalidationAuditAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("freshness-recovery")]
    public async Task<ActionResult<OperationalCacheFreshnessRecoveryDto>> GetFreshnessRecovery(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("FreshnessRecovery");
        return Ok(await _cacheDiagnostics.GetFreshnessRecoveryAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("invalidation-consistency")]
    public async Task<ActionResult<OperationalCacheInvalidationConsistencyDto>> GetInvalidationConsistency(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("InvalidationConsistency");
        return Ok(await _cacheDiagnostics.GetInvalidationConsistencyAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("invalidation-pressure")]
    public async Task<ActionResult<OperationalCacheInvalidationPressureDto>> GetInvalidationPressure(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("InvalidationPressure");
        return Ok(await _cacheDiagnostics.GetInvalidationPressureAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("consistency-recovery")]
    public async Task<ActionResult<OperationalCacheConsistencyRecoveryDto>> GetConsistencyRecovery(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("ConsistencyRecovery");
        return Ok(await _cacheDiagnostics.GetConsistencyRecoveryAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("containment-audit")]
    public async Task<ActionResult<OperationalCacheContainmentAuditDto>> GetContainmentAudit(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("ContainmentAudit");
        return Ok(await _cacheDiagnostics.GetContainmentAuditAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("propagation-diagnostics")]
    public async Task<ActionResult<OperationalCachePropagationDiagnosticsDto>> GetPropagationDiagnostics(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("PropagationDiagnostics");
        return Ok(await _cacheDiagnostics.GetPropagationDiagnosticsAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("consistency-confidence")]
    public async Task<ActionResult<OperationalCacheConsistencyConfidenceDto>> GetConsistencyConfidence(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("ConsistencyConfidence");
        return Ok(await _cacheDiagnostics.GetConsistencyConfidenceAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("pressure-lifecycle")]
    public async Task<ActionResult<OperationalPressureLifecycleDto>> GetPressureLifecycle(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("PressureLifecycle");
        return Ok(await _cacheDiagnostics.GetPressureLifecycleAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("pressure-recovery")]
    public async Task<ActionResult<OperationalPressureRecoveryDto>> GetPressureRecovery(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("PressureRecovery");
        return Ok(await _cacheDiagnostics.GetPressureRecoveryAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("pressure-convergence")]
    public async Task<ActionResult<OperationalPressureConvergenceDto>> GetPressureConvergence(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("PressureConvergence");
        return Ok(await _cacheDiagnostics.GetPressureConvergenceAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("runtime-protection")]
    public async Task<ActionResult<OperationalGovernanceRuntimeProtectionDto>> GetRuntimeProtection(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("RuntimeProtection");
        return Ok(await _cacheDiagnostics.GetRuntimeProtectionAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("execution-diagnostics")]
    public async Task<ActionResult<OperationalGovernanceExecutionDiagnosticsDto>> GetExecutionDiagnostics(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("ExecutionDiagnostics");
        return Ok(await _cacheDiagnostics.GetExecutionDiagnosticsAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("telemetry-saturation")]
    public async Task<ActionResult<OperationalGovernanceTelemetrySaturationDto>> GetTelemetrySaturation(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("TelemetrySaturation");
        return Ok(await _cacheDiagnostics.GetTelemetrySaturationAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("governance-snapshot")]
    public async Task<ActionResult<OperationalGovernanceSnapshotDto>> GetGovernanceSnapshot(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("GovernanceSnapshot");
        return Ok(await _cacheDiagnostics.GetGovernanceSnapshotAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("projection-reuse")]
    public async Task<ActionResult<OperationalGovernanceProjectionReuseDto>> GetProjectionReuse(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("ProjectionReuse");
        return Ok(await _cacheDiagnostics.GetProjectionReuseAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("projection-consistency")]
    public async Task<ActionResult<OperationalGovernanceProjectionConsistencyDto>> GetProjectionConsistency(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("ProjectionConsistency");
        return Ok(await _cacheDiagnostics.GetProjectionConsistencyAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("governance-fingerprint")]
    public async Task<ActionResult<OperationalGovernanceFingerprintDto>> GetGovernanceFingerprint(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("GovernanceFingerprint");
        return Ok(await _cacheDiagnostics.GetGovernanceFingerprintAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("governance-drift-analysis")]
    public async Task<ActionResult<OperationalGovernanceDriftAnalysisDto>> GetGovernanceDriftAnalysis(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("GovernanceDriftAnalysis");
        return Ok(await _cacheDiagnostics.GetGovernanceDriftAnalysisAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("replay-consistency")]
    public async Task<ActionResult<OperationalGovernanceReplayConsistencyDto>> GetReplayConsistency(
        CancellationToken cancellationToken = default)
    {
        LogAuthorization("ReplayConsistency");
        return Ok(await _cacheDiagnostics.GetReplayConsistencyAsync(cancellationToken).ConfigureAwait(false));
    }

    private void LogAuthorization(string scope)
    {
        _logger.LogInformation(
            "Operational cache diagnostics: diagnostics authorization path. Policy={Policy}, Scope={Scope}, User={User}",
            "Admin",
            scope,
            User.Identity?.Name ?? "unknown");
    }
}
