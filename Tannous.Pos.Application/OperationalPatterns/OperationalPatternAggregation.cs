using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTrends;

using Tannous.Pos.Application.OperationalCognition;

namespace Tannous.Pos.Application.OperationalPatterns;

/// <summary>Deterministic operational pattern recognition from bounded process-local continuity.</summary>
public static class OperationalPatternAggregation
{
    public const int MaxPatterns = 8;
    public const int MaxArchetypes = 8;
    public const int MaxCorrelations = 8;
    public const int MaxSequences = 8;
    public const int MaxStoredSnapshots = OperationalCognitionSnapshotLimits.MaxStoredSnapshots;
    public const int MaxSequenceStages = 4;

    public const string AreaReplay = "Replay";
    public const string AreaInventory = "Inventory";
    public const string AreaReconciliation = "Reconciliation";
    public const string AreaRuntime = "Runtime";
    public const string AreaOperational = "Operational Stability";

    public const string PatternReplayEscalation = "pattern-replay-escalation-cycle";
    public const string PatternRuntimeContainment = "pattern-runtime-containment-recovery";
    public const string PatternInventoryDrift = "pattern-inventory-drift-cascade";
    public const string PatternReconciliationVolatility = "pattern-reconciliation-volatility";
    public const string PatternRecoveryConvergence = "pattern-recovery-convergence";
    public const string PatternIncidentRecurrence = "pattern-incident-recurrence";
    public const string PatternCrossDomain = "pattern-cross-domain-instability";

    public static OperationalPatternsDto ComposePatterns(
        OperationalTrendSummaryDto trend,
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalCausalChainsDto chains,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationScenariosDto simulation,
        OperationalPlaybooksDto playbooks,
        IReadOnlyList<OperationalPatternSnapshot> priorSnapshots,
        DateTime generatedAtUtc)
    {
        var patterns = ComposePatternItems(
            trend,
            recovery,
            incidentSummary,
            causalitySummary,
            propagation,
            situationRoom,
            simulation,
            playbooks,
            priorSnapshots);

        var archetypes = ComposeArchetypes(
            recovery,
            incidentSummary,
            causalitySummary,
            propagation,
            situationRoom,
            simulation,
            playbooks,
            priorSnapshots);

        var correlations = ComposeCorrelations(patterns, archetypes, propagation);
        var sequences = ComposeSequences(propagation, simulation, playbooks, chains);
        var outlook = ComposeOutlook(patterns, archetypes, recovery, situationRoom, trend);

        return new OperationalPatternsDto
        {
            GeneratedAtUtc = generatedAtUtc,
            PatternCount = patterns.Count,
            CorrelationCount = correlations.Count,
            SequenceCount = sequences.Count,
            Patterns = patterns,
            Correlations = correlations,
            Sequences = sequences,
            Outlook = outlook
        };
    }

    public static OperationalStabilizationArchetypesDto ComposeArchetypesResponse(
        OperationalTrendSummaryDto trend,
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationScenariosDto simulation,
        OperationalPlaybooksDto playbooks,
        IReadOnlyList<OperationalPatternSnapshot> priorSnapshots,
        DateTime generatedAtUtc)
    {
        var archetypes = ComposeArchetypes(
            recovery,
            incidentSummary,
            causalitySummary,
            propagation,
            situationRoom,
            simulation,
            playbooks,
            priorSnapshots);

        return new OperationalStabilizationArchetypesDto
        {
            GeneratedAtUtc = generatedAtUtc,
            ArchetypeCount = archetypes.Count,
            Archetypes = archetypes
        };
    }

