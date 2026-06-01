using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;
using Tannous.Pos.Application.OperationalWorkbench;

using Tannous.Pos.Application.OperationalCognition;

namespace Tannous.Pos.Application.OperationalIncidents;

/// <summary>Deterministic operator incident case aggregation from existing operational read models.</summary>
public static class OperationalIncidentAggregation
{
    public const int MaxIncidentCases = 8;
    public const int MaxStoredSnapshots = OperationalCognitionSnapshotLimits.MaxStoredSnapshots;
    public const int MaxSignalsPerCase = 8;

    public const string IncidentReplayInstability = "incident-replay-instability";
    public const string IncidentInventoryDrift = "incident-inventory-drift";
    public const string IncidentReconciliationPressure = "incident-reconciliation-pressure";
    public const string IncidentRuntimeProtection = "incident-runtime-protection";
    public const string IncidentOperationalVolatility = "incident-operational-volatility";

    public const string RouteDashboard = "dashboard";
    public const string RouteReconciliationWorkbench = "workbench/reconciliation";
    public const string RouteInventoryWorkbench = "inventory-workbench/drift";
    public const string RouteReplayWorkbench = "replay-workbench/pressure";
    public const string RouteTrendSummary = "trends/summary";
    public const string RouteTimeline = "timeline";
    public const string RouteTriage = "triage";
    public const string RouteRecovery = "recovery";
    public const string RouteNavigation = "navigation";

    public const string WorkbenchReplay = "Replay Pressure Workbench";
    public const string WorkbenchInventory = "Inventory Drift Workbench";
    public const string WorkbenchReconciliation = "Reconciliation Workbench";
    public const string WorkbenchDashboard = "Operational Dashboard";
    public const string WorkbenchRecovery = "Recovery Outlook";

    public static OperationalIncidentCasesDto ComposeCases(
        OperationalTrendSummaryDto trend,
        OperationalTimelineDto timeline,
        IReadOnlyList<OperationalTimelineCorrelationDto> timelineCorrelations,
        OperationalTriageQueueDto triage,
        OperationalRecoveryPostureDto recovery,
        OperationalDashboardSummaryDto dashboard,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReplayRecoveryConfidenceDto replayRecoveryConfidence,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection,
        OperationalGovernanceFingerprintSnapshot fingerprint,
        bool runtimeSaturationIndicated,
        bool protectiveModeActive,
        IReadOnlyList<OperationalIncidentCaseSnapshot> priorSnapshots,
        DateTime generatedAtUtc)
    {
        var candidates = new List<(int Priority, OperationalIncidentCaseDto Case)>();

        TryAddReplayCase(
            candidates,
            trend,
            timeline,
            timelineCorrelations,
            triage,
            recovery,
            replayPressure,
            replayStabilization,
            replayRecoveryConfidence,
            runtimeSaturationIndicated,
            priorSnapshots,
            generatedAtUtc);

        TryAddInventoryCase(
            candidates,
            trend,
            triage,
            recovery,
            inventoryWorkbench,
            replayStabilization,
            priorSnapshots,
            generatedAtUtc);

        TryAddReconciliationCase(
            candidates,
            triage,
            recovery,
            reconciliationWorkbench,
            replayStabilization,
            priorSnapshots,
            generatedAtUtc);

        TryAddRuntimeCase(
            candidates,
            triage,
            recovery,
            dashboard,
            runtimeProtection,
            runtimeSaturationIndicated,
            protectiveModeActive,
            replayStabilization,
            priorSnapshots,
            generatedAtUtc);

        TryAddOperationalVolatilityCase(
            candidates,
            trend,
            timeline,
            triage,
            recovery,
            fingerprint,
            dashboard,
            priorSnapshots,
            generatedAtUtc);

        var cases = candidates
            .OrderBy(c => c.Priority)
            .ThenBy(c => c.Case.IncidentId, StringComparer.Ordinal)
            .Take(MaxIncidentCases)
            .Select(c => c.Case)
            .ToList();

        return new OperationalIncidentCasesDto
        {
            GeneratedAtUtc = generatedAtUtc,
            CaseCount = cases.Count,
            Cases = cases
        };
    }

    public static OperationalIncidentCasesSummaryDto ComposeSummary(
        IReadOnlyList<OperationalIncidentCaseDto> cases,
        OperationalRecoveryPostureDto recovery,
        DateTime generatedAtUtc)
    {
        var active = cases.Count(c => c.State != OperationalIncidentState.Resolved);
        var escalating = cases.Count(c => c.IsEscalating || c.State == OperationalIncidentState.Escalating);
        var recovering = cases.Count(c => c.State == OperationalIncidentState.Recovering || c.State == OperationalIncidentState.Stabilizing);
        var recurring = cases.Count(c => c.IsRecurring || c.State == OperationalIncidentState.Recurring);
        var highest = cases.Count == 0
            ? OperationalIncidentSeverity.Nominal
            : cases.Max(c => c.Severity);

        return new OperationalIncidentCasesSummaryDto
        {
            GeneratedAtUtc = generatedAtUtc,
            ActiveIncidentCount = active,
            EscalatingIncidentCount = escalating,
            RecoveringIncidentCount = recovering,
            RecurringIncidentCount = recurring,
            HighestSeverity = highest,
            PlatformStabilityState = DescribePlatformStability(recovery),
            OperatorAttentionLevel = DescribeAttentionLevel(highest, escalating, recurring),
            Summary = DescribeSummary(active, escalating, recovering, recurring)
        };
    }

