using Tannous.Pos.Application.OperationalComposition;
using Tannous.Pos.Application.OperationalDashboard;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalInventoryWorkbench;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalReplayWorkbench;
using Tannous.Pos.Application.OperationalTimeline;
using Tannous.Pos.Application.OperationalTrends;
using Tannous.Pos.Application.OperationalTriage;
using Tannous.Pos.Application.OperationalWorkbench;

using Tannous.Pos.Application.OperationalCognition;

namespace Tannous.Pos.Application.OperationalCausality;

/// <summary>Deterministic operator causal interpretation from existing operational read models.</summary>
public static class OperationalCausalityAggregation
{
    public const int MaxCausalChains = 8;
    public const int MaxNodesPerChain = 8;
    public const int MaxRootCauseCandidates = 8;
    public const int MaxStabilizationBlockers = 8;
    public const int MaxPropagations = 8;
    public const int MaxStoredSnapshots = OperationalCognitionSnapshotLimits.MaxStoredSnapshots;

    public const string ChainReplayOrigin = "chain-replay-origin";
    public const string ChainInventoryOrigin = "chain-inventory-origin";
    public const string ChainRuntimeOrigin = "chain-runtime-origin";
    public const string ChainReconciliationOrigin = "chain-reconciliation-origin";
    public const string ChainVolatilityCycle = "chain-volatility-cycle";

    public const string AreaReplay = "Replay";
    public const string AreaInventory = "Inventory";
    public const string AreaReconciliation = "Reconciliation";
    public const string AreaRuntime = "Runtime";
    public const string AreaOperational = "Operational Stability";

    public static OperationalCausalChainsDto ComposeChains(
        OperationalTrendSummaryDto trend,
        OperationalTimelineDto timeline,
        IReadOnlyList<OperationalTimelineCorrelationDto> timelineCorrelations,
        OperationalTriageQueueDto triage,
        OperationalRecoveryPostureDto recovery,
        IReadOnlyList<OperationalIncidentCaseDto> incidents,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection,
        OperationalGovernanceFingerprintSnapshot fingerprint,
        bool runtimeSaturationIndicated,
        bool protectiveModeActive,
        IReadOnlyList<OperationalCausalitySnapshot> priorSnapshots,
        DateTime generatedAtUtc)
    {
        var candidates = new List<(int Priority, OperationalCausalChainDto Chain, List<OperationalCausalNodeDto> Nodes)>();

        TryAddReplayChain(
            candidates,
            trend,
            timelineCorrelations,
            triage,
            recovery,
            incidents,
            replayPressure,
            replayStabilization,
            reconciliationWorkbench,
            runtimeSaturationIndicated,
            priorSnapshots,
            generatedAtUtc);

        TryAddInventoryChain(
            candidates,
            trend,
            triage,
            recovery,
            incidents,
            inventoryWorkbench,
            reconciliationWorkbench,
            replayStabilization,
            generatedAtUtc);

        TryAddRuntimeChain(
            candidates,
            triage,
            recovery,
            incidents,
            runtimeProtection,
            runtimeSaturationIndicated,
            protectiveModeActive,
            replayStabilization,
            generatedAtUtc);

        TryAddReconciliationChain(
            candidates,
            triage,
            recovery,
            incidents,
            reconciliationWorkbench,
            replayStabilization,
            generatedAtUtc);

        TryAddVolatilityChain(
            candidates,
            trend,
            timeline,
            recovery,
            incidents,
            fingerprint,
            priorSnapshots,
            generatedAtUtc);

        var selected = candidates
            .OrderBy(c => c.Priority)
            .ThenBy(c => c.Chain.ChainId, StringComparer.Ordinal)
            .Take(MaxCausalChains)
            .ToList();

        var chains = selected.Select(c => c.Chain).ToList();
        var nodes = selected
            .SelectMany(c => c.Nodes)
            .OrderBy(n => n.ChainId, StringComparer.Ordinal)
            .ThenBy(n => n.Area, StringComparer.Ordinal)
            .Take(MaxCausalChains * MaxNodesPerChain)
            .ToList();

        return new OperationalCausalChainsDto
        {
            GeneratedAtUtc = generatedAtUtc,
            ChainCount = chains.Count,
            Chains = chains,
            Nodes = nodes
        };
    }

    public static OperationalCausalitySummaryDto ComposeSummary(
        OperationalCausalChainsDto chains,
        OperationalPropagationAnalysisDto propagation,
        OperationalRecoveryPostureDto recovery,
        DateTime generatedAtUtc)
    {
        var escalating = propagation.Propagations.Count(p => p.IsEscalating);
        var collapsing = propagation.Propagations.Count(p => p.IsCollapsing);
        var dominant = chains.Chains.Count == 0
            ? AreaOperational
            : chains.Chains
                .GroupBy(c => c.DominantArea, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.Ordinal)
                .First()
                .Key;

        var highestRisk = propagation.Propagations
            .OrderByDescending(p => p.IsEscalating)
            .ThenBy(p => p.SourceArea, StringComparer.Ordinal)
            .FirstOrDefault()?.OperatorInterpretation
            ?? "No active propagation escalation detected";

        return new OperationalCausalitySummaryDto
        {
            GeneratedAtUtc = generatedAtUtc,
            ActiveCausalChains = chains.ChainCount,
            EscalatingPropagationCount = escalating,
            CollapsingPropagationCount = collapsing,
            DominantOperationalArea = dominant,
            HighestRiskPropagation = highestRisk,
            StabilizationBlockerCount = propagation.StabilizationBlockerCount,
            PlatformRecoveryOutlook = recovery.Summary,
            OperatorAttentionLevel = DescribeAttentionLevel(chains.Chains, propagation.StabilizationBlockerCount),
            Summary = DescribeSummary(chains.ChainCount, escalating, collapsing, dominant)
        };
    }