    public static OperationalPatternSummaryDto ComposeSummary(
        OperationalPatternsDto patterns,
        OperationalStabilizationArchetypesDto archetypes,
        OperationalSituationRoomDto situationRoom,
        DateTime generatedAtUtc)
    {
        var recurring = patterns.Patterns.Count(p => p.Frequency >= 2);
        var dominantArchetype = archetypes.Archetypes
            .OrderByDescending(a => a.RecoveryConfidence)
            .ThenBy(a => a.ArchetypeId, StringComparer.Ordinal)
            .FirstOrDefault()?.Name
            ?? "No dominant archetype identified";

        var highestRisk = patterns.Patterns
            .OrderByDescending(p => p.Severity)
            .ThenByDescending(p => p.Frequency)
            .ThenBy(p => p.PatternId, StringComparer.Ordinal)
            .FirstOrDefault()?.Title
            ?? "No elevated pattern risk";

        var recoveryStrength = patterns.Patterns.Any(p =>
            p.StabilityDirection is OperationalPatternDirection.Improving or OperationalPatternDirection.Stabilizing)
            ? OperationalPatternConfidence.Elevated
            : OperationalPatternConfidence.Moderate;

        var escalationStrength = patterns.Patterns.Any(p =>
            p.StabilityDirection is OperationalPatternDirection.Escalating or OperationalPatternDirection.Degrading)
            ? OperationalPatternConfidence.High
            : OperationalPatternConfidence.Low;

        var summary =
            $"{patterns.PatternCount} active pattern(s), {recurring} recurring. " +
            $"Dominant archetype: {dominantArchetype.ToLowerInvariant()}. " +
            $"Highest risk pattern: {highestRisk.ToLowerInvariant()}.";

        return new OperationalPatternSummaryDto
        {
            GeneratedAtUtc = generatedAtUtc,
            ActivePatternCount = patterns.PatternCount,
            RecurringPatternCount = recurring,
            DominantArchetype = dominantArchetype,
            HighestRiskPattern = highestRisk,
            RecoveryPatternStrength = recoveryStrength,
            EscalationPatternStrength = escalationStrength,
            OperatorAttentionLevel = situationRoom.AttentionLevel,
            Summary = summary
        };
    }

    public static OperationalPatternSnapshot CreateSnapshot(
        OperationalPatternsDto patterns,
        OperationalPatternSummaryDto summary,
        OperationalStabilizationArchetypesDto archetypes)
    {
        var dominantArea = patterns.Patterns
            .OrderByDescending(p => p.Severity)
            .ThenBy(p => p.DominantArea, StringComparer.Ordinal)
            .FirstOrDefault()?.DominantArea
            ?? AreaOperational;

        return new OperationalPatternSnapshot
        {
            GeneratedAtUtc = summary.GeneratedAtUtc,
            PatternCount = patterns.PatternCount,
            RecurringPatternCount = summary.RecurringPatternCount,
            DominantArchetype = summary.DominantArchetype,
            DominantArea = dominantArea,
            HighestRiskPattern = summary.HighestRiskPattern,
            OperatorSummary = summary.Summary
        };
    }