    public static OperationalIncidentCaseDetailDto? ComposeDetails(
        string incidentId,
        IReadOnlyList<OperationalIncidentCaseDto> cases,
        OperationalTrendSummaryDto trend,
        OperationalTimelineDto timeline,
        IReadOnlyList<OperationalTimelineCorrelationDto> timelineCorrelations,
        OperationalTriageQueueDto triage,
        OperationalRecoveryPostureDto recovery,
        OperationalDashboardSummaryDto dashboard,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReplayRecoveryConfidenceDto replayRecoveryConfidence,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection,
        OperationalGovernanceFingerprintSnapshot fingerprint,
        bool runtimeSaturationIndicated,
        bool protectiveModeActive)
    {
        var incidentCase = cases.FirstOrDefault(c =>
            string.Equals(c.IncidentId, incidentId, StringComparison.OrdinalIgnoreCase));

        if (incidentCase is null)
            return null;

        var signals = ComposeSignals(
            incidentCase.IncidentId,
            trend,
            timeline,
            triage,
            recovery,
            replayPressure,
            replayStabilization,
            replayRecoveryConfidence,
            reconciliationWorkbench,
            inventoryWorkbench,
            runtimeProtection,
            fingerprint,
            runtimeSaturationIndicated,
            protectiveModeActive);

        var context = ComposeInvestigationContext(
            incidentCase.IncidentId,
            timeline,
            timelineCorrelations,
            triage,
            recovery,
            replayStabilization,
            replayRecoveryConfidence,
            reconciliationWorkbench,
            inventoryWorkbench,
            fingerprint,
            runtimeSaturationIndicated,
            protectiveModeActive);

        var outlook = ComposeOutlook(incidentCase, recovery, replayStabilization, trend);

        return new OperationalIncidentCaseDetailDto
        {
            Case = incidentCase,
            Signals = signals,
            InvestigationContext = context,
            Outlook = outlook
        };
    }

    public static IReadOnlyList<OperationalIncidentCaseSnapshot> CreateSnapshots(
        IReadOnlyList<OperationalIncidentCaseDto> cases,
        OperationalGovernanceFingerprintSnapshot fingerprint,
        DateTime observedAtUtc) =>
        cases
            .Select(c => new OperationalIncidentCaseSnapshot
            {
                IncidentId = c.IncidentId,
                CategoryKey = ResolveCategoryKey(c.IncidentId),
                Severity = c.Severity,
                RecommendedRoute = c.RecommendedRoute,
                StabilityKey = BuildStabilityKey(fingerprint, c.IncidentId),
                ObservedAtUtc = observedAtUtc
            })
            .OrderBy(s => s.IncidentId, StringComparer.Ordinal)
            .ToList();

    public static bool DetectRecurrence(
        string incidentId,
        string recommendedRoute,
        string categoryKey,
        IReadOnlyList<OperationalIncidentCaseSnapshot> priorSnapshots)
    {
        if (priorSnapshots.Count == 0)
            return false;

        var sameIncident = priorSnapshots.Count(s =>
            string.Equals(s.IncidentId, incidentId, StringComparison.OrdinalIgnoreCase));

        var sameRouteCategory = priorSnapshots.Count(s =>
            string.Equals(s.CategoryKey, categoryKey, StringComparison.OrdinalIgnoreCase)
            && string.Equals(s.RecommendedRoute, recommendedRoute, StringComparison.OrdinalIgnoreCase));

        return sameIncident >= 1 || sameRouteCategory >= 2;
    }

    private static void TryAddReplayCase(
        List<(int Priority, OperationalIncidentCaseDto Case)> candidates,
        OperationalTrendSummaryDto trend,
        OperationalTimelineDto timeline,
        IReadOnlyList<OperationalTimelineCorrelationDto> timelineCorrelations,
        OperationalTriageQueueDto triage,
        OperationalRecoveryPostureDto recovery,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReplayRecoveryConfidenceDto replayRecoveryConfidence,
        bool runtimeSaturationIndicated,
        IReadOnlyList<OperationalIncidentCaseSnapshot> priorSnapshots,
        DateTime generatedAtUtc)
    {
        var replayTriage = triage.Items.Count(i => i.Category == OperationalTriageCategory.ReplayInstability);
        var pressureActive = replayPressure.InstabilityLevel >= OperationalReplayPressureLevel.Elevated
            || replayStabilization.ReplayPressureEscalating
            || replayStabilization.ReplayRecoveryImproving
            || replayStabilization.StabilizationActive
            || replayTriage > 0;

        if (!pressureActive)
            return;

        var severity = MapReplaySeverity(replayPressure.InstabilityLevel);
        var direction = replayStabilization.ReplayRecoveryImproving
            ? OperationalIncidentDirection.Converging
            : replayStabilization.ReplayPressureEscalating
                ? OperationalIncidentDirection.Escalating
                : OperationalIncidentDirection.Stable;

        var isEscalating = replayStabilization.ReplayPressureEscalating
            || replayPressure.InstabilityLevel >= OperationalReplayPressureLevel.High;

        var isRecurring = DetectRecurrence(
            IncidentReplayInstability,
            RouteReplayWorkbench,
            "replay",
            priorSnapshots);

        var state = ClassifyState(isEscalating, replayStabilization.ReplayRecoveryImproving, isRecurring, severity);
        var correlatedAreas = CountCorrelatedAreas(
            replay: true,
            inventory: replayStabilization.ReplayRecoveryImproving && runtimeSaturationIndicated,
            reconciliation: timelineCorrelations.Any(c => c.CorrelationLabel.Contains("Replay", StringComparison.OrdinalIgnoreCase)),
            runtime: runtimeSaturationIndicated,
            operational: trend.OverallDirection == OperationalTrendDirection.Degrading);

        candidates.Add((1, BuildCase(
            IncidentReplayInstability,
            "Replay instability incident",
            DescribeReplaySummary(replayPressure, replayStabilization, replayRecoveryConfidence),
            severity,
            state,
            direction,
            MapReplayConfidence(replayRecoveryConfidence),
            generatedAtUtc,
            MapTriagePriority(triage.OverallPriority, severity),
            DescribeRecoveryAlignment(recovery, "Replay"),
            isRecurring,
            isEscalating,
            CountReplaySignals(replayPressure, replayStabilization, replayTriage),
            correlatedAreas,
            RouteReplayWorkbench,
            WorkbenchReplay,
            DescribeEstimatedStabilization(replayStabilization, recovery),
            "Correlated replay instability — review replay pressure and recovery alignment")));
    }

