using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;
using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalWorkbench;
using Tannous.Pos.Application.Sync;

namespace Tannous.Pos.Infrastructure.Services.OperationalComposition;

/// <summary>
/// Request-scoped operational read composition hub. Memoizes upstream summaries sequentially;
/// composes lazy operator views via aggregation helpers without nested workbench service recursion.
/// </summary>
public sealed class OperationalReadCompositionHub : IOperationalReadCompositionHub
{
    private readonly IOperationalResilienceDiagnosticsService _resilience;
    private readonly ISyncConflictReconciliationService _reconciliation;
    private readonly IOperationalIncidentCorrelationService _incidents;
    private readonly IOperationalAlertSignalService _alertSignals;
    private readonly IOperationalDiagnosticsCacheDiagnosticsService _cacheDiagnostics;
    private readonly IOperationalDiagnosticsCacheTelemetry _telemetry;
    private readonly ILogger<OperationalReadCompositionHub> _logger;

    private OperationalResilienceSummaryDto? _resilienceSummary;
    private ReconciliationSummaryDto? _reconciliationSummary;
    private OperationalIncidentSummaryDto? _incidentsSummary;
    private OperationalAlertSummaryDto? _alertsSummary;
    private OperationalCacheGovernanceOverviewDto? _governanceOverview;
    private OperationalGovernanceRuntimeProtectionDto? _runtimeProtection;
    private OperationalGovernanceFingerprintDto? _fingerprint;
    private OperationalDashboardSummaryDto? _dashboardSummary;
    private OperationalReconciliationWorkbenchDto? _reconciliationWorkbenchView;
    private OperationalInventoryWorkbenchDto? _inventoryWorkbenchView;

    public OperationalReadCompositionHub(
        IOperationalResilienceDiagnosticsService resilience,
        ISyncConflictReconciliationService reconciliation,
        IOperationalIncidentCorrelationService incidents,
        IOperationalAlertSignalService alertSignals,
        IOperationalDiagnosticsCacheDiagnosticsService cacheDiagnostics,
        IOperationalDiagnosticsCacheTelemetry telemetry,
        ILogger<OperationalReadCompositionHub> logger)
    {
        _resilience = resilience;
        _reconciliation = reconciliation;
        _incidents = incidents;
        _alertSignals = alertSignals;
        _cacheDiagnostics = cacheDiagnostics;
        _telemetry = telemetry;
        _logger = logger;
        Context = new OperationalReadCompositionContext();
    }

    public OperationalReadCompositionContext Context { get; }

    public Task<OperationalResilienceSummaryDto> GetResilienceSummaryAsync(CancellationToken cancellationToken = default) =>
        GetOrLoadAsync(
            _resilienceSummary,
            () => _resilience.GetSummaryAsync(cancellationToken),
            value => _resilienceSummary = value);

    public Task<ReconciliationSummaryDto> GetReconciliationSummaryAsync(CancellationToken cancellationToken = default) =>
        GetOrLoadAsync(
            _reconciliationSummary,
            () => _reconciliation.GetSummaryAsync(cancellationToken),
            value => _reconciliationSummary = value);

    public Task<OperationalIncidentSummaryDto> GetIncidentsSummaryAsync(CancellationToken cancellationToken = default) =>
        GetOrLoadAsync(
            _incidentsSummary,
            () => _incidents.GetSummaryAsync(cancellationToken),
            value => _incidentsSummary = value);

    public Task<OperationalAlertSummaryDto> GetAlertSummaryAsync(CancellationToken cancellationToken = default) =>
        GetOrLoadAsync(
            _alertsSummary,
            () => _alertSignals.GetAlertSummaryAsync(cancellationToken),
            value => _alertsSummary = value);

    public async Task<OperationalCacheGovernanceOverviewDto> GetGovernanceOverviewAsync(
        CancellationToken cancellationToken = default)
    {
        if (_governanceOverview is null)
        {
            RecordReuseMiss();
            _governanceOverview = await _cacheDiagnostics
                .GetGovernanceOverviewAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            RecordReuseHit();
        }

        return EnrichGovernanceOverview(_governanceOverview);
    }