    public static OperationalPropagationAnalysisDto ComposePropagationAnalysis(
        OperationalTrendSummaryDto trend,
        OperationalTimelineDto timeline,
        IReadOnlyList<OperationalTimelineCorrelationDto> timelineCorrelations,
        OperationalTriageQueueDto triage,
        OperationalRecoveryPostureDto recovery,
        IReadOnlyList<OperationalIncidentCaseDto> incidents,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection,
        OperationalGovernanceFingerprintSnapshot fingerprint,
        bool runtimeSaturationIndicated,
        bool protectiveModeActive)
    {
        var propagations = ComposePropagations(
            trend,
            replayPressure,
            replayStabilization,
            reconciliationWorkbench,
            inventoryWorkbench,
            runtimeSaturationIndicated,
            protectiveModeActive,
            recovery);

        var rootCauses = ComposeRootCauseCandidates(
            trend,
            timelineCorrelations,
            triage,
            recovery,
            incidents,
            replayPressure,
            replayStabilization,
            reconciliationWorkbench,
            inventoryWorkbench,
            runtimeSaturationIndicated,
            protectiveModeActive,
            fingerprint);

        var blockers = ComposeStabilizationBlockers(
            recovery,
            replayPressure,
            replayStabilization,
            reconciliationWorkbench,
            inventoryWorkbench,
            runtimeProtection,
            runtimeSaturationIndicated,
            protectiveModeActive,
            trend);

        return new OperationalPropagationAnalysisDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            PropagationCount = propagations.Count,
            RootCauseCandidateCount = rootCauses.Count,
            StabilizationBlockerCount = blockers.Count,
            Propagations = propagations,
            RootCauseCandidates = rootCauses,
            StabilizationBlockers = blockers
        };
    }

    public static OperationalCausalitySnapshot CreateSnapshot(
        OperationalCausalChainsDto chains,
        DateTime observedAtUtc)
    {
        var dominant = chains.Chains.Count == 0
            ? AreaOperational
            : chains.Chains[0].DominantArea;

        var direction = chains.Chains.Count == 0
            ? OperationalCausalityDirection.Stable
            : chains.Chains[0].PropagationDirection;

        return new OperationalCausalitySnapshot
        {
            DominantArea = dominant,
            PropagationDirection = direction,
            ActiveChainCount = chains.ChainCount,
            ObservedAtUtc = observedAtUtc
        };
    }

    public static bool DetectRecurringPropagation(
        string dominantArea,
        OperationalCausalityDirection direction,
        IReadOnlyList<OperationalCausalitySnapshot> priorSnapshots) =>
        priorSnapshots.Count(s =>
            string.Equals(s.DominantArea, dominantArea, StringComparison.OrdinalIgnoreCase)
            && s.PropagationDirection == direction) >= 1;

    private static void TryAddReplayChain(
        List<(int Priority, OperationalCausalChainDto Chain, List<OperationalCausalNodeDto> Nodes)> candidates,
        OperationalTrendSummaryDto trend,
        IReadOnlyList<OperationalTimelineCorrelationDto> timelineCorrelations,
        OperationalTriageQueueDto triage,
        OperationalRecoveryPostureDto recovery,
        IReadOnlyList<OperationalIncidentCaseDto> incidents,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        bool runtimeSaturationIndicated,
        IReadOnlyList<OperationalCausalitySnapshot> priorSnapshots,
        DateTime generatedAtUtc)
    {
        var replayActive = replayPressure.InstabilityLevel >= OperationalReplayPressureLevel.Elevated
            || replayStabilization.ReplayPressureEscalating
            || triage.Items.Any(i => i.Category == OperationalTriageCategory.ReplayInstability);

        if (!replayActive)
            return;

        var reconciliationFollows = reconciliationWorkbench.Queue.EscalatingConflicts > 0
            || timelineCorrelations.Any(c => c.CorrelationLabel.Contains("Replay", StringComparison.OrdinalIgnoreCase));

        var direction = replayStabilization.ReplayRecoveryImproving
            ? OperationalCausalityDirection.Collapsing
            : replayStabilization.ReplayPressureEscalating || runtimeSaturationIndicated
                ? OperationalCausalityDirection.Expanding
                : OperationalCausalityDirection.Stable;

        if (DetectRecurringPropagation(AreaReplay, direction, priorSnapshots))
            direction = OperationalCausalityDirection.Recurring;

        var depth = 1 + (reconciliationFollows ? 1 : 0) + (runtimeSaturationIndicated ? 1 : 0);
        var incidentCount = incidents.Count(i =>
            string.Equals(i.IncidentId, OperationalIncidentAggregation.IncidentReplayInstability, StringComparison.OrdinalIgnoreCase));

        var nodes = new List<OperationalCausalNodeDto>
        {
            CreateNode(ChainReplayOrigin, AreaReplay, OperationalCausalRole.Origin, MapReplaySeverity(replayPressure.InstabilityLevel),
                direction, isUpstream: true, isDownstream: false, replayStabilization.ReplayRecoveryImproving,
                "Replay pressure likely upstream — escalation observed before downstream effects"),
            CreateNode(ChainReplayOrigin, AreaReconciliation, OperationalCausalRole.Downstream,
                reconciliationWorkbench.Queue.EscalatingConflicts > 0 ? OperationalCausalitySeverity.Elevated : OperationalCausalitySeverity.Moderate,
                reconciliationFollows ? OperationalCausalityDirection.Expanding : OperationalCausalityDirection.Stable,
                isUpstream: false, isDownstream: true, reconciliationWorkbench.ReplayRisk.StabilizationRecovering,
                reconciliationFollows
                    ? "Reconciliation degradation following replay pressure"
                    : "Reconciliation not yet showing downstream propagation")
        };

        if (runtimeSaturationIndicated)
        {
            nodes.Add(CreateNode(ChainReplayOrigin, AreaRuntime, OperationalCausalRole.Amplifier,
                OperationalCausalitySeverity.High, OperationalCausalityDirection.Expanding,
                isUpstream: false, isDownstream: true, false,
                "Runtime pressure increase following replay instability"));
        }

        candidates.Add((1, new OperationalCausalChainDto
        {
            ChainId = ChainReplayOrigin,
            Title = "Replay-origin propagation chain",
            Summary = replayStabilization.ReplayPressureEscalating
                ? "Replay escalation likely upstream of downstream operational pressure"
                : "Replay stabilization may be collapsing propagation outward",
            DominantArea = AreaReplay,
            RootCauseCandidate = "Replay pressure likely upstream",
            StabilizationBlocker = replayStabilization.ReplayRecoveryStalled
                ? "Replay recovery stalled — blocking stabilization"
                : "None identified",
            PropagationDirection = direction,
            RecoveryImpact = DescribeRecoveryImpact(recovery, AreaReplay),
            OperationalConfidence = MapReplayConfidence(replayStabilization, reconciliationFollows),
            FirstObservedUtc = generatedAtUtc,
            LastObservedUtc = generatedAtUtc,
            CorrelatedIncidentCount = incidentCount,
            PropagationDepth = depth,
            OperatorSummary = "Replay instability appears to precede downstream reconciliation and runtime effects"
        }, nodes));
    }

    private static void TryAddInventoryChain(
        List<(int Priority, OperationalCausalChainDto Chain, List<OperationalCausalNodeDto> Nodes)> candidates,
        OperationalTrendSummaryDto trend,
        OperationalTriageQueueDto triage,
        OperationalRecoveryPostureDto recovery,
        IReadOnlyList<OperationalIncidentCaseDto> incidents,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalReplayStabilizationDto replayStabilization,
        DateTime generatedAtUtc)
    {
        var drift = inventoryWorkbench.DriftSummary;
        if (drift.DriftSeverity < OperationalInventoryDriftSeverity.Elevated
            && drift.EscalatingDriftConflicts == 0
            && !triage.Items.Any(i => i.Category == OperationalTriageCategory.InventoryDrift))
            return;

        var reconciliationFollows = reconciliationWorkbench.Queue.EscalatingConflicts > 0
            || drift.ReplayLinkedDriftPressure > 0;

        var direction = drift.EscalatingDriftConflicts > 0
            ? OperationalCausalityDirection.Expanding
            : drift.UnresolvedDriftConflicts == 0
                ? OperationalCausalityDirection.Collapsing
                : OperationalCausalityDirection.Stabilizing;

        var nodes = new List<OperationalCausalNodeDto>
        {
            CreateNode(ChainInventoryOrigin, AreaInventory, OperationalCausalRole.Origin,
                MapInventorySeverity(drift.DriftSeverity), direction, true, false,
                drift.UnresolvedDriftConflicts == 0, "Inventory drift likely upstream of reconciliation pressure"),
            CreateNode(ChainInventoryOrigin, AreaReconciliation, OperationalCausalRole.Downstream,
                reconciliationFollows ? OperationalCausalitySeverity.Elevated : OperationalCausalitySeverity.Moderate,
                reconciliationFollows ? OperationalCausalityDirection.Expanding : OperationalCausalityDirection.Stable,
                false, true, reconciliationWorkbench.ReplayRisk.StabilizationRecovering,
                reconciliationFollows ? "Reconciliation pressure following inventory drift" : "Reconciliation not yet amplified")
        };

        candidates.Add((2, new OperationalCausalChainDto
        {
            ChainId = ChainInventoryOrigin,
            Title = "Inventory drift propagation chain",
            Summary = drift.EscalatingDriftConflicts > 0
                ? "Inventory instability propagating outward toward reconciliation"
                : "Inventory conditions stabilizing — propagation may be collapsing",
            DominantArea = AreaInventory,
            RootCauseCandidate = "Inventory drift likely upstream",
            StabilizationBlocker = drift.EscalatingDriftConflicts > 0
                ? "Escalating drift blocking recovery convergence"
                : "None identified",
            PropagationDirection = direction,
            RecoveryImpact = DescribeRecoveryImpact(recovery, AreaInventory),
            OperationalConfidence = trend.OverallDirection == OperationalTrendDirection.Degrading
                ? OperationalCausalityConfidence.Elevated
                : OperationalCausalityConfidence.Moderate,
            FirstObservedUtc = generatedAtUtc,
            LastObservedUtc = generatedAtUtc,
            CorrelatedIncidentCount = incidents.Count(i =>
                string.Equals(i.IncidentId, OperationalIncidentAggregation.IncidentInventoryDrift, StringComparison.OrdinalIgnoreCase)),
            PropagationDepth = reconciliationFollows ? 2 : 1,
            OperatorSummary = "Inventory drift appears upstream of reconciliation degradation"
        }, nodes));
    }

    private static void TryAddRuntimeChain(
        List<(int Priority, OperationalCausalChainDto Chain, List<OperationalCausalNodeDto> Nodes)> candidates,
        OperationalTriageQueueDto triage,
        OperationalRecoveryPostureDto recovery,
        IReadOnlyList<OperationalIncidentCaseDto> incidents,
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection,
        bool runtimeSaturationIndicated,
        bool protectiveModeActive,
        OperationalReplayStabilizationDto replayStabilization,
        DateTime generatedAtUtc)
    {
        if (!runtimeSaturationIndicated && !protectiveModeActive && !runtimeProtection.FailsafeActive
            && !triage.Items.Any(i => i.Category == OperationalTriageCategory.RuntimeProtection))
            return;

        var direction = runtimeSaturationIndicated || protectiveModeActive
            ? OperationalCausalityDirection.Expanding
            : replayStabilization.ReplayRecoveryImproving
                ? OperationalCausalityDirection.Collapsing
                : OperationalCausalityDirection.Stabilizing;

        var nodes = new List<OperationalCausalNodeDto>
        {
            CreateNode(ChainRuntimeOrigin, AreaRuntime, OperationalCausalRole.Origin,
                runtimeSaturationIndicated ? OperationalCausalitySeverity.High : OperationalCausalitySeverity.Elevated,
                direction, true, false, !protectiveModeActive,
                "Runtime protection transition likely upstream of cross-area pressure"),
            CreateNode(ChainRuntimeOrigin, AreaReplay, OperationalCausalRole.Downstream,
                OperationalCausalitySeverity.Elevated,
                replayStabilization.ReplayPressureEscalating ? OperationalCausalityDirection.Expanding : OperationalCausalityDirection.Stable,
                false, true, replayStabilization.ReplayRecoveryImproving,
                "Replay pressure may be downstream of runtime saturation")
        };

        candidates.Add((3, new OperationalCausalChainDto
        {
            ChainId = ChainRuntimeOrigin,
            Title = "Runtime-origin propagation chain",
            Summary = runtimeSaturationIndicated
                ? "Runtime pressure acting as dominant source spreading across operational areas"
                : "Runtime protection easing — propagation may be collapsing",
            DominantArea = AreaRuntime,
            RootCauseCandidate = "Runtime pressure likely dominant source",
            StabilizationBlocker = protectiveModeActive
                ? "Protective mode preventing full stabilization"
                : "None identified",
            PropagationDirection = direction,
            RecoveryImpact = DescribeRecoveryImpact(recovery, AreaRuntime),
            OperationalConfidence = OperationalCausalityConfidence.Elevated,
            FirstObservedUtc = generatedAtUtc,
            LastObservedUtc = generatedAtUtc,
            CorrelatedIncidentCount = incidents.Count(i =>
                string.Equals(i.IncidentId, OperationalIncidentAggregation.IncidentRuntimeProtection, StringComparison.OrdinalIgnoreCase)),
            PropagationDepth = 2,
            OperatorSummary = "Runtime pressure appears upstream of replay and operational constraints"
        }, nodes));
    }

    private static void TryAddReconciliationChain(
        List<(int Priority, OperationalCausalChainDto Chain, List<OperationalCausalNodeDto> Nodes)> candidates,
        OperationalTriageQueueDto triage,
        OperationalRecoveryPostureDto recovery,
        IReadOnlyList<OperationalIncidentCaseDto> incidents,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalReplayStabilizationDto replayStabilization,
        DateTime generatedAtUtc)
    {
        var queue = reconciliationWorkbench.Queue;
        if (queue.EscalatingConflicts == 0 && queue.UnresolvedConflicts == 0
            && !reconciliationWorkbench.ReplayRisk.StabilizationRecovering
            && !triage.Items.Any(i => i.Category == OperationalTriageCategory.ReconciliationBacklog))
            return;

        var replayUpstream = replayStabilization.ReplayPressureEscalating
            || replayStabilization.ReplayRecoveryImproving;

        var direction = queue.EscalatingConflicts > 0
            ? OperationalCausalityDirection.Expanding
            : reconciliationWorkbench.ReplayRisk.StabilizationRecovering
                ? OperationalCausalityDirection.Collapsing
                : OperationalCausalityDirection.Stabilizing;

        var dominant = replayUpstream ? AreaReplay : AreaReconciliation;
        var rootCause = replayUpstream
            ? "Replay pressure likely upstream of reconciliation backlog"
            : "Reconciliation backlog may be local origin";

        var nodes = new List<OperationalCausalNodeDto>
        {
            CreateNode(ChainReconciliationOrigin, dominant, replayUpstream ? OperationalCausalRole.Upstream : OperationalCausalRole.Origin,
                queue.EscalatingConflicts > 0 ? OperationalCausalitySeverity.Elevated : OperationalCausalitySeverity.Moderate,
                direction, replayUpstream, !replayUpstream, reconciliationWorkbench.ReplayRisk.StabilizationRecovering,
                rootCause),
            CreateNode(ChainReconciliationOrigin, AreaReconciliation, OperationalCausalRole.Downstream,
                MapReconciliationSeverity(queue), direction, false, true,
                reconciliationWorkbench.ReplayRisk.StabilizationRecovering,
                queue.Summary)
        };

        candidates.Add((4, new OperationalCausalChainDto
        {
            ChainId = ChainReconciliationOrigin,
            Title = "Reconciliation propagation chain",
            Summary = queue.EscalatingConflicts > 0
                ? "Reconciliation backlog diverging — may amplify recovery delay"
                : "Reconciliation recovery convergence detected",
            DominantArea = dominant,
            RootCauseCandidate = rootCause,
            StabilizationBlocker = queue.EscalatingConflicts > 0
                ? "Escalating reconciliation backlog blocking stabilization"
                : "None identified",
            PropagationDirection = direction,
            RecoveryImpact = DescribeRecoveryImpact(recovery, AreaReconciliation),
            OperationalConfidence = OperationalCausalityConfidence.Moderate,
            FirstObservedUtc = generatedAtUtc,
            LastObservedUtc = generatedAtUtc,
            CorrelatedIncidentCount = incidents.Count(i =>
                string.Equals(i.IncidentId, OperationalIncidentAggregation.IncidentReconciliationPressure, StringComparison.OrdinalIgnoreCase)),
            PropagationDepth = replayUpstream ? 2 : 1,
            OperatorSummary = "Reconciliation pressure interpreted within upstream replay context"
        }, nodes));
    }

    private static void TryAddVolatilityChain(
        List<(int Priority, OperationalCausalChainDto Chain, List<OperationalCausalNodeDto> Nodes)> candidates,
        OperationalTrendSummaryDto trend,
        OperationalTimelineDto timeline,
        OperationalRecoveryPostureDto recovery,
        IReadOnlyList<OperationalIncidentCaseDto> incidents,
        OperationalGovernanceFingerprintSnapshot fingerprint,
        IReadOnlyList<OperationalCausalitySnapshot> priorSnapshots,
        DateTime generatedAtUtc)
    {
        var volatileFingerprint = IsVolatileFingerprint(fingerprint);
        if (!volatileFingerprint
            && trend.OverallDirection == OperationalTrendDirection.Stable
            && timeline.AttentionItems.Count == 0
            && recovery.OverallState != OperationalRecoveryState.Volatile)
            return;

        var direction = trend.OverallDirection == OperationalTrendDirection.Improving
            ? OperationalCausalityDirection.Collapsing
            : volatileFingerprint
                ? OperationalCausalityDirection.Cyclical
                : OperationalCausalityDirection.Recurring;

        if (DetectRecurringPropagation(AreaOperational, direction, priorSnapshots))
            direction = OperationalCausalityDirection.Recurring;

        var nodes = new List<OperationalCausalNodeDto>
        {
            CreateNode(ChainVolatilityCycle, AreaOperational, OperationalCausalRole.Origin,
                OperationalCausalitySeverity.Elevated, direction, true, false,
                trend.OverallDirection == OperationalTrendDirection.Improving,
                volatileFingerprint
                    ? "Operational stability transitions driving recurring instability"
                    : trend.Summary),
            CreateNode(ChainVolatilityCycle, AreaReplay, OperationalCausalRole.Amplifier,
                OperationalCausalitySeverity.Moderate, OperationalCausalityDirection.Cyclical,
                false, true, false, "Cross-area volatility amplification possible")
        };

        candidates.Add((5, new OperationalCausalChainDto
        {
            ChainId = ChainVolatilityCycle,
            Title = "Operational volatility cycle",
            Summary = volatileFingerprint
                ? "Operational conditions remain volatile — cyclical propagation likely"
                : "Operational trend movement suggests recurring instability pattern",
            DominantArea = AreaOperational,
            RootCauseCandidate = "Operational volatility likely recurring pattern",
            StabilizationBlocker = recovery.OverallState == OperationalRecoveryState.Volatile
                ? "Volatile conditions preventing stabilization confidence"
                : "None identified",
            PropagationDirection = direction,
            RecoveryImpact = recovery.Summary,
            OperationalConfidence = MapRecoveryConfidence(recovery.OverallConfidence),
            FirstObservedUtc = generatedAtUtc,
            LastObservedUtc = generatedAtUtc,
            CorrelatedIncidentCount = incidents.Count(i =>
                string.Equals(i.IncidentId, OperationalIncidentAggregation.IncidentOperationalVolatility, StringComparison.OrdinalIgnoreCase)),
            PropagationDepth = 2,
            OperatorSummary = "Operational volatility may be cycling across correlated areas"
        }, nodes));
    }

    private static IReadOnlyList<OperationalPressurePropagationDto> ComposePropagations(
        OperationalTrendSummaryDto trend,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        bool runtimeSaturationIndicated,
        bool protectiveModeActive,
        OperationalRecoveryPostureDto recovery)
    {
        var items = new List<(int Priority, OperationalPressurePropagationDto Item)>();

        if (replayPressure.InstabilityLevel >= OperationalReplayPressureLevel.Elevated)
        {
            items.Add((1, CreatePropagation(AreaReplay, AreaReconciliation, OperationalPropagationType.ReplayPressure,
                replayStabilization.ReplayPressureEscalating ? OperationalCausalityDirection.Expanding : OperationalCausalityDirection.Stabilizing,
                replayStabilization.ReplayPressureEscalating, replayStabilization.ReplayRecoveryImproving,
                "Replay pressure propagating toward reconciliation visibility")));

            if (runtimeSaturationIndicated)
            {
                items.Add((2, CreatePropagation(AreaReplay, AreaRuntime, OperationalPropagationType.ReplayPressure,
                    OperationalCausalityDirection.Expanding, true, false,
                    "Replay escalation followed by runtime pressure increase")));
            }
        }

        if (inventoryWorkbench.DriftSummary.EscalatingDriftConflicts > 0)
        {
            items.Add((3, CreatePropagation(AreaInventory, AreaReconciliation, OperationalPropagationType.InventoryDrift,
                OperationalCausalityDirection.Expanding, true, inventoryWorkbench.DriftSummary.UnresolvedDriftConflicts == 0,
                "Inventory drift propagating toward reconciliation degradation")));
        }

        if (runtimeSaturationIndicated || protectiveModeActive)
        {
            items.Add((4, CreatePropagation(AreaRuntime, AreaReplay, OperationalPropagationType.RuntimeProtection,
                OperationalCausalityDirection.Expanding, true, !protectiveModeActive,
                "Runtime protection pressure spreading across operational workflows")));
        }

        if (reconciliationWorkbench.Queue.EscalatingConflicts > 0 && !replayStabilization.ReplayPressureEscalating)
        {
            items.Add((5, CreatePropagation(AreaReconciliation, AreaOperational, OperationalPropagationType.ReconciliationPressure,
                OperationalCausalityDirection.Expanding, true, false,
                "Reconciliation backlog amplifying operational recovery delay")));
        }

        if (trend.OverallDirection == OperationalTrendDirection.Improving)
        {
            items.Add((6, CreatePropagation(AreaOperational, AreaReplay, OperationalPropagationType.OperationalVolatility,
                OperationalCausalityDirection.Collapsing, false, true,
                "Operational trend improving — pressure propagation collapsing")));
        }

        foreach (var convergence in recovery.Convergence.Where(c => c.Direction == OperationalRecoveryDirection.Converging))
        {
            if (items.Count >= MaxPropagations)
                break;

            items.Add((7, CreatePropagation(convergence.Domain, AreaOperational, OperationalPropagationType.OperationalVolatility,
                OperationalCausalityDirection.Collapsing, false, true, convergence.Summary)));
        }

        return items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Item.SourceArea, StringComparer.Ordinal)
            .Take(MaxPropagations)
            .Select(i => i.Item)
            .ToList();
    }

    private static IReadOnlyList<OperationalRootCauseCandidateDto> ComposeRootCauseCandidates(
        OperationalTrendSummaryDto trend,
        IReadOnlyList<OperationalTimelineCorrelationDto> timelineCorrelations,
        OperationalTriageQueueDto triage,
        OperationalRecoveryPostureDto recovery,
        IReadOnlyList<OperationalIncidentCaseDto> incidents,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        bool runtimeSaturationIndicated,
        bool protectiveModeActive,
        OperationalGovernanceFingerprintSnapshot fingerprint)
    {
        var candidates = new List<(int Priority, OperationalRootCauseCandidateDto Item)>();

        if (replayPressure.InstabilityLevel >= OperationalReplayPressureLevel.Elevated)
        {
            var evidence = new List<string>();
            if (replayStabilization.ReplayPressureEscalating) evidence.Add("Replay pressure escalating");
            if (reconciliationWorkbench.Queue.EscalatingConflicts > 0) evidence.Add("Reconciliation followed replay escalation");
            if (runtimeSaturationIndicated) evidence.Add("Runtime pressure increased after replay instability");

            candidates.Add((1, new OperationalRootCauseCandidateDto
            {
                Area = AreaReplay,
                Explanation = "Replay pressure likely started upstream instability",
                Confidence = replayStabilization.ReplayPressureEscalating
                    ? OperationalCausalityConfidence.Elevated
                    : OperationalCausalityConfidence.Moderate,
                EvidenceCount = Math.Max(evidence.Count, 1),
                SupportingSignals = evidence.Take(4).ToList(),
                RecoveryAlignment = recovery.Convergence.FirstOrDefault(c => c.Domain == AreaReplay)?.Summary ?? recovery.Summary
            }));
        }

        if (inventoryWorkbench.DriftSummary.EscalatingDriftConflicts > 0)
        {
            candidates.Add((2, new OperationalRootCauseCandidateDto
            {
                Area = AreaInventory,
                Explanation = "Inventory drift likely upstream of reconciliation pressure",
                Confidence = OperationalCausalityConfidence.Elevated,
                EvidenceCount = inventoryWorkbench.DriftSummary.EscalatingDriftConflicts,
                SupportingSignals = new[] { inventoryWorkbench.DriftSummary.Summary, "Escalating drift conflicts" },
                RecoveryAlignment = recovery.Convergence.FirstOrDefault(c => c.Domain == AreaInventory)?.Summary ?? recovery.Summary
            }));
        }

        if (runtimeSaturationIndicated && protectiveModeActive)
        {
            candidates.Add((3, new OperationalRootCauseCandidateDto
            {
                Area = AreaRuntime,
                Explanation = "Runtime protection transition likely dominant instability source",
                Confidence = OperationalCausalityConfidence.High,
                EvidenceCount = 2,
                SupportingSignals = new[] { "Runtime saturation indicated", "Protective mode active" },
                RecoveryAlignment = recovery.Convergence.FirstOrDefault(c => c.Domain == AreaRuntime)?.Summary ?? recovery.Summary
            }));
        }

        if (IsVolatileFingerprint(fingerprint))
        {
            candidates.Add((4, new OperationalRootCauseCandidateDto
            {
                Area = AreaOperational,
                Explanation = "Operational stability transitions suggest recurring volatility origin",
                Confidence = OperationalCausalityConfidence.Moderate,
                EvidenceCount = timelineCorrelations.Count + 1,
                SupportingSignals = new[] { "Stability transition detected", trend.Summary },
                RecoveryAlignment = recovery.Summary
            }));
        }

        if (incidents.Any(i => i.IsRecurring))
        {
            candidates.Add((5, new OperationalRootCauseCandidateDto
            {
                Area = incidents.First(i => i.IsRecurring).Title,
                Explanation = "Recurring incident pattern suggests repeated upstream trigger",
                Confidence = OperationalCausalityConfidence.Moderate,
                EvidenceCount = incidents.Count(i => i.IsRecurring),
                SupportingSignals = incidents.Where(i => i.IsRecurring).Select(i => i.Summary).Take(3).ToList(),
                RecoveryAlignment = recovery.Summary
            }));
        }

        return candidates
            .OrderBy(c => c.Priority)
            .ThenBy(c => c.Item.Area, StringComparer.Ordinal)
            .Take(MaxRootCauseCandidates)
            .Select(c => c.Item)
            .ToList();
    }

    private static IReadOnlyList<OperationalStabilizationBlockerDto> ComposeStabilizationBlockers(
        OperationalRecoveryPostureDto recovery,
        OperationalReplayPressureSummaryDto replayPressure,
        OperationalReplayStabilizationDto replayStabilization,
        OperationalReconciliationWorkbenchDto reconciliationWorkbench,
        OperationalInventoryWorkbenchDto inventoryWorkbench,
        OperationalGovernanceRuntimeProtectionSnapshot runtimeProtection,
        bool runtimeSaturationIndicated,
        bool protectiveModeActive,
        OperationalTrendSummaryDto trend)
    {
        var blockers = new List<(int Priority, OperationalStabilizationBlockerDto Item)>();

        if (replayStabilization.ReplayRecoveryStalled || replayPressure.InstabilityLevel >= OperationalReplayPressureLevel.High)
        {
            blockers.Add((1, new OperationalStabilizationBlockerDto
            {
                Area = AreaReplay,
                Description = replayStabilization.ReplayRecoveryStalled
                    ? "Replay recovery stalled — preventing stabilization convergence"
                    : "Replay instability remains elevated — blocking recovery confidence",
                Severity = MapReplaySeverity(replayPressure.InstabilityLevel),
                PreventingRecovery = replayStabilization.ReplayRecoveryStalled || recovery.OverallState == OperationalRecoveryState.Degrading,
                EscalationRisk = replayStabilization.ReplayPressureEscalating ? "High — replay escalation continuing" : "Moderate",
                SuggestedOperatorFocus = "Investigate replay pressure before downstream areas can stabilize"
            }));
        }

        if (inventoryWorkbench.DriftSummary.EscalatingDriftConflicts > 0)
        {
            blockers.Add((2, new OperationalStabilizationBlockerDto
            {
                Area = AreaInventory,
                Description = "Inventory drift escalation blocking inventory stabilization",
                Severity = MapInventorySeverity(inventoryWorkbench.DriftSummary.DriftSeverity),
                PreventingRecovery = true,
                EscalationRisk = "Elevated — drift divergence continuing",
                SuggestedOperatorFocus = "Review inventory drift hotspots and linked replay pressure"
            }));
        }

        if (reconciliationWorkbench.Queue.EscalatingConflicts > 0)
        {
            blockers.Add((3, new OperationalStabilizationBlockerDto
            {
                Area = AreaReconciliation,
                Description = "Escalating reconciliation backlog preventing recovery convergence",
                Severity = MapReconciliationSeverity(reconciliationWorkbench.Queue),
                PreventingRecovery = !reconciliationWorkbench.ReplayRisk.StabilizationRecovering,
                EscalationRisk = "Elevated — backlog diverging",
                SuggestedOperatorFocus = "Clear reconciliation backlog to unblock recovery outlook"
            }));
        }

        if (runtimeSaturationIndicated || protectiveModeActive || runtimeProtection.FailsafeActive)
        {
            blockers.Add((4, new OperationalStabilizationBlockerDto
            {
                Area = AreaRuntime,
                Description = "Runtime protection active — constraining stabilization outlook",
                Severity = runtimeSaturationIndicated ? OperationalCausalitySeverity.High : OperationalCausalitySeverity.Elevated,
                PreventingRecovery = true,
                EscalationRisk = "High — runtime pressure dominating",
                SuggestedOperatorFocus = "Review runtime pressure before cross-area recovery can progress"
            }));
        }

        if (trend.OverallDirection == OperationalTrendDirection.Degrading)
        {
            blockers.Add((5, new OperationalStabilizationBlockerDto
            {
                Area = AreaOperational,
                Description = "Operational trend degrading — blocking platform-wide stabilization confidence",
                Severity = OperationalCausalitySeverity.Elevated,
                PreventingRecovery = recovery.OverallDirection == OperationalRecoveryDirection.Degrading,
                EscalationRisk = "Moderate — trend divergence",
                SuggestedOperatorFocus = "Correlate trend movement with timeline and incident cases"
            }));
        }

        foreach (var attention in recovery.Attention.Take(2))
        {
            if (blockers.Count >= MaxStabilizationBlockers)
                break;

            blockers.Add((6, new OperationalStabilizationBlockerDto
            {
                Area = attention.Domain,
                Description = attention.Summary,
                Severity = MapRecoverySeverity(attention.Severity),
                PreventingRecovery = attention.Severity >= OperationalRecoverySeverity.Elevated,
                EscalationRisk = "Moderate",
                SuggestedOperatorFocus = attention.RecommendedRoute
            }));
        }

        return blockers
            .OrderBy(b => b.Priority)
            .ThenBy(b => b.Item.Area, StringComparer.Ordinal)
            .Take(MaxStabilizationBlockers)
            .Select(b => b.Item)
            .ToList();
    }

    private static OperationalCausalNodeDto CreateNode(
        string chainId,
        string area,
        OperationalCausalRole role,
        OperationalCausalitySeverity severity,
        OperationalCausalityDirection direction,
        bool isUpstream,
        bool isDownstream,
        bool isStabilizing,
        string summary) =>
        new()
        {
            ChainId = chainId,
            Area = area,
            Role = role,
            Severity = severity,
            Direction = direction,
            IsUpstream = isUpstream,
            IsDownstream = isDownstream,
            IsStabilizing = isStabilizing,
            ContributionSummary = summary
        };

    private static OperationalPressurePropagationDto CreatePropagation(
        string source,
        string target,
        OperationalPropagationType type,
        OperationalCausalityDirection direction,
        bool isEscalating,
        bool isCollapsing,
        string interpretation) =>
        new()
        {
            SourceArea = source,
            TargetArea = target,
            PropagationType = type,
            Direction = direction,
            IsEscalating = isEscalating,
            IsCollapsing = isCollapsing,
            OperatorInterpretation = interpretation
        };

    private static bool IsVolatileFingerprint(OperationalGovernanceFingerprintSnapshot fingerprint) =>
        string.Equals(fingerprint.FingerprintStability, "Volatile", StringComparison.OrdinalIgnoreCase)
        || (fingerprint.FingerprintChanged && fingerprint.HasPreviousFingerprint);

    private static string DescribeRecoveryImpact(OperationalRecoveryPostureDto recovery, string area)
    {
        var convergence = recovery.Convergence.FirstOrDefault(c =>
            string.Equals(c.Domain, area, StringComparison.OrdinalIgnoreCase));

        return convergence?.Summary ?? recovery.Summary;
    }

    private static string DescribeAttentionLevel(
        IReadOnlyList<OperationalCausalChainDto> chains,
        int blockerCount)
    {
        if (chains.Any(c => c.PropagationDirection == OperationalCausalityDirection.Expanding) && blockerCount >= 2)
            return "Immediate operator attention — expanding propagation with active blockers";

        if (chains.Any(c => c.PropagationDirection == OperationalCausalityDirection.Recurring))
            return "Elevated attention — recurring causal pattern detected";

        if (blockerCount > 0)
            return "Moderate attention — stabilization blockers require review";

        return "Routine monitoring — propagation stable or collapsing";
    }

    private static string DescribeSummary(int chainCount, int escalating, int collapsing, string dominant)
    {
        if (chainCount == 0)
            return "No active causal chains — operational propagation appears stable";

        if (escalating > 0 && collapsing == 0)
            return $"{chainCount} causal chain(s) with expanding propagation — {dominant} likely dominant upstream";

        if (collapsing > 0 && escalating == 0)
            return $"{chainCount} causal chain(s) — pressure propagation collapsing toward stabilization";

        return $"{chainCount} causal chain(s) — mixed propagation around {dominant}";
    }

    private static OperationalCausalityConfidence MapReplayConfidence(
        OperationalReplayStabilizationDto replayStabilization,
        bool downstreamEffects) =>
        replayStabilization.ReplayPressureEscalating && downstreamEffects
            ? OperationalCausalityConfidence.Elevated
            : replayStabilization.ReplayRecoveryImproving
                ? OperationalCausalityConfidence.High
                : OperationalCausalityConfidence.Moderate;

    private static OperationalCausalityConfidence MapRecoveryConfidence(OperationalRecoveryConfidence confidence) =>
        confidence switch
        {
            OperationalRecoveryConfidence.High => OperationalCausalityConfidence.High,
            OperationalRecoveryConfidence.Elevated => OperationalCausalityConfidence.Elevated,
            OperationalRecoveryConfidence.Moderate => OperationalCausalityConfidence.Moderate,
            _ => OperationalCausalityConfidence.Low
        };

    private static OperationalCausalitySeverity MapReplaySeverity(OperationalReplayPressureLevel level) =>
        level switch
        {
            OperationalReplayPressureLevel.Critical => OperationalCausalitySeverity.Critical,
            OperationalReplayPressureLevel.High => OperationalCausalitySeverity.High,
            OperationalReplayPressureLevel.Elevated => OperationalCausalitySeverity.Elevated,
            _ => OperationalCausalitySeverity.Nominal
        };

    private static OperationalCausalitySeverity MapInventorySeverity(OperationalInventoryDriftSeverity severity) =>
        severity switch
        {
            OperationalInventoryDriftSeverity.Critical => OperationalCausalitySeverity.Critical,
            OperationalInventoryDriftSeverity.High => OperationalCausalitySeverity.High,
            OperationalInventoryDriftSeverity.Elevated => OperationalCausalitySeverity.Elevated,
            _ => OperationalCausalitySeverity.Nominal
        };

    private static OperationalCausalitySeverity MapReconciliationSeverity(OperationalReconciliationQueueDto queue)
    {
        var score = queue.EscalatingConflicts + queue.UnresolvedConflicts;
        if (score >= 6) return OperationalCausalitySeverity.High;
        if (score >= 3) return OperationalCausalitySeverity.Elevated;
        if (score >= 1) return OperationalCausalitySeverity.Moderate;
        return OperationalCausalitySeverity.Nominal;
    }

    private static OperationalCausalitySeverity MapRecoverySeverity(OperationalRecoverySeverity severity) =>
        severity switch
        {
            OperationalRecoverySeverity.Critical => OperationalCausalitySeverity.Critical,
            OperationalRecoverySeverity.High => OperationalCausalitySeverity.High,
            OperationalRecoverySeverity.Elevated => OperationalCausalitySeverity.Elevated,
            OperationalRecoverySeverity.Moderate => OperationalCausalitySeverity.Moderate,
            _ => OperationalCausalitySeverity.Nominal
        };
}