    private static void TryAddInventoryCase(
        List<(int Priority, OperationalIncidentCaseDto Case)> candidates,
        OperationalTrendSummaryDto trend,
        OperationalTriageQueueDto triage,
        OperationalRecoveryPostureDto recovery,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalReplayStabilizationDto replayStabilization,
        IReadOnlyList<OperationalIncidentCaseSnapshot> priorSnapshots,
        DateTime generatedAtUtc)
    {
        var drift = inventoryWorkbench.DriftSummary;
        if (drift.DriftSeverity < OperationalInventoryDriftSeverity.Elevated
            && drift.EscalatingDriftConflicts == 0
            && drift.UnresolvedDriftConflicts == 0
            && !triage.Items.Any(i => i.Category == OperationalTriageCategory.InventoryDrift))
            return;

        var severity = MapInventorySeverity(drift.DriftSeverity);
        var direction = drift.EscalatingDriftConflicts > 0
            ? OperationalIncidentDirection.Diverging
            : drift.UnresolvedDriftConflicts == 0
                ? OperationalIncidentDirection.Converging
                : OperationalIncidentDirection.Stable;

        var isEscalating = drift.EscalatingDriftConflicts > 0;
        var isRecurring = DetectRecurrence(
            IncidentInventoryDrift,
            RouteInventoryWorkbench,
            "inventory",
            priorSnapshots);

        var state = ClassifyState(isEscalating, drift.UnresolvedDriftConflicts == 0, isRecurring, severity);

        candidates.Add((2, BuildCase(
            IncidentInventoryDrift,
            "Inventory drift incident",
            drift.Summary,
            severity,
            state,
            direction,
            trend.OverallDirection == OperationalTrendDirection.Improving
                ? OperationalIncidentConfidence.Elevated
                : OperationalIncidentConfidence.Moderate,
            generatedAtUtc,
            MapTriagePriority(triage.OverallPriority, severity),
            DescribeRecoveryAlignment(recovery, "Inventory"),
            isRecurring,
            isEscalating,
            CountInventorySignals(drift, replayStabilization),
            CountCorrelatedAreas(replay: drift.ReplayLinkedDriftPressure > 0, inventory: true, reconciliation: false, runtime: false, operational: trend.OverallDirection == OperationalTrendDirection.Degrading),
            RouteInventoryWorkbench,
            WorkbenchInventory,
            isEscalating ? "Stabilization uncertain — monitor drift convergence" : "Inventory conditions may stabilize with continued monitoring",
            "Correlated inventory drift — review drift hotspots and linked replay pressure")));
    }

    private static void TryAddReconciliationCase(
        List<(int Priority, OperationalIncidentCaseDto Case)> candidates,
        OperationalTriageQueueDto triage,
        OperationalRecoveryPostureDto recovery,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalReplayStabilizationDto replayStabilization,
        IReadOnlyList<OperationalIncidentCaseSnapshot> priorSnapshots,
        DateTime generatedAtUtc)
    {
        var queue = reconciliationWorkbench.Queue;
        if (queue.EscalatingConflicts == 0
            && queue.UnresolvedConflicts == 0
            && !reconciliationWorkbench.ReplayRisk.StabilizationRecovering
            && !triage.Items.Any(i => i.Category == OperationalTriageCategory.ReconciliationBacklog))
            return;

        var severity = MapReconciliationSeverity(queue);
        var direction = queue.EscalatingConflicts > 0
            ? OperationalIncidentDirection.Escalating
            : reconciliationWorkbench.ReplayRisk.StabilizationRecovering
                ? OperationalIncidentDirection.Converging
                : OperationalIncidentDirection.Stable;

        var isEscalating = queue.EscalatingConflicts > 0;
        var isRecurring = DetectRecurrence(
            IncidentReconciliationPressure,
            RouteReconciliationWorkbench,
            "reconciliation",
            priorSnapshots);

        var state = ClassifyState(isEscalating, reconciliationWorkbench.ReplayRisk.StabilizationRecovering, isRecurring, severity);

        candidates.Add((3, BuildCase(
            IncidentReconciliationPressure,
            "Reconciliation pressure incident",
            queue.Summary,
            severity,
            state,
            direction,
            OperationalIncidentConfidence.Moderate,
            generatedAtUtc,
            MapTriagePriority(triage.OverallPriority, severity),
            DescribeRecoveryAlignment(recovery, "Reconciliation"),
            isRecurring,
            isEscalating,
            CountReconciliationSignals(queue, reconciliationWorkbench),
            CountCorrelatedAreas(replay: reconciliationWorkbench.ReplayRisk.StabilizationRecovering, inventory: false, reconciliation: true, runtime: false, operational: false),
            RouteReconciliationWorkbench,
            WorkbenchReconciliation,
            reconciliationWorkbench.ReplayRisk.StabilizationRecovering
                ? "Reconciliation recovery may continue with operator review"
                : "Backlog stabilization requires investigation focus",
            "Correlated reconciliation pressure — verify backlog and recovery convergence")));
    }