    private static List<OperationalPatternDto> ComposePatternItems(
        OperationalTrendSummaryDto trend,
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationScenariosDto simulation,
        OperationalPlaybooksDto playbooks,
        IReadOnlyList<OperationalPatternSnapshot> priorSnapshots)
    {
        var items = new List<(int Priority, OperationalPatternDto Pattern)>();

        var replayEscalating = propagation.Propagations.Any(p =>
            p.IsEscalating && string.Equals(p.SourceArea, AreaReplay, StringComparison.OrdinalIgnoreCase));

        if (replayEscalating || string.Equals(causalitySummary.DominantOperationalArea, AreaReplay, StringComparison.OrdinalIgnoreCase))
        {
            var frequency = CountSnapshotMatches(priorSnapshots, AreaReplay, PatternReplayEscalation);
            items.Add((1, new OperationalPatternDto
            {
                PatternId = PatternReplayEscalation,
                PatternType = OperationalPatternType.EscalationCycle,
                Title = "Replay escalation cycle",
                Description = "Replay pressure repeatedly upstream with reconciliation degradation following",
                DominantArea = AreaReplay,
                StabilityDirection = MapDirection(situationRoom.StabilizationDirection),
                Severity = OperationalPatternSeverity.High,
                Frequency = Math.Max(1, frequency),
                RecurrenceConfidence = MapFrequencyConfidence(frequency),
                OperationalImpact = "Reconciliation degradation and runtime survivability decline may follow",
                RecoveryAlignment = recovery.Summary,
                OperatorSummary = "Resembles known replay degradation archetype; stabilize replay upstream first"
            }));
        }

        if (situationRoom.EscalationSeverity >= OperationalExecutiveSeverity.High
            || propagation.Propagations.Any(p =>
                string.Equals(p.SourceArea, AreaRuntime, StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.TargetArea, AreaRuntime, StringComparison.OrdinalIgnoreCase)))
        {
            var frequency = CountSnapshotMatches(priorSnapshots, AreaRuntime, PatternRuntimeContainment);
            items.Add((2, new OperationalPatternDto
            {
                PatternId = PatternRuntimeContainment,
                PatternType = OperationalPatternType.ContainmentRecovery,
                Title = "Runtime containment recovery",
                Description = "Runtime protection escalation with replay instability secondary",
                DominantArea = AreaRuntime,
                StabilityDirection = recovery.OverallDirection is OperationalRecoveryDirection.Improving or OperationalRecoveryDirection.Converging
                    ? OperationalPatternDirection.Stabilizing
                    : OperationalPatternDirection.Escalating,
                Severity = OperationalPatternSeverity.Critical,
                Frequency = Math.Max(1, frequency),
                RecurrenceConfidence = MapFrequencyConfidence(frequency),
                OperationalImpact = "Propagation collapses after survivability improvement when contained",
                RecoveryAlignment = recovery.Summary,
                OperatorSummary = "Runtime containment before replay reassessment matches known recovery shape"
            }));
        }

        if (propagation.Propagations.Any(p =>
                string.Equals(p.SourceArea, AreaInventory, StringComparison.OrdinalIgnoreCase))
            || playbooks.Playbooks.Any(p => p.ScenarioType == OperationalPlaybookScenarioType.InventoryDriftStabilization))
        {
            var frequency = CountSnapshotMatches(priorSnapshots, AreaInventory, PatternInventoryDrift);
            items.Add((3, new OperationalPatternDto
            {
                PatternId = PatternInventoryDrift,
                PatternType = OperationalPatternType.DriftCascade,
                Title = "Inventory drift cascade",
                Description = "Drift concentration expands with reconciliation alignment degradation",
                DominantArea = AreaInventory,
                StabilityDirection = MapDirection(situationRoom.StabilizationDirection),
                Severity = OperationalPatternSeverity.Elevated,
                Frequency = Math.Max(1, frequency),
                RecurrenceConfidence = MapFrequencyConfidence(frequency),
                OperationalImpact = "Operational volatility increases across reconciliation domains",
                RecoveryAlignment = recovery.Summary,
                OperatorSummary = "Drift cascade pattern; stabilize inventory before reconciliation validation"
            }));
        }

        if (propagation.Propagations.Any(p =>
                string.Equals(p.SourceArea, AreaReconciliation, StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.TargetArea, AreaReconciliation, StringComparison.OrdinalIgnoreCase)))
        {
            var frequency = CountSnapshotMatches(priorSnapshots, AreaReconciliation, PatternReconciliationVolatility);
            items.Add((4, new OperationalPatternDto
            {
                PatternId = PatternReconciliationVolatility,
                PatternType = OperationalPatternType.VolatilityCycle,
                Title = "Reconciliation volatility cycle",
                Description = "Reconciliation queue pressure oscillates with cross-domain escalation",
                DominantArea = AreaReconciliation,
                StabilityDirection = trend.OverallDirection == OperationalTrendDirection.Degrading
                    ? OperationalPatternDirection.Degrading
                    : OperationalPatternDirection.Stable,
                Severity = OperationalPatternSeverity.Elevated,
                Frequency = Math.Max(1, frequency),
                RecurrenceConfidence = MapFrequencyConfidence(frequency),
                OperationalImpact = "Queue pressure may amplify replay and inventory instability",
                RecoveryAlignment = recovery.Summary,
                OperatorSummary = "Reconciliation volatility pattern emerging; validate upstream stabilization first"
            }));
        }

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving or OperationalRecoveryDirection.Converging)
        {
            items.Add((5, new OperationalPatternDto
            {
                PatternId = PatternRecoveryConvergence,
                PatternType = OperationalPatternType.RecoveryConvergence,
                Title = "Recovery convergence pattern",
                Description = "Recovery alignment improving across operational domains",
                DominantArea = causalitySummary.DominantOperationalArea,
                StabilityDirection = OperationalPatternDirection.Improving,
                Severity = OperationalPatternSeverity.Elevated,
                Frequency = CountSnapshotMatches(priorSnapshots, causalitySummary.DominantOperationalArea, PatternRecoveryConvergence),
                RecurrenceConfidence = OperationalPatternConfidence.Elevated,
                OperationalImpact = "Repeated recovery convergence behavior when dominant constraint eases",
                RecoveryAlignment = recovery.Summary,
                OperatorSummary = "Recovery convergence pattern; monitor residual pressure while convergence holds"
            }));
        }

        if (incidentSummary.RecurringIncidentCount >= 1 || incidentSummary.ActiveIncidentCount >= 2)
        {
            items.Add((6, new OperationalPatternDto
            {
                PatternId = PatternIncidentRecurrence,
                PatternType = OperationalPatternType.CrossDomainInstability,
                Title = "Incident recurrence pattern",
                Description = "Repeated incident continuity with cross-domain operational pressure",
                DominantArea = causalitySummary.DominantOperationalArea,
                StabilityDirection = OperationalPatternDirection.Degrading,
                Severity = MapIncidentSeverity(incidentSummary.HighestSeverity),
                Frequency = Math.Max(1, incidentSummary.RecurringIncidentCount),
                RecurrenceConfidence = incidentSummary.RecurringIncidentCount >= 2
                    ? OperationalPatternConfidence.High
                    : OperationalPatternConfidence.Moderate,
                OperationalImpact = incidentSummary.Summary,
                RecoveryAlignment = recovery.Summary,
                OperatorSummary = "Incident recurrence pattern; align investigation with stabilization sequencing"
            }));
        }