    public async Task<OperationalGovernanceRuntimeProtectionSnapshot> GetRuntimeProtectionSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        var runtime = await EnsureRuntimeProtectionAsync(cancellationToken).ConfigureAwait(false);
        return MapRuntimeProtectionSnapshot(runtime);
    }

    public async Task<OperationalGovernanceFingerprintSnapshot> GetFingerprintSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        var fingerprint = await EnsureFingerprintAsync(cancellationToken).ConfigureAwait(false);
        return MapFingerprintSnapshot(fingerprint);
    }

    public async Task<OperationalDashboardSummaryDto> GetDashboardSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        if (_dashboardSummary is not null)
        {
            RecordReuseHit();
            return _dashboardSummary;
        }

        RecordReuseMiss();
        var resilience = await GetResilienceSummaryAsync(cancellationToken).ConfigureAwait(false);
        var reconciliation = await GetReconciliationSummaryAsync(cancellationToken).ConfigureAwait(false);
        var incidents = await GetIncidentsSummaryAsync(cancellationToken).ConfigureAwait(false);
        var alerts = await GetAlertSummaryAsync(cancellationToken).ConfigureAwait(false);
        var governanceOverview = await GetGovernanceOverviewAsync(cancellationToken).ConfigureAwait(false);
        var runtimeProtection = await EnsureRuntimeProtectionAsync(cancellationToken).ConfigureAwait(false);
        var fingerprint = await EnsureFingerprintAsync(cancellationToken).ConfigureAwait(false);

        var health = OperationalDashboardAggregation.ComposeHealth(
            resilience,
            alerts,
            incidents,
            runtimeProtection,
            fingerprint);
        var risk = OperationalDashboardAggregation.ComposeRisk(resilience, reconciliation, alerts, incidents);
        var pressure = OperationalDashboardAggregation.ComposePressure(resilience, runtimeProtection, governanceOverview);
        var activity = OperationalDashboardAggregation.ComposeActivity(reconciliation, alerts, incidents);
        var activeConcerns = OperationalDashboardAggregation.ComposeActiveConcerns(
            resilience,
            reconciliation,
            alerts,
            incidents,
            runtimeProtection,
            fingerprint);
        var recommendations = OperationalDashboardAggregation.ComposeRecommendations(
            resilience,
            reconciliation,
            alerts,
            governanceOverview,
            runtimeProtection);

        _dashboardSummary = new OperationalDashboardSummaryDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            Health = health,
            Risk = risk,
            Pressure = pressure,
            Activity = activity,
            ActiveConcerns = activeConcerns,
            Recommendations = recommendations,
            ReadinessSummary = OperationalDashboardAggregation.ComposeReadinessSummary(
                runtimeProtection.ProductionReadiness),
            FingerprintStabilitySummary = OperationalDashboardAggregation.ComposeFingerprintStabilitySummary(fingerprint)
        };

        return _dashboardSummary;
    }

    public async Task<OperationalReconciliationWorkbenchDto> GetReconciliationWorkbenchViewAsync(
        CancellationToken cancellationToken = default)
    {
        if (_reconciliationWorkbenchView is not null)
        {
            RecordReuseHit();
            RecordNestedReadAvoidance();
            return _reconciliationWorkbenchView;
        }

        RecordReuseMiss();
        RecordNestedReadAvoidance();

        var reconciliation = await GetReconciliationSummaryAsync(cancellationToken).ConfigureAwait(false);
        var incidents = await GetIncidentsSummaryAsync(cancellationToken).ConfigureAwait(false);
        var alerts = await GetAlertSummaryAsync(cancellationToken).ConfigureAwait(false);
        var resilience = await GetResilienceSummaryAsync(cancellationToken).ConfigureAwait(false);
        var governanceOverview = await GetGovernanceOverviewAsync(cancellationToken).ConfigureAwait(false);
        var dashboard = await GetDashboardSummaryAsync(cancellationToken).ConfigureAwait(false);

        var queue = OperationalReconciliationWorkbenchAggregation.ComposeQueue(reconciliation, incidents);
        var hotspots = OperationalReconciliationWorkbenchAggregation.ComposeHotspots(
            reconciliation,
            alerts,
            resilience,
            governanceOverview,
            dashboard);
        var replayRisk = OperationalReconciliationWorkbenchAggregation.ComposeReplayRisk(
            resilience,
            reconciliation,
            incidents,
            dashboard);
        var inventoryDrift = OperationalReconciliationWorkbenchAggregation.ComposeInventoryDrift(reconciliation, alerts);
        var attentionItems = OperationalReconciliationWorkbenchAggregation.ComposeAttentionItems(
            reconciliation,
            alerts,
            incidents,
            resilience,
            governanceOverview,
            dashboard);

        _reconciliationWorkbenchView = new OperationalReconciliationWorkbenchDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            Queue = queue,
            Hotspots = hotspots,
            ReplayRisk = replayRisk,
            InventoryDrift = inventoryDrift,
            AttentionItems = attentionItems
        };

        return _reconciliationWorkbenchView;
    }

    public async Task<OperationalInventoryWorkbenchDto> GetInventoryWorkbenchViewAsync(
        CancellationToken cancellationToken = default)
    {
        if (_inventoryWorkbenchView is not null)
        {
            RecordReuseHit();
            RecordNestedReadAvoidance();
            return _inventoryWorkbenchView;
        }

        RecordReuseMiss();
        RecordNestedReadAvoidance();

        var reconciliation = await GetReconciliationSummaryAsync(cancellationToken).ConfigureAwait(false);
        var incidents = await GetIncidentsSummaryAsync(cancellationToken).ConfigureAwait(false);
        var alerts = await GetAlertSummaryAsync(cancellationToken).ConfigureAwait(false);
        var dashboard = await GetDashboardSummaryAsync(cancellationToken).ConfigureAwait(false);
        var reconciliationWorkbench = await GetReconciliationWorkbenchViewAsync(cancellationToken).ConfigureAwait(false);
        var governanceOverview = await GetGovernanceOverviewAsync(cancellationToken).ConfigureAwait(false);
        var resilience = await GetResilienceSummaryAsync(cancellationToken).ConfigureAwait(false);

        var driftSummary = OperationalInventoryWorkbenchAggregation.ComposeDriftSummary(
            reconciliation,
            alerts,
            resilience,
            incidents,
            dashboard,
            reconciliationWorkbench);
        var hotspots = OperationalInventoryWorkbenchAggregation.ComposeHotspots(
            reconciliation,
            alerts,
            resilience,
            incidents,
            governanceOverview,
            dashboard,
            reconciliationWorkbench);
        var resolutionReadiness = OperationalInventoryWorkbenchAggregation.ComposeResolutionReadiness(
            reconciliation,
            alerts,
            resilience,
            dashboard,
            reconciliationWorkbench);
        var mismatchCategories = OperationalInventoryWorkbenchAggregation.ComposeMismatchCategories(
            reconciliation,
            alerts,
            resilience,
            incidents,
            governanceOverview);
        var attentionItems = OperationalInventoryWorkbenchAggregation.ComposeAttentionItems(
            reconciliation,
            alerts,
            resilience,
            incidents,
            governanceOverview,
            dashboard,
            reconciliationWorkbench);

        _inventoryWorkbenchView = new OperationalInventoryWorkbenchDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            DriftSummary = driftSummary,
            Hotspots = hotspots,
            ResolutionReadiness = resolutionReadiness,
            MismatchCategories = mismatchCategories,
            AttentionItems = attentionItems
        };

        return _inventoryWorkbenchView;
    }

    public async Task<OperationalReadCompositionSnapshot> BuildSnapshotAsync(CancellationToken cancellationToken = default)
    {
        Context.RecordSnapshotBuild();
        _telemetry.RecordCompositionSnapshotBuild();

        var resilience = await GetResilienceSummaryAsync(cancellationToken).ConfigureAwait(false);
        var reconciliation = await GetReconciliationSummaryAsync(cancellationToken).ConfigureAwait(false);
        var incidents = await GetIncidentsSummaryAsync(cancellationToken).ConfigureAwait(false);
        var alerts = await GetAlertSummaryAsync(cancellationToken).ConfigureAwait(false);
        var governanceOverview = await GetGovernanceOverviewAsync(cancellationToken).ConfigureAwait(false);
        var runtimeProtection = await GetRuntimeProtectionSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var fingerprint = await GetFingerprintSnapshotAsync(cancellationToken).ConfigureAwait(false);

        return new OperationalReadCompositionSnapshot
        {
            GeneratedAtUtc = DateTime.UtcNow,
            Resilience = resilience,
            Reconciliation = reconciliation,
            Incidents = incidents,
            Alerts = alerts,
            GovernanceOverview = governanceOverview,
            RuntimeProtection = runtimeProtection,
            Fingerprint = fingerprint,
            Dashboard = _dashboardSummary,
            ReconciliationWorkbench = _reconciliationWorkbenchView,
            InventoryWorkbench = _inventoryWorkbenchView,
            CompositionContext = Context
        };
    }

    private async Task<OperationalGovernanceRuntimeProtectionDto> EnsureRuntimeProtectionAsync(
        CancellationToken cancellationToken)
    {
        if (_runtimeProtection is not null)
        {
            RecordReuseHit();
            return _runtimeProtection;
        }

        RecordReuseMiss();
        _runtimeProtection = await _cacheDiagnostics.GetRuntimeProtectionAsync(cancellationToken).ConfigureAwait(false);
        return _runtimeProtection;
    }

    private async Task<OperationalGovernanceFingerprintDto> EnsureFingerprintAsync(CancellationToken cancellationToken)
    {
        if (_fingerprint is not null)
        {
            RecordReuseHit();
            return _fingerprint;
        }

        RecordReuseMiss();
        _fingerprint = await _cacheDiagnostics.GetGovernanceFingerprintAsync(cancellationToken).ConfigureAwait(false);
        return _fingerprint;
    }

    private OperationalCacheGovernanceOverviewDto EnrichGovernanceOverview(OperationalCacheGovernanceOverviewDto overview)
    {
        var ratio = Context.CompositionReuseRatio;
        if (Math.Abs(overview.CompositionReuseRatio - ratio) < 0.0001
            && overview.NestedCompositionAvoidance == Context.CompositionNestedReadAvoidanceCount)
        {
            return overview;
        }

        return new OperationalCacheGovernanceOverviewDto
        {
            GeneratedAtUtc = overview.GeneratedAtUtc,
            ReadinessState = overview.ReadinessState,
            PressureSeverity = overview.PressureSeverity,
            DegradationState = overview.DegradationState,
            CardinalityClassification = overview.CardinalityClassification,
            DominantTtlMode = overview.DominantTtlMode,
            StabilityScore = overview.StabilityScore,
            StabilityClassification = overview.StabilityClassification,
            HitRatio = overview.HitRatio,
            TotalHits = overview.TotalHits,
            TotalMisses = overview.TotalMisses,
            TotalBypasses = overview.TotalBypasses,
            TotalInvalidations = overview.TotalInvalidations,
            ActiveEntryCount = overview.ActiveEntryCount,
            ActiveScopedKeyCount = overview.ActiveScopedKeyCount,
            WarmCandidateCount = overview.WarmCandidateCount,
            WarmRecommendationsSuppressed = overview.WarmRecommendationsSuppressed,
            AgingEntryCount = overview.AgingEntryCount,
            NearExpiryEntryCount = overview.NearExpiryEntryCount,
            ExpiredEntryCount = overview.ExpiredEntryCount,
            Cardinality = overview.Cardinality,
            ScopeDiagnostics = overview.ScopeDiagnostics,
            Degradation = overview.Degradation,
            GovernanceNote = overview.GovernanceNote,
            ReasonCodes = overview.ReasonCodes,
            TriggerSignals = overview.TriggerSignals,
            GovernanceNotes = overview.GovernanceNotes,
            RecommendedActions = overview.RecommendedActions,
            ProductionReadiness = overview.ProductionReadiness,
            CompositionReuseRatio = ratio,
            NestedCompositionAvoidance = Context.CompositionNestedReadAvoidanceCount
        };
    }

    private static OperationalGovernanceRuntimeProtectionSnapshot MapRuntimeProtectionSnapshot(
        OperationalGovernanceRuntimeProtectionDto runtime) =>
        new()
        {
            FailsafeActive = runtime.Failsafe.FailsafeActive,
            TelemetrySaturationLevel = runtime.TelemetrySaturationLevel,
            ReadinessState = runtime.ProductionReadiness.ReadinessState
        };

    private static OperationalGovernanceFingerprintSnapshot MapFingerprintSnapshot(
        OperationalGovernanceFingerprintDto fingerprint) =>
        new()
        {
            FingerprintChanged = fingerprint.FingerprintChanged,
            HasPreviousFingerprint = fingerprint.HasPreviousFingerprint,
            FingerprintHash = fingerprint.FingerprintHash,
            FingerprintStability = fingerprint.FingerprintStability
        };

    private async Task<T> GetOrLoadAsync<T>(
        T? cached,
        Func<Task<T>> loader,
        Action<T> assign)
        where T : class
    {
        if (cached is not null)
        {
            RecordReuseHit();
            return cached;
        }

        RecordReuseMiss();
        var value = await loader().ConfigureAwait(false);
        assign(value);
        return value;
    }

    private void RecordReuseHit()
    {
        Context.RecordReuseHit();
        _telemetry.RecordCompositionReuseHit();
    }

    private void RecordReuseMiss()
    {
        Context.RecordReuseMiss();
        _telemetry.RecordCompositionReuseMiss();
    }

    private void RecordNestedReadAvoidance()
    {
        Context.RecordNestedReadAvoidance();
        _telemetry.RecordCompositionNestedReadAvoidance();
    }
}