    private static void TryAddRuntimeCase(
        List<(int Priority, OperationalIncidentCaseDto Case)> candidates,
        OperationalTriageQueueDto triage,
        OperationalRecoveryPostureDto recovery,
        OperationalDashboardSummaryDto dashboard,
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection,
        bool runtimeSaturationIndicated,
        bool protectiveModeActive,
        OperationalReplayStabilizationDto replayStabilization,
        IReadOnlyList<OperationalIncidentCaseSnapshot> priorSnapshots,
        DateTime generatedAtUtc)
    {
        if (!runtimeSaturationIndicated && !protectiveModeActive && !runtimeProtection.FailsafeActive
            && !triage.Items.Any(i => i.Category == OperationalTriageCategory.RuntimeProtection))
            return;

        var severity = runtimeSaturationIndicated && protectiveModeActive
            ? OperationalIncidentSeverity.Critical
            : runtimeSaturationIndicated || protectiveModeActive
                ? OperationalIncidentSeverity.High
                : OperationalIncidentSeverity.Elevated;

        var direction = runtimeSaturationIndicated || protectiveModeActive
            ? OperationalIncidentDirection.Escalating
            : replayStabilization.ReplayRecoveryImproving
                ? OperationalIncidentDirection.Converging
                : OperationalIncidentDirection.Stable;

        var isEscalating = runtimeSaturationIndicated || protectiveModeActive || runtimeProtection.FailsafeActive;
        var isRecurring = DetectRecurrence(
            IncidentRuntimeProtection,
            RouteDashboard,
            "runtime",
            priorSnapshots);

        var state = ClassifyState(isEscalating, !isEscalating && replayStabilization.ReplayRecoveryImproving, isRecurring, severity);

        candidates.Add((4, BuildCase(
            IncidentRuntimeProtection,
            "Runtime protection incident",
            runtimeSaturationIndicated
                ? "Runtime pressure remains elevated"
                : dashboard.Pressure.Summary,
            severity,
            state,
            direction,
            OperationalIncidentConfidence.Low,
            generatedAtUtc,
            MapTriagePriority(triage.OverallPriority, severity),
            DescribeRecoveryAlignment(recovery, "Runtime"),
            isRecurring,
            isEscalating,
            CountRuntimeSignals(runtimeProtection, runtimeSaturationIndicated, protectiveModeActive),
            CountCorrelatedAreas(replay: false, inventory: false, reconciliation: false, runtime: true, operational: recovery.OverallSeverity >= OperationalRecoverySeverity.Elevated),
            RouteDashboard,
            WorkbenchDashboard,
            isEscalating ? "Runtime protection may constrain recovery outlook" : "Runtime pressure easing toward stability",
            "Correlated runtime protection — review pressure and survivability conditions")));
    }

    private static void TryAddOperationalVolatilityCase(
        List<(int Priority, OperationalIncidentCaseDto Case)> candidates,
        OperationalTrendSummaryDto trend,
        OperationalTimelineDto timeline,
        OperationalTriageQueueDto triage,
        OperationalRecoveryPostureDto recovery,
        OperationalGovernanceFingerprintSnapshot fingerprint,
        OperationalDashboardSummaryDto dashboard,
        IReadOnlyList<OperationalIncidentCaseSnapshot> priorSnapshots,
        DateTime generatedAtUtc)
    {
        var volatileFingerprint = IsVolatileFingerprint(fingerprint);
        var trendDegrading = trend.OverallDirection == OperationalTrendDirection.Degrading;
        var timelineAttention = timeline.AttentionItems.Count > 0;

        if (!volatileFingerprint && !trendDegrading && !timelineAttention
            && recovery.OverallState != OperationalRecoveryState.Volatile)
            return;

        var severity = recovery.OverallSeverity >= OperationalRecoverySeverity.High
            ? OperationalIncidentSeverity.High
            : OperationalIncidentSeverity.Elevated;

        var direction = trend.OverallDirection == OperationalTrendDirection.Improving
            ? OperationalIncidentDirection.Improving
            : trendDegrading || volatileFingerprint
                ? OperationalIncidentDirection.Diverging
                : OperationalIncidentDirection.Stable;

        var isEscalating = trendDegrading || volatileFingerprint;
        var isRecurring = DetectRecurrence(
            IncidentOperationalVolatility,
            RouteRecovery,
            "operational",
            priorSnapshots);

        var state = isRecurring && isEscalating
            ? OperationalIncidentState.Recurring
            : isEscalating
                ? OperationalIncidentState.Escalating
                : trend.OverallDirection == OperationalTrendDirection.Improving
                    ? OperationalIncidentState.Recovering
                    : OperationalIncidentState.Active;

        candidates.Add((5, BuildCase(
            IncidentOperationalVolatility,
            "Operational volatility incident",
            volatileFingerprint
                ? "Operational conditions remain volatile"
                : trend.Summary,
            severity,
            state,
            direction,
            MapRecoveryConfidence(recovery.OverallConfidence),
            generatedAtUtc,
            MapTriagePriority(triage.OverallPriority, severity),
            recovery.Summary,
            isRecurring,
            isEscalating,
            CountOperationalSignals(trend, timeline, volatileFingerprint),
            CountCorrelatedAreas(replay: false, inventory: false, reconciliation: false, runtime: false, operational: true),
            RouteRecovery,
            WorkbenchRecovery,
            "Operational stability may require sustained monitoring",
            dashboard.ReadinessSummary)));
    }