        if (simulation.DegradationPaths.Count >= 2)
        {
            items.Add((7, new OperationalPatternDto
            {
                PatternId = PatternCrossDomain,
                PatternType = OperationalPatternType.PropagationSequence,
                Title = "Cross-domain propagation sequence",
                Description = "Multiple degradation paths indicate repeated escalation sequencing",
                DominantArea = simulation.LeveragePoints.FirstOrDefault()?.Area ?? AreaOperational,
                StabilityDirection = OperationalPatternDirection.Escalating,
                Severity = OperationalPatternSeverity.High,
                Frequency = priorSnapshots.Count(s => s.PatternCount >= 2),
                RecurrenceConfidence = MapFrequencyConfidence(priorSnapshots.Count),
                OperationalImpact = "Repeated escalation flow across operational domains",
                RecoveryAlignment = recovery.Summary,
                OperatorSummary = "Cross-domain propagation sequence resembles prior bounded continuity snapshots"
            }));
        }

        if (items.Count == 0)
        {
            items.Add((99, new OperationalPatternDto
            {
                PatternId = "pattern-stable-baseline",
                PatternType = OperationalPatternType.StabilizationArchetype,
                Title = "Stable operational baseline",
                Description = $"No recurring instability pattern detected in {OperationalContinuityPhrasing.BoundedContinuityWindow}",
                DominantArea = AreaOperational,
                StabilityDirection = OperationalPatternDirection.Stable,
                Severity = OperationalPatternSeverity.Normal,
                Frequency = 1,
                RecurrenceConfidence = OperationalPatternConfidence.Moderate,
                OperationalImpact = "Operational patterns within expected advisory bounds",
                RecoveryAlignment = recovery.Summary,
                OperatorSummary = "No recurring pattern; continue routine monitoring"
            }));
        }