    private static IReadOnlyList<OperationalIncidentSignalDto> ComposeSignals(
        string incidentId,
        OperationalTrendSummaryDto trend,
        OperationalTimelineDto timeline,
        OperationalTriageQueueDto triage,
        OperationalRecoveryPostureDto recovery,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReplayRecoveryConfidenceDto replayRecoveryConfidence,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection,
        OperationalGovernanceFingerprintSnapshot fingerprint,
        bool runtimeSaturationIndicated,
        bool protectiveModeActive)
    {
        var signals = new List<OperationalIncidentSignalDto>();

        switch (incidentId.ToLowerInvariant())
        {
            case IncidentReplayInstability:
                AddSignal(signals, "Replay pressure", replayPressure.Summary, MapReplaySeverity(replayPressure.InstabilityLevel),
                    replayStabilization.ReplayRecoveryImproving ? OperationalIncidentDirection.Converging : OperationalIncidentDirection.Escalating,
                    replayStabilization.ReplayRecoveryImproving, "Replay");
                AddSignal(signals, "Recovery confidence", replayRecoveryConfidence.Summary,
                    MapReplayConfidenceSeverity(replayRecoveryConfidence), OperationalIncidentDirection.Stable,
                    replayStabilization.ReplayRecoveryImproving, "Recovery");
                break;

            case IncidentInventoryDrift:
                AddSignal(signals, "Inventory drift", inventoryWorkbench.DriftSummary.Summary,
                    MapInventorySeverity(inventoryWorkbench.DriftSummary.DriftSeverity),
                    inventoryWorkbench.DriftSummary.EscalatingDriftConflicts > 0
                        ? OperationalIncidentDirection.Diverging
                        : OperationalIncidentDirection.Stable,
                    inventoryWorkbench.DriftSummary.UnresolvedDriftConflicts == 0, "Inventory");
                break;

            case IncidentReconciliationPressure:
                AddSignal(signals, "Reconciliation backlog", reconciliationWorkbench.Queue.Summary,
                    MapReconciliationSeverity(reconciliationWorkbench.Queue), OperationalIncidentDirection.Stable,
                    reconciliationWorkbench.ReplayRisk.StabilizationRecovering, "Reconciliation");
                break;

            case IncidentRuntimeProtection:
                AddSignal(signals, "Runtime pressure",
                    runtimeSaturationIndicated ? "Runtime pressure remains elevated" : "Runtime protection active",
                    runtimeSaturationIndicated ? OperationalIncidentSeverity.High : OperationalIncidentSeverity.Elevated,
                    protectiveModeActive ? OperationalIncidentDirection.Escalating : OperationalIncidentDirection.Stable,
                    !protectiveModeActive, "Runtime");
                break;

            default:
                AddSignal(signals, "Operational trend", trend.Summary, MapTrendSeverity(trend.Severity),
                    trend.OverallDirection == OperationalTrendDirection.Improving
                        ? OperationalIncidentDirection.Improving
                        : OperationalIncidentDirection.Diverging,
                    trend.OverallDirection == OperationalTrendDirection.Improving, "Trend");
                AddSignal(signals, "Timeline activity", timeline.Summary, OperationalIncidentSeverity.Moderate,
                    OperationalIncidentDirection.Stable, timeline.AttentionItems.Count == 0, "Timeline");
                break;
        }

        foreach (var item in triage.Items.Take(3))
        {
            if (signals.Count >= MaxSignalsPerCase)
                break;

            AddSignal(signals, DescribeTriageCategory(item.Category), item.Summary,
                MapTriageItemSeverity(item.PriorityBand), OperationalIncidentDirection.Stable, false, "Triage");
        }

        if (recovery.Signals.Count > 0 && signals.Count < MaxSignalsPerCase)
        {
            var recoverySignal = recovery.Signals[0];
            AddSignal(signals, recoverySignal.Domain, recoverySignal.Summary,
                MapRecoverySeverity(recoverySignal.Severity),
                MapRecoveryDirection(recoverySignal.Direction),
                recoverySignal.Direction == OperationalRecoveryDirection.Converging
                    || recoverySignal.Direction == OperationalRecoveryDirection.Improving,
                "Recovery");
        }

        if (IsVolatileFingerprint(fingerprint) && signals.Count < MaxSignalsPerCase)
        {
            AddSignal(signals, "Stability transition", "Operational stability transitions detected",
                OperationalIncidentSeverity.Elevated, OperationalIncidentDirection.Diverging, false, "Stability");
        }

        return signals
            .OrderBy(s => s.SourceArea, StringComparer.Ordinal)
            .ThenBy(s => s.Category, StringComparer.Ordinal)
            .Take(MaxSignalsPerCase)
            .ToList();
    }

    private static OperationalInvestigationContextDto ComposeInvestigationContext(
        string incidentId,
        OperationalTimelineDto timeline,
        IReadOnlyList<OperationalTimelineCorrelationDto> timelineCorrelations,
        OperationalTriageQueueDto triage,
        OperationalRecoveryPostureDto recovery,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReplayRecoveryConfidenceDto replayRecoveryConfidence,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalGovernanceFingerprintSnapshot fingerprint,
        bool runtimeSaturationIndicated,
        bool protectiveModeActive)
    {
        var timelineMatch = timelineCorrelations.FirstOrDefault()?.Summary
            ?? (timeline.EventCount > 0 ? timeline.Summary : "No correlated timeline cluster");

        return new OperationalInvestigationContextDto
        {
            TimelineCorrelation = timelineMatch,
            ReplayAlignment = replayStabilization.ReplayRecoveryImproving
                ? "Replay pressure aligning with stabilization"
                : replayStabilization.ReplayPressureEscalating
                    ? "Replay pressure diverging from recovery"
                    : replayRecoveryConfidence.Summary,
            RecoveryAlignment = recovery.Summary,
            RuntimePressureAlignment = runtimeSaturationIndicated || protectiveModeActive
                ? "Runtime pressure constraining investigation continuity"
                : "Runtime pressure not dominating incident context",
            DriftAlignment = inventoryWorkbench.DriftSummary.EscalatingDriftConflicts > 0
                ? "Inventory drift diverging from stabilization"
                : inventoryWorkbench.DriftSummary.UnresolvedDriftConflicts == 0
                    ? "Inventory drift aligned with convergence"
                    : inventoryWorkbench.DriftSummary.Summary,
            TriageAlignment = triage.ItemCount > 0
                ? $"Triage queue prioritizes {triage.OverallPriority} investigation focus"
                : "Triage queue monitoring active",
            FingerprintAlignment = IsVolatileFingerprint(fingerprint)
                ? "Operational stability transitions suggest recurring instability"
                : fingerprint.FingerprintChanged
                    ? "Recent stability transition observed — monitor continuity"
                    : "Operational stability holding steady"
        };
    }

    private static OperationalIncidentOutlookDto ComposeOutlook(
        OperationalIncidentCaseDto incidentCase,
        OperationalRecoveryPostureDto recovery,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalTrendSummaryDto trend)
    {
        var recoveryDirection = incidentCase.Direction switch
        {
            OperationalIncidentDirection.Improving or OperationalIncidentDirection.Converging =>
                OperationalIncidentDirection.Converging,
            OperationalIncidentDirection.Escalating or OperationalIncidentDirection.Diverging =>
                OperationalIncidentDirection.Diverging,
            _ => OperationalIncidentDirection.Stable
        };

        return new OperationalIncidentOutlookDto
        {
            RecoveryDirection = recoveryDirection,
            StabilizationLikelihood = incidentCase.IsEscalating
                ? "Stabilization uncertain — escalation risk remains"
                : incidentCase.State is OperationalIncidentState.Recovering or OperationalIncidentState.Stabilizing
                    ? "Stabilization likely with continued operator monitoring"
                    : "Stabilization outlook requires correlation review",
            EscalationRisk = incidentCase.IsEscalating || incidentCase.IsRecurring
                ? "Escalation risk elevated — recurring or diverging signals"
                : "Escalation risk moderate",
            OperationalConfidence = incidentCase.Confidence,
            RecommendedOperatorFocus = DescribeOperatorFocus(incidentCase.IncidentId, recovery, trend, replayStabilization)
        };
    }

    private static OperationalIncidentCaseDto BuildCase(
        string incidentId,
        string title,
        string summary,
        OperationalIncidentSeverity severity,
        OperationalIncidentState state,
        OperationalIncidentDirection direction,
        OperationalIncidentConfidence confidence,
        DateTime generatedAtUtc,
        OperationalInvestigationPriority investigationPriority,
        string recoveryAlignment,
        bool isRecurring,
        bool isEscalating,
        int activeSignalCount,
        int correlatedAreaCount,
        string recommendedRoute,
        string recommendedWorkbench,
        string estimatedStabilization,
        string operatorSummary) =>
        new()
        {
            IncidentId = incidentId,
            Title = title,
            Summary = summary,
            Severity = severity,
            State = state,
            Direction = direction,
            Confidence = confidence,
            FirstObservedUtc = generatedAtUtc,
            LastObservedUtc = generatedAtUtc,
            InvestigationPriority = investigationPriority,
            RecoveryAlignment = recoveryAlignment,
            IsRecurring = isRecurring,
            IsEscalating = isEscalating,
            ActiveSignalCount = activeSignalCount,
            CorrelatedAreaCount = correlatedAreaCount,
            RecommendedRoute = recommendedRoute,
            RecommendedWorkbench = recommendedWorkbench,
            EstimatedStabilization = estimatedStabilization,
            OperatorSummary = operatorSummary
        };

    private static OperationalIncidentState ClassifyState(
        bool isEscalating,
        bool isRecovering,
        bool isRecurring,
        OperationalIncidentSeverity severity)
    {
        if (isRecurring && isEscalating)
            return OperationalIncidentState.Recurring;

        if (isEscalating)
            return OperationalIncidentState.Escalating;

        if (isRecovering)
            return severity <= OperationalIncidentSeverity.Moderate
                ? OperationalIncidentState.Recovering
                : OperationalIncidentState.Stabilizing;

        if (severity == OperationalIncidentSeverity.Nominal)
            return OperationalIncidentState.Resolved;

        return OperationalIncidentState.Active;
    }

    private static void AddSignal(
        List<OperationalIncidentSignalDto> signals,
        string category,
        string description,
        OperationalIncidentSeverity severity,
        OperationalIncidentDirection direction,
        bool isStabilizing,
        string sourceArea)
    {
        if (signals.Count >= MaxSignalsPerCase)
            return;

        signals.Add(new OperationalIncidentSignalDto
        {
            Category = category,
            Description = description,
            Severity = severity,
            Direction = direction,
            IsStabilizing = isStabilizing,
            SourceArea = sourceArea
        });
    }

    private static string ResolveCategoryKey(string incidentId) =>
        incidentId switch
        {
            IncidentReplayInstability => "replay",
            IncidentInventoryDrift => "inventory",
            IncidentReconciliationPressure => "reconciliation",
            IncidentRuntimeProtection => "runtime",
            _ => "operational"
        };

    private static string BuildStabilityKey(
        OperationalGovernanceFingerprintSnapshot fingerprint,
        string incidentId) =>
        string.IsNullOrWhiteSpace(fingerprint.FingerprintHash)
            ? incidentId
            : fingerprint.FingerprintHash[..Math.Min(8, fingerprint.FingerprintHash.Length)];

    private static bool IsVolatileFingerprint(OperationalGovernanceFingerprintSnapshot fingerprint) =>
        string.Equals(fingerprint.FingerprintStability, "Volatile", StringComparison.OrdinalIgnoreCase)
        || (fingerprint.FingerprintChanged && fingerprint.HasPreviousFingerprint);

    private static int CountCorrelatedAreas(bool replay, bool inventory, bool reconciliation, bool runtime, bool operational) =>
        (replay ? 1 : 0) + (inventory ? 1 : 0) + (reconciliation ? 1 : 0) + (runtime ? 1 : 0) + (operational ? 1 : 0);

    private static int CountReplaySignals(
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        int replayTriageCount)
    {
        var count = 0;
        if (replayPressure.InstabilityLevel >= OperationalReplayPressureLevel.Elevated) count++;
        if (replayStabilization.ReplayPressureEscalating) count++;
        if (replayStabilization.ReplayRecoveryImproving) count++;
        if (replayTriageCount > 0) count++;
        return Math.Max(count, 1);
    }