        return items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Pattern.PatternId, StringComparer.Ordinal)
            .Take(MaxPatterns)
            .Select(i => i.Pattern)
            .ToList();
    }

    private static List<OperationalStabilizationArchetypeDto> ComposeArchetypes(
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationScenariosDto simulation,
        OperationalPlaybooksDto playbooks,
        IReadOnlyList<OperationalPatternSnapshot> priorSnapshots)
    {
        var items = new List<(int Priority, OperationalStabilizationArchetypeDto Archetype)>();

        var replayEscalating = propagation.Propagations.Any(p =>
            p.IsEscalating && string.Equals(p.SourceArea, AreaReplay, StringComparison.OrdinalIgnoreCase));

        if (replayEscalating)
        {
            items.Add((1, new OperationalStabilizationArchetypeDto
            {
                ArchetypeId = "archetype-replay-escalation-cycle",
                Name = "Replay escalation cycle",
                ArchetypeType = OperationalArchetypeType.ReplayEscalationCycle,
                TriggerCharacteristics = "Replay pressure repeatedly upstream; reconciliation degradation follows",
                RecoveryBehavior = "Replay stabilization collapses downstream propagation",
                EscalationBehavior = "Replay to reconciliation to runtime pressure sequence",
                DominantConstraint = situationRoom.OutlookDetail.DominantConstraint,
                StabilizationLikelihood = OperationalPatternSeverity.Elevated,
                RecoveryConfidence = MapRecoveryConfidence(recovery.OverallConfidence),
                OperatorInterpretation = "Known replay degradation archetype; upstream replay stabilization is highest leverage"
            }));
        }

        if (situationRoom.EscalationSeverity >= OperationalExecutiveSeverity.High)
        {
            items.Add((2, new OperationalStabilizationArchetypeDto
            {
                ArchetypeId = "archetype-runtime-containment-recovery",
                Name = "Runtime containment recovery",
                ArchetypeType = OperationalArchetypeType.RuntimeContainmentRecovery,
                TriggerCharacteristics = "Runtime protection escalation first; replay instability secondary",
                RecoveryBehavior = "Propagation collapses after survivability improvement",
                EscalationBehavior = "Runtime survivability decline spreads to replay domains",
                DominantConstraint = "Runtime survivability limits downstream recovery",
                StabilizationLikelihood = OperationalPatternSeverity.High,
                RecoveryConfidence = MapRecoveryConfidence(recovery.OverallConfidence),
                OperatorInterpretation = "Runtime containment archetype; contain survivability before downstream reassessment"
            }));
        }

        if (playbooks.Playbooks.Any(p => p.ScenarioType == OperationalPlaybookScenarioType.InventoryDriftStabilization)
            || propagation.Propagations.Any(p => string.Equals(p.SourceArea, AreaInventory, StringComparison.OrdinalIgnoreCase)))
        {
            items.Add((3, new OperationalStabilizationArchetypeDto
            {
                ArchetypeId = "archetype-inventory-drift-cascade",
                Name = "Inventory drift cascade",
                ArchetypeType = OperationalArchetypeType.InventoryDriftCascade,
                TriggerCharacteristics = "Drift concentration expands; reconciliation alignment degrades",
                RecoveryBehavior = "Drift hotspot resolution reduces reconciliation escalation",
                EscalationBehavior = "Inventory to reconciliation volatility increase",
                DominantConstraint = "Unresolved drift hotspots",
                StabilizationLikelihood = OperationalPatternSeverity.Elevated,
                RecoveryConfidence = MapRecoveryConfidence(recovery.OverallConfidence),
                OperatorInterpretation = "Inventory drift cascade archetype; stabilize drift before reconciliation changes"
            }));
        }

        if (propagation.Propagations.Any(p =>
                string.Equals(p.SourceArea, AreaReconciliation, StringComparison.OrdinalIgnoreCase)))
        {
            items.Add((4, new OperationalStabilizationArchetypeDto
            {
                ArchetypeId = "archetype-reconciliation-volatility",
                Name = "Reconciliation volatility cycle",
                ArchetypeType = OperationalArchetypeType.ReconciliationVolatilityCycle,
                TriggerCharacteristics = "Queue pressure oscillates with cross-domain escalation",
                RecoveryBehavior = "Queue stabilization improves replay recovery alignment",
                EscalationBehavior = "Reconciliation pressure amplifies upstream instability",
                DominantConstraint = "Escalating reconciliation conflicts",
                StabilizationLikelihood = OperationalPatternSeverity.Elevated,
                RecoveryConfidence = OperationalPatternConfidence.Moderate,
                OperatorInterpretation = "Reconciliation volatility archetype; validate upstream before queue triage expansion"
            }));
        }

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving or OperationalRecoveryDirection.Converging)
        {
            items.Add((5, new OperationalStabilizationArchetypeDto
            {
                ArchetypeId = "archetype-recovery-convergence",
                Name = "Recovery convergence",
                ArchetypeType = OperationalArchetypeType.RecoveryConvergence,
                TriggerCharacteristics = "Dominant constraint eases; convergence signals align",
                RecoveryBehavior = "Recovery confidence improves across domains",
                EscalationBehavior = "Escalation pressure collapses when constraint eases",
                DominantConstraint = situationRoom.OutlookDetail.DominantConstraint,
                StabilizationLikelihood = OperationalPatternSeverity.High,
                RecoveryConfidence = OperationalPatternConfidence.High,
                OperatorInterpretation = "Recovery convergence archetype repeatedly succeeds when dominant constraint eases"
            }));
        }

        if (incidentSummary.RecurringIncidentCount >= 1)
        {
            items.Add((6, new OperationalStabilizationArchetypeDto
            {
                ArchetypeId = "archetype-incident-recurrence",
                Name = "Incident recurrence",
                ArchetypeType = OperationalArchetypeType.IncidentRecurrence,
                TriggerCharacteristics = $"{incidentSummary.RecurringIncidentCount} recurring incident signal(s)",
                RecoveryBehavior = "Incident continuity aligned with stabilization sequencing improves outcomes",
                EscalationBehavior = "Incident escalation amplifies cross-domain pressure",
                DominantConstraint = causalitySummary.DominantOperationalArea,
                StabilizationLikelihood = MapIncidentSeverity(incidentSummary.HighestSeverity),
                RecoveryConfidence = MapRecoveryConfidence(recovery.OverallConfidence),
                OperatorInterpretation = $"Incident recurrence archetype seen in {OperationalContinuityPhrasing.BoundedContinuityWindow}"
            }));
        }

        if (priorSnapshots.Count >= 2
            && priorSnapshots.GroupBy(s => s.DominantArea, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() >= 2))
        {
            var repeatedArea = priorSnapshots
                .GroupBy(s => s.DominantArea, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;

            items.Add((7, new OperationalStabilizationArchetypeDto
            {
                ArchetypeId = "archetype-operational-volatility",
                Name = "Operational volatility",
                ArchetypeType = OperationalArchetypeType.OperationalVolatility,
                TriggerCharacteristics = $"Repeated concentration in {repeatedArea.ToLowerInvariant()} across continuity snapshots",
                RecoveryBehavior = simulation.StabilizationPaths.FirstOrDefault()?.OperatorSummary ?? "Stabilization monitoring",
                EscalationBehavior = simulation.DegradationPaths.FirstOrDefault()?.OperatorSummary ?? "Cross-domain escalation",
                DominantConstraint = situationRoom.OutlookDetail.DominantConstraint,
                StabilizationLikelihood = OperationalPatternSeverity.Elevated,
                RecoveryConfidence = OperationalPatternConfidence.Moderate,
                OperatorInterpretation = $"Operational volatility archetype emerging around {repeatedArea.ToLowerInvariant()}"
            }));
        }

        if (items.Count == 0)
        {
            items.Add((99, new OperationalStabilizationArchetypeDto
            {
                ArchetypeId = "archetype-stable-baseline",
                Name = "Stable baseline",
                ArchetypeType = OperationalArchetypeType.RecoveryConvergence,
                TriggerCharacteristics = $"No recurring archetype in {OperationalContinuityPhrasing.BoundedContinuityWindow}",
                RecoveryBehavior = "Routine monitoring sufficient",
                EscalationBehavior = "Escalation pressure contained",
                DominantConstraint = "None identified",
                StabilizationLikelihood = OperationalPatternSeverity.Normal,
                RecoveryConfidence = OperationalPatternConfidence.Moderate,
                OperatorInterpretation = "No stabilization archetype active; platform within advisory bounds"
            }));
        }

        return items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Archetype.ArchetypeId, StringComparer.Ordinal)
            .Take(MaxArchetypes)
            .Select(i => i.Archetype)
            .ToList();
    }

    private static List<OperationalPatternCorrelationDto> ComposeCorrelations(
        IReadOnlyList<OperationalPatternDto> patterns,
        IReadOnlyList<OperationalStabilizationArchetypeDto> archetypes,
        OperationalPropagationAnalysisDto propagation)
    {
        var items = new List<(int Priority, OperationalPatternCorrelationDto Correlation)>();

        for (var i = 0; i < patterns.Count; i++)
        {
            for (var j = i + 1; j < patterns.Count; j++)
            {
                var left = patterns[i];
                var right = patterns[j];
                if (!string.Equals(left.DominantArea, right.DominantArea, StringComparison.OrdinalIgnoreCase)
                    && left.DominantArea != AreaOperational
                    && right.DominantArea != AreaOperational)
                {
                    var sharedAreas = new[] { left.DominantArea, right.DominantArea }.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    items.Add((1, new OperationalPatternCorrelationDto
                    {
                        SourcePattern = left.PatternId,
                        RelatedPattern = right.PatternId,
                        CorrelationStrength = OperationalPatternConfidence.Elevated,
                        SharedOperationalAreas = sharedAreas,
                        SharedPropagationCharacteristics = DescribeSharedPropagation(propagation, sharedAreas),
                        SharedRecoveryBehavior = "Upstream stabilization before downstream validation",
                        OperatorInterpretation = $"{left.Title} correlates with {right.Title.ToLowerInvariant()} in {OperationalContinuityPhrasing.BoundedContinuityWindow}"
                    }));
                }
            }
        }

        foreach (var pattern in patterns)
        {
            var matchingArchetype = archetypes.FirstOrDefault(a =>
                string.Equals(a.Name, pattern.Title, StringComparison.OrdinalIgnoreCase)
                || pattern.PatternId.Contains(a.ArchetypeType.ToString(), StringComparison.OrdinalIgnoreCase));

            if (matchingArchetype != null)
            {
                items.Add((2, new OperationalPatternCorrelationDto
                {
                    SourcePattern = pattern.PatternId,
                    RelatedPattern = matchingArchetype.ArchetypeId,
                    CorrelationStrength = pattern.RecurrenceConfidence,
                    SharedOperationalAreas = new[] { pattern.DominantArea },
                    SharedPropagationCharacteristics = matchingArchetype.EscalationBehavior,
                    SharedRecoveryBehavior = matchingArchetype.RecoveryBehavior,
                    OperatorInterpretation = $"Pattern aligns with {matchingArchetype.Name.ToLowerInvariant()} archetype"
                }));
            }
        }

        if (items.Count == 0 && patterns.Count > 0)
        {
            items.Add((99, new OperationalPatternCorrelationDto
            {
                SourcePattern = patterns[0].PatternId,
                RelatedPattern = "none",
                CorrelationStrength = OperationalPatternConfidence.Low,
                SharedOperationalAreas = new[] { patterns[0].DominantArea },
                SharedPropagationCharacteristics = "Limited cross-pattern correlation",
                SharedRecoveryBehavior = patterns[0].RecoveryAlignment,
                OperatorInterpretation = "Single dominant pattern; limited correlation breadth"
            }));
        }

        return items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Correlation.SourcePattern, StringComparer.Ordinal)
            .Take(MaxCorrelations)
            .Select(i => i.Correlation)
            .ToList();
    }

    private static List<OperationalPatternSequenceDto> ComposeSequences(
        OperationalPropagationAnalysisDto propagation,
        OperationalSimulationScenariosDto simulation,
        OperationalPlaybooksDto playbooks,
        OperationalCausalChainsDto chains)
    {
        var items = new List<(int Priority, OperationalPatternSequenceDto Sequence)>();

        var escalating = propagation.Propagations.Where(p => p.IsEscalating).OrderBy(p => p.SourceArea, StringComparer.Ordinal).ToList();
        if (escalating.Count >= 1)
        {
            var stages = escalating
                .Select(p => $"{p.SourceArea} to {p.TargetArea}")
                .Take(MaxSequenceStages)
                .ToList();

            items.Add((1, new OperationalPatternSequenceDto
            {
                SequenceId = "sequence-escalation-flow",
                SequenceType = OperationalPatternType.PropagationSequence,
                OperationalStages = stages,
                EscalationFlow = string.Join(" then ", stages).ToLowerInvariant(),
                RecoveryFlow = "Upstream stabilization before downstream validation",
                DominantTransition = escalating[0].OperatorInterpretation,
                OperatorSummary = "Repeated escalation sequence across operational domains"
            }));
        }

        var stabilizationPath = simulation.StabilizationPaths.FirstOrDefault();
        if (stabilizationPath != null)
        {
            items.Add((2, new OperationalPatternSequenceDto
            {
                SequenceId = "sequence-stabilization-flow",
                SequenceType = OperationalPatternType.StabilizationArchetype,
                OperationalStages = stabilizationPath.ExpectedImprovementSequence.Take(MaxSequenceStages).ToList(),
                EscalationFlow = "Escalation pressure collapses when upstream constraint eases",
                RecoveryFlow = stabilizationPath.OperatorSummary,
                DominantTransition = stabilizationPath.DominantArea,
                OperatorSummary = "Recurring stabilization ordering from hypothetical analysis alignment"
            }));
        }

        if (playbooks.ResponseSteps.Count >= 2)
        {
            items.Add((3, new OperationalPatternSequenceDto
            {
                SequenceId = "sequence-playbook-ordering",
                SequenceType = OperationalPatternType.StabilizationArchetype,
                OperationalStages = playbooks.ResponseSteps
                    .OrderBy(s => s.SequenceOrder)
                    .Select(s => s.RecommendedFocus)
                    .Take(MaxSequenceStages)
                    .ToList(),
                EscalationFlow = "Follow playbook sequencing to reduce escalation spread",
                RecoveryFlow = playbooks.Playbooks.FirstOrDefault()?.EstimatedOperationalImpact ?? "Recovery alignment",
                DominantTransition = playbooks.Playbooks.FirstOrDefault()?.DominantArea ?? AreaOperational,
                OperatorSummary = "Playbook response ordering matches recurring stabilization shape"
            }));
        }

        if (chains.ChainCount >= 1)
        {
            var chain = chains.Chains.OrderBy(c => c.ChainId, StringComparer.Ordinal).First();
            items.Add((4, new OperationalPatternSequenceDto
            {
                SequenceId = $"sequence-causal-{chain.ChainId}",
                SequenceType = OperationalPatternType.EscalationCycle,
                OperationalStages = new[] { chain.DominantArea, AreaReconciliation, AreaOperational }
                    .Take(MaxSequenceStages)
                    .ToList(),
                EscalationFlow = chain.Summary,
                RecoveryFlow = "Causal chain collapse after upstream stabilization",
                DominantTransition = chain.DominantArea,
                OperatorSummary = "Causal chain sequence resembles prior escalation patterns"
            }));
        }

        if (items.Count == 0)
        {
            items.Add((99, new OperationalPatternSequenceDto
            {
                SequenceId = "sequence-stable-baseline",
                SequenceType = OperationalPatternType.StabilizationArchetype,
                OperationalStages = new[] { "Routine monitoring" },
                EscalationFlow = "No active escalation sequence",
                RecoveryFlow = "Stable operational baseline",
                DominantTransition = AreaOperational,
                OperatorSummary = "No recurring sequence detected"
            }));
        }

        return items
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Sequence.SequenceId, StringComparer.Ordinal)
            .Take(MaxSequences)
            .Select(i => i.Sequence)
            .ToList();
    }

    private static OperationalPatternOutlookDto ComposeOutlook(
        IReadOnlyList<OperationalPatternDto> patterns,
        IReadOnlyList<OperationalStabilizationArchetypeDto> archetypes,
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalTrendSummaryDto trend)
    {
        var dominant = patterns
            .OrderByDescending(p => p.Severity)
            .ThenByDescending(p => p.Frequency)
            .FirstOrDefault()?.Title
            ?? "Stable operational baseline";

        var emerging = patterns
            .Where(p => p.Frequency <= 1 && p.Severity >= OperationalPatternSeverity.Elevated)
            .OrderByDescending(p => p.Severity)
            .FirstOrDefault()?.Title
            ?? "No emerging pattern";

        var recoveryPattern = patterns
            .FirstOrDefault(p => p.StabilityDirection == OperationalPatternDirection.Improving)?.Title
            ?? (recovery.OverallDirection is OperationalRecoveryDirection.Improving or OperationalRecoveryDirection.Converging
                ? "Recovery convergence emerging"
                : "Recovery pattern stable");

        var escalationPattern = patterns
            .FirstOrDefault(p => p.StabilityDirection == OperationalPatternDirection.Escalating)?.Title
            ?? "No active escalation pattern";

        var stabilizationPattern = archetypes
            .OrderByDescending(a => a.StabilizationLikelihood)
            .FirstOrDefault()?.Name
            ?? situationRoom.RecommendedOperationalFocus;

        return new OperationalPatternOutlookDto
        {
            DominantPattern = dominant,
            EmergingPattern = emerging,
            RecoveryPattern = recoveryPattern,
            EscalationPattern = escalationPattern,
            StabilizationPattern = stabilizationPattern,
            OperationalConfidence = recovery.OverallConfidence
        };
    }

    private static int CountSnapshotMatches(
        IReadOnlyList<OperationalPatternSnapshot> snapshots,
        string area,
        string patternHint)
    {
        if (snapshots.Count == 0)
            return 0;

        return snapshots.Count(s =>
            string.Equals(s.DominantArea, area, StringComparison.OrdinalIgnoreCase)
            || s.HighestRiskPattern.Contains(patternHint.Replace("pattern-", "").Replace("-", " "), StringComparison.OrdinalIgnoreCase));
    }

    private static string DescribeSharedPropagation(
        OperationalPropagationAnalysisDto propagation,
        IReadOnlyList<string> areas)
    {
        var matches = propagation.Propagations
            .Where(p => areas.Any(a =>
                string.Equals(p.SourceArea, a, StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.TargetArea, a, StringComparison.OrdinalIgnoreCase)))
            .Select(p => p.OperatorInterpretation)
            .Take(2)
            .ToList();

        return matches.Count == 0
            ? "Shared operational concentration without active propagation"
            : string.Join("; ", matches);
    }

    private static OperationalPatternDirection MapDirection(OperationalSituationDirection direction) =>
        direction switch
        {
            OperationalSituationDirection.Improving => OperationalPatternDirection.Improving,
            OperationalSituationDirection.Stabilizing => OperationalPatternDirection.Stabilizing,
            OperationalSituationDirection.Escalating => OperationalPatternDirection.Escalating,
            OperationalSituationDirection.Degrading => OperationalPatternDirection.Degrading,
            _ => OperationalPatternDirection.Stable
        };

    private static OperationalPatternConfidence MapFrequencyConfidence(int frequency) =>
        frequency switch
        {
            >= 3 => OperationalPatternConfidence.High,
            2 => OperationalPatternConfidence.Elevated,
            1 => OperationalPatternConfidence.Moderate,
            _ => OperationalPatternConfidence.Low
        };

    private static OperationalPatternConfidence MapRecoveryConfidence(OperationalRecoveryConfidence confidence) =>
        confidence switch
        {
            OperationalRecoveryConfidence.High => OperationalPatternConfidence.High,
            OperationalRecoveryConfidence.Elevated => OperationalPatternConfidence.Elevated,
            OperationalRecoveryConfidence.Moderate => OperationalPatternConfidence.Moderate,
            _ => OperationalPatternConfidence.Low
        };

    private static OperationalPatternSeverity MapIncidentSeverity(OperationalIncidentSeverity severity) =>
        severity switch
        {
            OperationalIncidentSeverity.Critical => OperationalPatternSeverity.Critical,
            OperationalIncidentSeverity.High => OperationalPatternSeverity.High,
            OperationalIncidentSeverity.Elevated => OperationalPatternSeverity.Elevated,
            _ => OperationalPatternSeverity.Normal
        };
}