    private static int CountInventorySignals(
        OperationalInventoryDriftSummaryDto drift,
        OperationalReplayStabilizationDto replayStabilization)
    {
        var count = 0;
        if (drift.TotalInventoryDriftConflicts > 0) count++;
        if (drift.EscalatingDriftConflicts > 0) count++;
        if (drift.ReplayLinkedDriftPressure > 0) count++;
        if (replayStabilization.ReplayRecoveryImproving) count++;
        return Math.Max(count, 1);
    }

    private static int CountReconciliationSignals(
        OperationalReconciliationQueueDto queue,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench)
    {
        var count = 0;
        if (queue.UnresolvedConflicts > 0) count++;
        if (queue.EscalatingConflicts > 0) count++;
        if (reconciliationWorkbench.ReplayRisk.StabilizationRecovering) count++;
        return Math.Max(count, 1);
    }

    private static int CountRuntimeSignals(
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection,
        bool runtimeSaturationIndicated,
        bool protectiveModeActive)
    {
        var count = 0;
        if (runtimeSaturationIndicated) count++;
        if (protectiveModeActive) count++;
        if (runtimeProtection.FailsafeActive) count++;
        return Math.Max(count, 1);
    }

    private static int CountOperationalSignals(
        OperationalTrendSummaryDto trend,
        OperationalTimelineDto timeline,
        bool volatileFingerprint)
    {
        var count = 0;
        if (trend.OverallDirection != OperationalTrendDirection.Stable) count++;
        if (timeline.AttentionItems.Count > 0) count++;
        if (volatileFingerprint) count++;
        return Math.Max(count, 1);
    }

    private static string DescribeReplaySummary(
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReplayRecoveryConfidenceDto replayRecoveryConfidence)
    {
        if (replayStabilization.ReplayRecoveryImproving)
            return "Replay pressure stabilizing within correlated incident";

        if (replayStabilization.ReplayPressureEscalating)
            return "Replay instability remains elevated across correlated signals";

        return replayRecoveryConfidence.Summary;
    }

    private static string DescribeRecoveryAlignment(OperationalRecoveryPostureDto recovery, string domain)
    {
        var convergence = recovery.Convergence.FirstOrDefault(c =>
            string.Equals(c.Domain, domain, StringComparison.OrdinalIgnoreCase));

        return convergence?.Summary ?? recovery.Summary;
    }

    private static string DescribeEstimatedStabilization(
        OperationalReplayStabilizationDto replayStabilization,
        OperationalRecoveryPostureDto recovery) =>
        replayStabilization.ReplayRecoveryImproving
            ? "Replay stabilization progressing — monitor recovery outlook"
            : recovery.OverallState == OperationalRecoveryState.Recovering
                ? "Recovery convergence may shorten stabilization window"
                : "Stabilization timeline uncertain — review correlated signals";

    private static string DescribePlatformStability(OperationalRecoveryPostureDto recovery) =>
        recovery.OverallState switch
        {
            OperationalRecoveryState.Recovering => "Platform recovery in progress",
            OperationalRecoveryState.Stabilizing => "Platform stabilizing across active domains",
            OperationalRecoveryState.Degrading => "Platform conditions degrading",
            OperationalRecoveryState.Volatile => "Platform conditions remain volatile",
            OperationalRecoveryState.Saturated => "Platform recovery constrained by saturation",
            _ => "Platform stability holding with active monitoring"
        };

    private static string DescribeAttentionLevel(
        OperationalIncidentSeverity highestSeverity,
        int escalating,
        int recurring)
    {
        if (highestSeverity >= OperationalIncidentSeverity.Critical || escalating >= 2)
            return "Immediate operator attention recommended";

        if (highestSeverity >= OperationalIncidentSeverity.High || recurring >= 1)
            return "Elevated operator attention recommended";

        if (highestSeverity >= OperationalIncidentSeverity.Elevated)
            return "Moderate operator attention recommended";

        return "Routine monitoring sufficient";
    }

    private static string DescribeSummary(int active, int escalating, int recovering, int recurring)
    {
        if (active == 0)
            return "No active operational incident cases — continue routine monitoring";

        if (escalating > 0 && recurring > 0)
            return $"{active} active incident case(s) with escalating and recurring instability patterns";

        if (recovering > 0)
            return $"{active} active incident case(s) — recovery convergence detected in {recovering} area(s)";

        return $"{active} active operational incident case(s) require correlated investigation review";
    }

    private static string DescribeOperatorFocus(
        string incidentId,
        OperationalRecoveryPostureDto recovery,
        OperationalTrendSummaryDto trend,
        OperationalReplayStabilizationDto replayStabilization) =>
        incidentId switch
        {
            IncidentReplayInstability =>
                replayStabilization.ReplayRecoveryImproving
                    ? "Confirm replay stabilization progress and linked recovery signals"
                    : "Investigate replay escalation and correlated runtime pressure",
            IncidentInventoryDrift =>
                "Review inventory drift hotspots and reconciliation linkage",
            IncidentReconciliationPressure =>
                "Clear reconciliation backlog and verify recovery convergence",
            IncidentRuntimeProtection =>
                "Review runtime protection conditions affecting investigation continuity",
            _ =>
                trend.OverallDirection == OperationalTrendDirection.Improving
                    ? "Monitor operational volatility and recovery alignment"
                    : recovery.Summary
        };

    private static string DescribeTriageCategory(OperationalTriageCategory category) =>
        category switch
        {
            OperationalTriageCategory.ReplayInstability => "Replay instability",
            OperationalTriageCategory.RuntimeProtection => "Runtime protection",
            OperationalTriageCategory.InventoryDrift => "Inventory drift",
            OperationalTriageCategory.ReconciliationBacklog => "Reconciliation backlog",
            OperationalTriageCategory.Stabilization => "Stabilization",
            OperationalTriageCategory.TrendMovement => "Trend movement",
            _ => "System monitoring"
        };

    private static OperationalIncidentSeverity MapReplaySeverity(OperationalReplayPressureLevel level) =>
        level switch
        {
            OperationalReplayPressureLevel.Critical => OperationalIncidentSeverity.Critical,
            OperationalReplayPressureLevel.High => OperationalIncidentSeverity.High,
            OperationalReplayPressureLevel.Elevated => OperationalIncidentSeverity.Elevated,
            _ => OperationalIncidentSeverity.Nominal
        };

    private static OperationalIncidentSeverity MapInventorySeverity(OperationalInventoryDriftSeverity severity) =>
        severity switch
        {
            OperationalInventoryDriftSeverity.Critical => OperationalIncidentSeverity.Critical,
            OperationalInventoryDriftSeverity.High => OperationalIncidentSeverity.High,
            OperationalInventoryDriftSeverity.Elevated => OperationalIncidentSeverity.Elevated,
            _ => OperationalIncidentSeverity.Nominal
        };

    private static OperationalIncidentSeverity MapReconciliationSeverity(OperationalReconciliationQueueDto queue)
    {
        var score = queue.EscalatingConflicts + queue.UnresolvedConflicts;
        if (score >= 6) return OperationalIncidentSeverity.High;
        if (score >= 3) return OperationalIncidentSeverity.Elevated;
        if (score >= 1) return OperationalIncidentSeverity.Moderate;
        return OperationalIncidentSeverity.Nominal;
    }

    private static OperationalIncidentSeverity MapTrendSeverity(OperationalTrendSeverity severity) =>
        severity switch
        {
            OperationalTrendSeverity.Critical => OperationalIncidentSeverity.Critical,
            OperationalTrendSeverity.High => OperationalIncidentSeverity.High,
            OperationalTrendSeverity.Elevated => OperationalIncidentSeverity.Elevated,
            OperationalTrendSeverity.Moderate => OperationalIncidentSeverity.Moderate,
            _ => OperationalIncidentSeverity.Nominal
        };

    private static OperationalIncidentSeverity MapTriageItemSeverity(OperationalTriagePriority priority) =>
        priority switch
        {
            OperationalTriagePriority.Critical => OperationalIncidentSeverity.Critical,
            OperationalTriagePriority.High => OperationalIncidentSeverity.High,
            OperationalTriagePriority.Elevated => OperationalIncidentSeverity.Elevated,
            OperationalTriagePriority.Moderate => OperationalIncidentSeverity.Moderate,
            _ => OperationalIncidentSeverity.Nominal
        };

    private static OperationalIncidentSeverity MapReplayConfidenceSeverity(
        OperationalReplayRecoveryConfidenceDto replayRecoveryConfidence) =>
        replayRecoveryConfidence.Confidence switch
        {
            OperationalReplayRecoveryConfidence.Fragile => OperationalIncidentSeverity.High,
            OperationalReplayRecoveryConfidence.Uncertain => OperationalIncidentSeverity.Elevated,
            OperationalReplayRecoveryConfidence.Recovering => OperationalIncidentSeverity.Moderate,
            _ => OperationalIncidentSeverity.Nominal
        };

    private static OperationalIncidentSeverity MapRecoverySeverity(OperationalRecoverySeverity severity) =>
        severity switch
        {
            OperationalRecoverySeverity.Critical => OperationalIncidentSeverity.Critical,
            OperationalRecoverySeverity.High => OperationalIncidentSeverity.High,
            OperationalRecoverySeverity.Elevated => OperationalIncidentSeverity.Elevated,
            OperationalRecoverySeverity.Moderate => OperationalIncidentSeverity.Moderate,
            _ => OperationalIncidentSeverity.Nominal
        };

    private static OperationalIncidentDirection MapRecoveryDirection(OperationalRecoveryDirection direction) =>
        direction switch
        {
            OperationalRecoveryDirection.Improving => OperationalIncidentDirection.Improving,
            OperationalRecoveryDirection.Degrading => OperationalIncidentDirection.Degrading,
            OperationalRecoveryDirection.Converging => OperationalIncidentDirection.Converging,
            OperationalRecoveryDirection.Diverging => OperationalIncidentDirection.Diverging,
            _ => OperationalIncidentDirection.Stable
        };

    private static OperationalIncidentConfidence MapReplayConfidence(
        OperationalReplayRecoveryConfidenceDto replayRecoveryConfidence) =>
        replayRecoveryConfidence.Confidence switch
        {
            OperationalReplayRecoveryConfidence.Stable => OperationalIncidentConfidence.High,
            OperationalReplayRecoveryConfidence.Recovering => OperationalIncidentConfidence.Elevated,
            OperationalReplayRecoveryConfidence.Uncertain => OperationalIncidentConfidence.Moderate,
            _ => OperationalIncidentConfidence.Low
        };

    private static OperationalIncidentConfidence MapRecoveryConfidence(OperationalRecoveryConfidence confidence) =>
        confidence switch
        {
            OperationalRecoveryConfidence.High => OperationalIncidentConfidence.High,
            OperationalRecoveryConfidence.Elevated => OperationalIncidentConfidence.Elevated,
            OperationalRecoveryConfidence.Moderate => OperationalIncidentConfidence.Moderate,
            _ => OperationalIncidentConfidence.Low
        };

    private static OperationalInvestigationPriority MapTriagePriority(
        OperationalTriagePriority triagePriority,
        OperationalIncidentSeverity severity)
    {
        if (severity >= OperationalIncidentSeverity.Critical)
            return OperationalInvestigationPriority.Critical;

        return triagePriority switch
        {
            OperationalTriagePriority.Critical => OperationalInvestigationPriority.Critical,
            OperationalTriagePriority.High => OperationalInvestigationPriority.High,
            OperationalTriagePriority.Elevated => OperationalInvestigationPriority.Elevated,
            OperationalTriagePriority.Moderate => OperationalInvestigationPriority.Moderate,
            OperationalTriagePriority.Monitoring => OperationalInvestigationPriority.Monitoring,
            _ => OperationalInvestigationPriority.Stable
        };
    }
}
