using Tannous.Pos.Application.OperationalCausality;
using Tannous.Pos.Application.OperationalIncidents;
using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalSimulation;
using Tannous.Pos.Application.OperationalSituationRoom;
using Tannous.Pos.Application.OperationalTrends;

using Tannous.Pos.Application.OperationalCognition;

namespace Tannous.Pos.Application.OperationalIntegrity;

/// <summary>Deterministic cross-layer operational interpretation integrity verification.</summary>
public static class OperationalIntegrityAggregation
{
    public const int MaxAlignments = 8;
    public const int MaxContradictions = 8;
    public const int MaxWarnings = 8;
    public const int MaxStoredSnapshots = OperationalCognitionSnapshotLimits.MaxStoredSnapshots;

    public const string LayerIncidents = "Incidents";
    public const string LayerCausality = "Causality";
    public const string LayerSimulation = "Simulation";
    public const string LayerPlaybooks = "Playbooks";
    public const string LayerPatterns = "Patterns";
    public const string LayerSituationRoom = "Situation Room";
    public const string LayerRecoveryPosture = "Recovery Posture";
    public const string LayerPropagation = "Propagation";

    public const string AreaReplay = "Replay";
    public const string AreaRuntime = "Runtime";
    public const string AreaInventory = "Inventory";
    public const string AreaReconciliation = "Reconciliation";
    public const string AreaOperational = "Operational Stability";

    public static OperationalIntegrityReportDto ComposeIntegrityReport(
        OperationalTrendSummaryDto trend,
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationScenariosDto simulation,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalSimulationOutlookDto simulationOutlook,
        OperationalPlaybooksDto playbooks,
        OperationalPatternsDto patterns,
        OperationalPatternSummaryDto patternSummary,
        IReadOnlyList<OperationalIntegritySnapshot> priorSnapshots,
        DateTime generatedAtUtc)
    {
        var alignments = ComposeAlignments(
            recovery,
            incidentSummary,
            causalitySummary,
            propagation,
            situationRoom,
            simulationSummary,
            playbooks,
            patterns,
            patternSummary);

        var contradictions = ComposeContradictions(
            recovery,
            incidentSummary,
            causalitySummary,
            propagation,
            situationRoom,
            simulation,
            simulationSummary,
            simulationOutlook,
            playbooks);

        var warnings = ComposeWarnings(
            causalitySummary,
            propagation,
            situationRoom,
            simulationSummary,
            playbooks,
            patterns,
            alignments,
            contradictions);

        var narrative = ComposeNarrativeConsistency(
            recovery,
            incidentSummary,
            causalitySummary,
            situationRoom,
            simulationSummary,
            playbooks,
            patternSummary,
            alignments,
            contradictions);

        var consistencyScore = ComputeConsistencyScore(alignments, contradictions, warnings);
        var integrityState = ResolveIntegrityState(contradictions, warnings, consistencyScore);
        var alignmentState = ResolveAlignmentState(alignments, contradictions);
        var dominantNarrative = narrative.DominantNarrative;
        var operatorSummary = ComposeOperatorSummary(
            integrityState,
            consistencyScore,
            contradictions.Count,
            alignments.Count,
            dominantNarrative,
            priorSnapshots);

        return new OperationalIntegrityReportDto
        {
            GeneratedAtUtc = generatedAtUtc,
            OverallIntegrityState = integrityState,
            ConsistencyScore = consistencyScore,
            DominantOperationalNarrative = dominantNarrative,
            AlignmentState = alignmentState,
            ContradictionCount = contradictions.Count,
            AlignmentCount = alignments.Count,
            Alignments = alignments,
            NarrativeConsistency = narrative,
            IntegrityWarnings = warnings,
            OperatorSummary = operatorSummary
        };
    }

    public static OperationalIntegritySummaryDto ComposeSummary(
        OperationalIntegrityReportDto report,
        OperationalSituationRoomDto situationRoom,
        DateTime generatedAtUtc)
    {
        var alignmentStrength = report.AlignmentCount switch
        {
            >= 5 => "Strong cross-layer alignment",
            >= 3 => "Moderate cross-layer alignment",
            >= 1 => "Partial cross-layer alignment",
            _ => "Limited cross-layer alignment"
        };

        var contradictionPressure = report.ContradictionCount switch
        {
            0 => "No active contradiction pressure",
            1 => "Single contradiction requires review",
            >= 4 => "High contradiction pressure across layers",
            _ => "Multiple contradictions require review"
        };

        var recoveryConsistency = report.NarrativeConsistency.RecoveryAlignment;
        var attention = situationRoom.AttentionLevel.ToString();

        var summary =
            $"Operational interpretation integrity is {report.OverallIntegrityState.ToString().ToLowerInvariant()} " +
            $"with consistency score {report.ConsistencyScore}. " +
            $"{report.AlignmentCount} alignment(s) and {report.ContradictionCount} contradiction(s) detected. " +
            $"Dominant story: {report.DominantOperationalNarrative.ToLowerInvariant()}.";

        return new OperationalIntegritySummaryDto
        {
            GeneratedAtUtc = generatedAtUtc,
            IntegrityState = report.OverallIntegrityState,
            AlignmentStrength = alignmentStrength,
            ContradictionPressure = contradictionPressure,
            DominantOperationalStory = report.DominantOperationalNarrative,
            RecoveryConsistency = recoveryConsistency,
            OperatorAttentionLevel = attention,
            Summary = summary
        };
    }

    public static OperationalIntegrityContradictionsDto ComposeContradictionsResponse(
        OperationalTrendSummaryDto trend,
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationScenariosDto simulation,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalSimulationOutlookDto simulationOutlook,
        OperationalPlaybooksDto playbooks,
        DateTime generatedAtUtc)
    {
        var contradictions = ComposeContradictions(
            recovery,
            incidentSummary,
            causalitySummary,
            propagation,
            situationRoom,
            simulation,
            simulationSummary,
            simulationOutlook,
            playbooks);

        return new OperationalIntegrityContradictionsDto
        {
            GeneratedAtUtc = generatedAtUtc,
            ContradictionCount = contradictions.Count,
            Contradictions = contradictions
        };
    }

    public static OperationalIntegritySnapshot CreateSnapshot(OperationalIntegrityReportDto report)
    {
        return new OperationalIntegritySnapshot
        {
            GeneratedAtUtc = report.GeneratedAtUtc,
            IntegrityState = report.OverallIntegrityState,
            ConsistencyScore = report.ConsistencyScore,
            ContradictionCount = report.ContradictionCount,
            AlignmentCount = report.AlignmentCount,
            DominantOperationalNarrative = report.DominantOperationalNarrative,
            AlignmentState = report.AlignmentState
        };
    }

    private static IReadOnlyList<OperationalInterpretationAlignmentDto> ComposeAlignments(
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalPlaybooksDto playbooks,
        OperationalPatternsDto patterns,
        OperationalPatternSummaryDto patternSummary)
    {
        var alignments = new List<OperationalInterpretationAlignmentDto>();
        var dominantArea = NormalizeArea(causalitySummary.DominantOperationalArea);
        var leverageArea = NormalizeArea(simulationSummary.HighestLeverageArea);
        var playbookArea = ResolveTopPlaybookArea(playbooks);
        var recoveryDirection = recovery.OverallDirection.ToString();

        if (AreasMatch(dominantArea, leverageArea))
        {
            alignments.Add(new OperationalInterpretationAlignmentDto
            {
                SourceLayer = LayerCausality,
                TargetLayer = LayerSimulation,
                AlignmentType = OperationalAlignmentType.DominantAreaMatch,
                AlignmentStrength = "Strong",
                SharedOperationalDirection = situationRoom.StabilizationDirection.ToString(),
                SharedDominantArea = dominantArea,
                SharedRecoveryInterpretation = recoveryDirection,
                OperatorInterpretation =
                    $"Causality and simulation both center on {dominantArea.ToLowerInvariant()} as the dominant operational focus"
            });
        }

        if (AreasMatch(dominantArea, playbookArea))
        {
            alignments.Add(new OperationalInterpretationAlignmentDto
            {
                SourceLayer = LayerCausality,
                TargetLayer = LayerPlaybooks,
                AlignmentType = OperationalAlignmentType.StabilizationCoherence,
                AlignmentStrength = "Strong",
                SharedOperationalDirection = situationRoom.StabilizationDirection.ToString(),
                SharedDominantArea = dominantArea,
                SharedRecoveryInterpretation = recoveryDirection,
                OperatorInterpretation =
                    $"Playbook guidance aligns with causality dominant area {dominantArea.ToLowerInvariant()}"
            });
        }

        if (AreasMatch(leverageArea, playbookArea))
        {
            alignments.Add(new OperationalInterpretationAlignmentDto
            {
                SourceLayer = LayerSimulation,
                TargetLayer = LayerPlaybooks,
                AlignmentType = OperationalAlignmentType.StabilizationCoherence,
                AlignmentStrength = "Moderate",
                SharedOperationalDirection = situationRoom.StabilizationDirection.ToString(),
                SharedDominantArea = leverageArea,
                SharedRecoveryInterpretation = recoveryDirection,
                OperatorInterpretation =
                    $"Simulation leverage point and playbook stabilization focus agree on {leverageArea.ToLowerInvariant()}"
            });
        }

        if (IsReplayStabilizationTriad(dominantArea, leverageArea, playbookArea, playbooks))
        {
            alignments.Add(new OperationalInterpretationAlignmentDto
            {
                SourceLayer = LayerCausality,
                TargetLayer = LayerPlaybooks,
                AlignmentType = OperationalAlignmentType.ReplayStabilizationAlignment,
                AlignmentStrength = "Strong",
                SharedOperationalDirection = OperationalConsistencyDirection.Aligning.ToString(),
                SharedDominantArea = AreaReplay,
                SharedRecoveryInterpretation = recoveryDirection,
                OperatorInterpretation =
                    "Replay is the shared dominant upstream source, highest leverage point, and prioritized stabilization target"
            });
        }

        if (IsRecoveryDirectionCoherent(recovery, situationRoom))
        {
            alignments.Add(new OperationalInterpretationAlignmentDto
            {
                SourceLayer = LayerRecoveryPosture,
                TargetLayer = LayerSituationRoom,
                AlignmentType = OperationalAlignmentType.RecoveryDirectionMatch,
                AlignmentStrength = "Moderate",
                SharedOperationalDirection = situationRoom.StabilizationDirection.ToString(),
                SharedDominantArea = dominantArea,
                SharedRecoveryInterpretation = recoveryDirection,
                OperatorInterpretation =
                    "Recovery posture direction aligns with situation room stabilization interpretation"
            });
        }

        if (IsPropagationConsistentWithRecovery(propagation, recovery))
        {
            alignments.Add(new OperationalInterpretationAlignmentDto
            {
                SourceLayer = LayerPropagation,
                TargetLayer = LayerRecoveryPosture,
                AlignmentType = OperationalAlignmentType.PropagationConsistency,
                AlignmentStrength = "Moderate",
                SharedOperationalDirection = recovery.OverallDirection.ToString(),
                SharedDominantArea = dominantArea,
                SharedRecoveryInterpretation = recoveryDirection,
                OperatorInterpretation =
                    "Propagation pressure interpretation remains consistent with recovery posture movement"
            });
        }

        if (IsRuntimeNarrativeCoherent(recovery, patternSummary, incidentSummary, patterns))
        {
            alignments.Add(new OperationalInterpretationAlignmentDto
            {
                SourceLayer = LayerPatterns,
                TargetLayer = LayerIncidents,
                AlignmentType = OperationalAlignmentType.NarrativeAgreement,
                AlignmentStrength = "Strong",
                SharedOperationalDirection = OperationalConsistencyDirection.Aligning.ToString(),
                SharedDominantArea = AreaRuntime,
                SharedRecoveryInterpretation = recoveryDirection,
                OperatorInterpretation =
                    "Runtime survivability decline, recurring runtime escalation patterns, and incident containment instability form a coherent operational narrative"
            });
        }

        if (NarrativeAreasAgree(dominantArea, situationRoom.HighestPriorityFocus, patternSummary.DominantArchetype))
        {
            alignments.Add(new OperationalInterpretationAlignmentDto
            {
                SourceLayer = LayerSituationRoom,
                TargetLayer = LayerPatterns,
                AlignmentType = OperationalAlignmentType.NarrativeAgreement,
                AlignmentStrength = "Moderate",
                SharedOperationalDirection = situationRoom.StabilizationDirection.ToString(),
                SharedDominantArea = dominantArea,
                SharedRecoveryInterpretation = recoveryDirection,
                OperatorInterpretation =
                    "Situation room priority focus and pattern archetype interpretation tell a consistent operational story"
            });
        }

        return alignments
            .OrderByDescending(a => a.AlignmentStrength, StringComparer.Ordinal)
            .ThenBy(a => a.SourceLayer, StringComparer.Ordinal)
            .ThenBy(a => a.TargetLayer, StringComparer.Ordinal)
            .Take(MaxAlignments)
            .ToList();
    }

    private static IReadOnlyList<OperationalContradictionDto> ComposeContradictions(
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationScenariosDto simulation,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalSimulationOutlookDto simulationOutlook,
        OperationalPlaybooksDto playbooks)
    {
        var contradictions = new List<OperationalContradictionDto>();
        var dominantArea = NormalizeArea(causalitySummary.DominantOperationalArea);
        var leverageArea = NormalizeArea(simulationSummary.HighestLeverageArea);
        var playbookArea = ResolveTopPlaybookArea(playbooks);

        if (IsRecoveryContradiction(recovery, simulation, simulationOutlook, situationRoom))
        {
            contradictions.Add(new OperationalContradictionDto
            {
                ContradictionId = "contradiction-recovery-simulation-escalation",
                SourceLayer = LayerRecoveryPosture,
                TargetLayer = LayerSimulation,
                ContradictionType = OperationalContradictionType.RecoverySimulationMismatch,
                Description =
                    "Recovery posture indicates improvement while simulation and situation room signal expanding degradation or worsening escalation",
                Severity = OperationalIntegritySeverity.High,
                OperationalRisk = "Operators may trust recovery movement while downstream layers still predict escalation expansion",
                RecommendedOperatorReview =
                    "Compare recovery posture sections with simulation degradation paths and situation room escalation severity"
            });
        }

        if (IsEscalationRecoveryConflict(recovery, situationRoom, propagation))
        {
            contradictions.Add(new OperationalContradictionDto
            {
                ContradictionId = "contradiction-escalation-recovery-conflict",
                SourceLayer = LayerSituationRoom,
                TargetLayer = LayerRecoveryPosture,
                ContradictionType = OperationalContradictionType.EscalationRecoveryConflict,
                Description =
                    "Situation room escalation pressure conflicts with recovery posture improving or converging interpretation",
                Severity = OperationalIntegritySeverity.Elevated,
                OperationalRisk = "Stabilization narratives may diverge between executive briefing and recovery outlook",
                RecommendedOperatorReview =
                    "Review situation room escalation severity against recovery direction and propagation counts"
            });
        }

        if (IsPlaybookRecoveryDivergence(recovery, playbooks))
        {
            contradictions.Add(new OperationalContradictionDto
            {
                ContradictionId = "contradiction-playbook-recovery-divergence",
                SourceLayer = LayerPlaybooks,
                TargetLayer = LayerRecoveryPosture,
                ContradictionType = OperationalContradictionType.PlaybookRecoveryDivergence,
                Description =
                    "Playbook guidance emphasizes recovery acceleration while recovery posture remains degrading or diverging",
                Severity = OperationalIntegritySeverity.Elevated,
                OperationalRisk = "Stabilization sequencing may not match observed recovery movement",
                RecommendedOperatorReview =
                    "Validate playbook priority ordering against recovery posture signals and outlook sections"
            });
        }

        if (IsDominantAreaConflict(dominantArea, leverageArea, playbookArea))
        {
            contradictions.Add(new OperationalContradictionDto
            {
                ContradictionId = "contradiction-dominant-area-conflict",
                SourceLayer = LayerCausality,
                TargetLayer = LayerSimulation,
                ContradictionType = OperationalContradictionType.DominantAreaConflict,
                Description =
                    $"Causality dominant area ({dominantArea}) diverges from simulation leverage ({leverageArea}) and playbook focus ({playbookArea})",
                Severity = OperationalIntegritySeverity.High,
                OperationalRisk = "Cross-layer stabilization guidance may target different operational areas",
                RecommendedOperatorReview =
                    "Reconcile causality dominant area with simulation leverage and playbook dominant areas"
            });
        }

        if (IsPropagationRecoveryConflict(propagation, recovery))
        {
            contradictions.Add(new OperationalContradictionDto
            {
                ContradictionId = "contradiction-propagation-recovery-conflict",
                SourceLayer = LayerPropagation,
                TargetLayer = LayerRecoveryPosture,
                ContradictionType = OperationalContradictionType.PropagationRecoveryConflict,
                Description =
                    "Escalating propagation pressure persists while recovery posture reports improving or converging movement",
                Severity = OperationalIntegritySeverity.Elevated,
                OperationalRisk = "Upstream pressure may still be spreading despite recovery posture optimism",
                RecommendedOperatorReview =
                    "Inspect propagation analysis escalating paths alongside recovery posture direction"
            });
        }

        if (IsNarrativeContradiction(situationRoom, incidentSummary, recovery))
        {
            contradictions.Add(new OperationalContradictionDto
            {
                ContradictionId = "contradiction-narrative-stabilization",
                SourceLayer = LayerSituationRoom,
                TargetLayer = LayerIncidents,
                ContradictionType = OperationalContradictionType.NarrativeContradiction,
                Description =
                    "Situation room stabilization narrative conflicts with active incident escalation and degrading recovery signals",
                Severity = OperationalIntegritySeverity.High,
                OperationalRisk = "Dominant operational story may not reflect active incident pressure",
                RecommendedOperatorReview =
                    "Compare situation room operator summary with incident case escalation counts and recovery direction"
            });
        }

        return contradictions
            .OrderByDescending(c => c.Severity)
            .ThenBy(c => c.ContradictionId, StringComparer.Ordinal)
            .Take(MaxContradictions)
            .ToList();
    }

    private static IReadOnlyList<OperationalIntegrityWarningDto> ComposeWarnings(
        OperationalCausalitySummaryDto causalitySummary,
        OperationalPropagationAnalysisDto propagation,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalPlaybooksDto playbooks,
        OperationalPatternsDto patterns,
        IReadOnlyList<OperationalInterpretationAlignmentDto> alignments,
        IReadOnlyList<OperationalContradictionDto> contradictions)
    {
        var warnings = new List<OperationalIntegrityWarningDto>();

        if (alignments.Count <= 1 && contradictions.Count >= 1)
        {
            warnings.Add(new OperationalIntegrityWarningDto
            {
                WarningType = "Interpretation Fragmentation",
                RelatedArea = NormalizeArea(causalitySummary.DominantOperationalArea),
                Description = "Limited cross-layer alignment with active contradictions detected",
                Severity = OperationalIntegritySeverity.Elevated,
                OperationalImpact = "Operational interpretations may require manual reconciliation before trusting stabilization guidance",
                SuggestedOperatorFocus = "Review alignment and contradiction endpoints together before acting on playbooks"
            });
        }

        if (CountEscalatingPropagations(propagation) >= 2
            && !string.Equals(
                playbooks.ResponseAlignment.SimulationAlignment,
                playbooks.ResponseAlignment.CausalityAlignment,
                StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(new OperationalIntegrityWarningDto
            {
                WarningType = "Stabilization Guidance Divergence",
                RelatedArea = NormalizeArea(simulationSummary.HighestLeverageArea),
                Description = "Playbook cross-layer response alignment shows divergence between causality and simulation interpretations",
                Severity = OperationalIntegritySeverity.Elevated,
                OperationalImpact = "Stabilization sequencing may not reflect consistent leverage interpretation",
                SuggestedOperatorFocus = "Validate playbook response alignment against simulation leverage and causality summaries"
            });
        }

        if (situationRoom.EscalationSeverity >= OperationalExecutiveSeverity.High
            && patterns.PatternCount == 0)
        {
            warnings.Add(new OperationalIntegrityWarningDto
            {
                WarningType = "Narrative Support Gap",
                RelatedArea = NormalizeArea(causalitySummary.DominantOperationalArea),
                Description = "High escalation severity without supporting recurring pattern recognition",
                Severity = OperationalIntegritySeverity.Normal,
                OperationalImpact = "Dominant operational story lacks pattern continuity reinforcement",
                SuggestedOperatorFocus = "Correlate situation room escalation with timeline and incident recurrence signals"
            });
        }

        if (contradictions.Any(c => c.Severity >= OperationalIntegritySeverity.High))
        {
            warnings.Add(new OperationalIntegrityWarningDto
            {
                WarningType = "High-Severity Contradiction",
                RelatedArea = NormalizeArea(causalitySummary.DominantOperationalArea),
                Description = "One or more high-severity cross-layer contradictions require operator review",
                Severity = OperationalIntegritySeverity.High,
                OperationalImpact = "Operational trustworthiness of the dominant narrative is reduced until contradictions are reviewed",
                SuggestedOperatorFocus = "Start with contradictions endpoint and reconcile affected layer summaries"
            });
        }

        return warnings
            .OrderByDescending(w => w.Severity)
            .ThenBy(w => w.WarningType, StringComparer.Ordinal)
            .Take(MaxWarnings)
            .ToList();
    }

    private static OperationalNarrativeConsistencyDto ComposeNarrativeConsistency(
        OperationalRecoveryPostureDto recovery,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalCausalitySummaryDto causalitySummary,
        OperationalSituationRoomDto situationRoom,
        OperationalSimulationSummaryDto simulationSummary,
        OperationalPlaybooksDto playbooks,
        OperationalPatternSummaryDto patternSummary,
        IReadOnlyList<OperationalInterpretationAlignmentDto> alignments,
        IReadOnlyList<OperationalContradictionDto> contradictions)
    {
        var dominantArea = NormalizeArea(causalitySummary.DominantOperationalArea);
        var dominantNarrative =
            $"{dominantArea} operational pressure with {recovery.OverallDirection.ToString().ToLowerInvariant()} recovery movement " +
            $"and {situationRoom.StabilizationDirection.ToString().ToLowerInvariant()} stabilization direction";

        var supporting = new List<string>();
        var contradicting = new List<string>();

        if (alignments.Any(a => a.SourceLayer == LayerCausality || a.TargetLayer == LayerCausality))
            supporting.Add(LayerCausality);
        if (alignments.Any(a => a.SourceLayer == LayerSimulation || a.TargetLayer == LayerSimulation))
            supporting.Add(LayerSimulation);
        if (alignments.Any(a => a.SourceLayer == LayerPlaybooks || a.TargetLayer == LayerPlaybooks))
            supporting.Add(LayerPlaybooks);
        if (alignments.Any(a => a.SourceLayer == LayerPatterns || a.TargetLayer == LayerPatterns))
            supporting.Add(LayerPatterns);
        if (alignments.Any(a => a.SourceLayer == LayerSituationRoom || a.TargetLayer == LayerSituationRoom))
            supporting.Add(LayerSituationRoom);
        if (alignments.Any(a => a.SourceLayer == LayerRecoveryPosture || a.TargetLayer == LayerRecoveryPosture))
            supporting.Add(LayerRecoveryPosture);

        foreach (var contradiction in contradictions)
        {
            if (!contradicting.Contains(contradiction.SourceLayer))
                contradicting.Add(contradiction.SourceLayer);
            if (!contradicting.Contains(contradiction.TargetLayer))
                contradicting.Add(contradiction.TargetLayer);
        }

        var stabilityDirection = ResolveStabilityDirection(recovery, situationRoom, contradictions);
        var recoveryAlignment = IsRecoveryDirectionCoherent(recovery, situationRoom)
            ? "Recovery interpretation aligns across recovery posture and situation room"
            : "Recovery interpretation diverges between recovery posture and situation room";

        var operationalConfidence = contradictions.Count switch
        {
            0 when alignments.Count >= 4 => "High operational narrative consistency",
            0 => "Moderate operational narrative consistency",
            >= 3 => "Low operational narrative consistency",
            _ => "Reduced operational narrative consistency"
        };

        return new OperationalNarrativeConsistencyDto
        {
            DominantNarrative = dominantNarrative,
            SupportingLayers = supporting
                .OrderBy(l => l, StringComparer.Ordinal)
                .Take(8)
                .ToList(),
            ContradictingLayers = contradicting
                .OrderBy(l => l, StringComparer.Ordinal)
                .Take(8)
                .ToList(),
            StabilityDirection = stabilityDirection,
            RecoveryAlignment = recoveryAlignment,
            OperationalConfidence = operationalConfidence
        };
    }

    private static int ComputeConsistencyScore(
        IReadOnlyList<OperationalInterpretationAlignmentDto> alignments,
        IReadOnlyList<OperationalContradictionDto> contradictions,
        IReadOnlyList<OperationalIntegrityWarningDto> warnings)
    {
        var score = 100;
        score -= contradictions.Count(c => c.Severity == OperationalIntegritySeverity.Critical) * 25;
        score -= contradictions.Count(c => c.Severity == OperationalIntegritySeverity.High) * 15;
        score -= contradictions.Count(c => c.Severity == OperationalIntegritySeverity.Elevated) * 8;
        score -= warnings.Count(w => w.Severity >= OperationalIntegritySeverity.Elevated) * 4;
        score += alignments.Count(a => string.Equals(a.AlignmentStrength, "Strong", StringComparison.OrdinalIgnoreCase)) * 3;
        score += alignments.Count(a => string.Equals(a.AlignmentStrength, "Moderate", StringComparison.OrdinalIgnoreCase)) * 1;

        return Math.Clamp(score, 0, 100);
    }

    private static OperationalIntegrityState ResolveIntegrityState(
        IReadOnlyList<OperationalContradictionDto> contradictions,
        IReadOnlyList<OperationalIntegrityWarningDto> warnings,
        int consistencyScore)
    {
        if (contradictions.Any(c => c.Severity >= OperationalIntegritySeverity.High) || consistencyScore < 45)
            return OperationalIntegrityState.Contradictory;

        if (contradictions.Count >= 2 || consistencyScore < 65)
            return OperationalIntegrityState.Fragmented;

        if (contradictions.Count == 1 || warnings.Count >= 2 || consistencyScore < 80)
            return OperationalIntegrityState.MostlyCoherent;

        return OperationalIntegrityState.Coherent;
    }

    private static string ResolveAlignmentState(
        IReadOnlyList<OperationalInterpretationAlignmentDto> alignments,
        IReadOnlyList<OperationalContradictionDto> contradictions)
    {
        if (alignments.Count >= 4 && contradictions.Count == 0)
            return "Strong cross-layer alignment";

        if (alignments.Count >= 2 && contradictions.Count <= 1)
            return "Moderate cross-layer alignment";

        if (contradictions.Count >= 2)
            return "Cross-layer alignment under contradiction pressure";

        return "Partial cross-layer alignment";
    }

    private static OperationalConsistencyDirection ResolveStabilityDirection(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        IReadOnlyList<OperationalContradictionDto> contradictions)
    {
        if (contradictions.Count >= 2)
            return OperationalConsistencyDirection.Contradicting;

        if (recovery.OverallDirection is OperationalRecoveryDirection.Degrading
                or OperationalRecoveryDirection.Diverging
            && situationRoom.StabilizationDirection is OperationalSituationDirection.Degrading
                or OperationalSituationDirection.Escalating)
            return OperationalConsistencyDirection.Diverging;

        if (recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging
            && situationRoom.StabilizationDirection is OperationalSituationDirection.Improving
                or OperationalSituationDirection.Stabilizing)
            return OperationalConsistencyDirection.Aligning;

        return OperationalConsistencyDirection.Stable;
    }

    private static string ComposeOperatorSummary(
        OperationalIntegrityState integrityState,
        int consistencyScore,
        int contradictionCount,
        int alignmentCount,
        string dominantNarrative,
        IReadOnlyList<OperationalIntegritySnapshot> priorSnapshots)
    {
        var continuity = string.Empty;
        if (priorSnapshots.Count > 0)
        {
            var prior = priorSnapshots[^1];
            if (prior.IntegrityState != integrityState)
            {
                continuity = " " + OperationalContinuityPhrasing.MovedFromTo(
                    "Integrity state",
                    prior.IntegrityState.ToString().ToLowerInvariant(),
                    integrityState.ToString().ToLowerInvariant()) + ".";
            }
        }

        return
            $"Operational interpretation integrity is {integrityState.ToString().ToLowerInvariant()} " +
            $"(score {consistencyScore}) with {alignmentCount} alignment(s) and {contradictionCount} contradiction(s). " +
            $"Dominant narrative: {dominantNarrative.ToLowerInvariant()}.{continuity}";
    }

    private static bool IsReplayStabilizationTriad(
        string dominantArea,
        string leverageArea,
        string playbookArea,
        OperationalPlaybooksDto playbooks)
    {
        if (!AreasMatch(dominantArea, AreaReplay)
            || !AreasMatch(leverageArea, AreaReplay)
            || !AreasMatch(playbookArea, AreaReplay))
            return false;

        return playbooks.Playbooks.Any(p =>
            p.PlaybookId.Contains("replay", StringComparison.OrdinalIgnoreCase)
            || string.Equals(p.DominantArea, AreaReplay, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsRecoveryDirectionCoherent(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom)
    {
        return recovery.OverallDirection switch
        {
            OperationalRecoveryDirection.Improving or OperationalRecoveryDirection.Converging
                => situationRoom.StabilizationDirection is OperationalSituationDirection.Improving
                    or OperationalSituationDirection.Stabilizing
                    or OperationalSituationDirection.Stable,
            OperationalRecoveryDirection.Degrading or OperationalRecoveryDirection.Diverging
                => situationRoom.StabilizationDirection is OperationalSituationDirection.Degrading
                    or OperationalSituationDirection.Escalating,
            _ => situationRoom.StabilizationDirection is OperationalSituationDirection.Stable
        };
    }

    private static bool IsPropagationConsistentWithRecovery(
        OperationalPropagationAnalysisDto propagation,
        OperationalRecoveryPostureDto recovery)
    {
        if (CountEscalatingPropagations(propagation) == 0)
            return true;

        return recovery.OverallDirection is OperationalRecoveryDirection.Degrading
            or OperationalRecoveryDirection.Diverging;
    }

    private static bool IsRuntimeNarrativeCoherent(
        OperationalRecoveryPostureDto recovery,
        OperationalPatternSummaryDto patternSummary,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalPatternsDto patterns)
    {
        var runtimePattern = patterns.Patterns.Any(p =>
            p.PatternId.Contains("runtime", StringComparison.OrdinalIgnoreCase)
            || string.Equals(p.DominantArea, AreaRuntime, StringComparison.OrdinalIgnoreCase));

        var runtimeArchetype = patternSummary.DominantArchetype.Contains("runtime", StringComparison.OrdinalIgnoreCase)
            || patternSummary.HighestRiskPattern.Contains("runtime", StringComparison.OrdinalIgnoreCase);

        var incidentPressure = incidentSummary.EscalatingIncidentCount > 0
            || incidentSummary.RecurringIncidentCount > 0;

        var recoveryDeclining = recovery.OverallDirection is OperationalRecoveryDirection.Degrading
            or OperationalRecoveryDirection.Diverging;

        return (runtimePattern || runtimeArchetype) && incidentPressure && recoveryDeclining;
    }

    private static bool NarrativeAreasAgree(string dominantArea, string priorityFocus, string dominantArchetype)
    {
        if (string.IsNullOrWhiteSpace(priorityFocus) && string.IsNullOrWhiteSpace(dominantArchetype))
            return false;

        return priorityFocus.Contains(dominantArea, StringComparison.OrdinalIgnoreCase)
            || dominantArchetype.Contains(dominantArea, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRecoveryContradiction(
        OperationalRecoveryPostureDto recovery,
        OperationalSimulationScenariosDto simulation,
        OperationalSimulationOutlookDto simulationOutlook,
        OperationalSituationRoomDto situationRoom)
    {
        var recoveryImproving = recovery.OverallDirection is OperationalRecoveryDirection.Improving
            or OperationalRecoveryDirection.Converging;

        var expandingDegradation = simulation.DegradationPaths.Count >= 2
            || simulationOutlook.HighestRiskDegradationPath.Contains(
                "expand",
                StringComparison.OrdinalIgnoreCase)
            || simulationOutlook.HighestRiskDegradationPath.Contains(
                "degrad",
                StringComparison.OrdinalIgnoreCase);

        var escalationWorsening = situationRoom.StabilizationDirection == OperationalSituationDirection.Escalating
            || situationRoom.EscalationSeverity >= OperationalExecutiveSeverity.High;

        return recoveryImproving && expandingDegradation && escalationWorsening;
    }

    private static bool IsEscalationRecoveryConflict(
        OperationalRecoveryPostureDto recovery,
        OperationalSituationRoomDto situationRoom,
        OperationalPropagationAnalysisDto propagation)
    {
        var recoveryImproving = recovery.OverallDirection is OperationalRecoveryDirection.Improving
            or OperationalRecoveryDirection.Converging;

        var escalationHigh = situationRoom.EscalationSeverity >= OperationalExecutiveSeverity.Elevated
            || situationRoom.StabilizationDirection == OperationalSituationDirection.Escalating
            || CountEscalatingPropagations(propagation) >= 2;

        return recoveryImproving && escalationHigh;
    }

    private static bool IsPlaybookRecoveryDivergence(
        OperationalRecoveryPostureDto recovery,
        OperationalPlaybooksDto playbooks)
    {
        var recoveryDegrading = recovery.OverallDirection is OperationalRecoveryDirection.Degrading
            or OperationalRecoveryDirection.Diverging;

        var recoveryAccelerationPlaybook = playbooks.Playbooks.Any(p =>
            p.PlaybookId.Contains("recovery-acceleration", StringComparison.OrdinalIgnoreCase)
            || p.PlaybookId.Contains("recovery", StringComparison.OrdinalIgnoreCase));

        return recoveryDegrading && recoveryAccelerationPlaybook;
    }

    private static bool IsDominantAreaConflict(string dominantArea, string leverageArea, string playbookArea)
    {
        if (string.IsNullOrWhiteSpace(dominantArea)
            || string.IsNullOrWhiteSpace(leverageArea)
            || string.IsNullOrWhiteSpace(playbookArea))
            return false;

        return !AreasMatch(dominantArea, leverageArea)
            && !AreasMatch(dominantArea, playbookArea)
            && !AreasMatch(leverageArea, playbookArea);
    }

    private static bool IsPropagationRecoveryConflict(
        OperationalPropagationAnalysisDto propagation,
        OperationalRecoveryPostureDto recovery)
    {
        return CountEscalatingPropagations(propagation) >= 2
            && recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging;
    }

    private static bool IsNarrativeContradiction(
        OperationalSituationRoomDto situationRoom,
        OperationalIncidentCasesSummaryDto incidentSummary,
        OperationalRecoveryPostureDto recovery)
    {
        var stabilizingNarrative = situationRoom.StabilizationDirection is OperationalSituationDirection.Improving
            or OperationalSituationDirection.Stabilizing;

        var incidentEscalating = incidentSummary.EscalatingIncidentCount > 0;
        var recoveryDegrading = recovery.OverallDirection is OperationalRecoveryDirection.Degrading
            or OperationalRecoveryDirection.Diverging;

        return stabilizingNarrative && incidentEscalating && recoveryDegrading;
    }

    private static string ResolveTopPlaybookArea(OperationalPlaybooksDto playbooks)
    {
        return playbooks.Playbooks
            .OrderByDescending(p => p.Severity)
            .ThenBy(p => p.DominantArea, StringComparer.Ordinal)
            .FirstOrDefault()?.DominantArea ?? AreaOperational;
    }

    private static int CountEscalatingPropagations(OperationalPropagationAnalysisDto propagation)
    {
        return propagation.Propagations.Count(p => p.IsEscalating);
    }

    private static string NormalizeArea(string area)
    {
        return string.IsNullOrWhiteSpace(area) ? AreaOperational : area.Trim();
    }

    private static bool AreasMatch(string left, string right)
    {
        return string.Equals(NormalizeArea(left), NormalizeArea(right), StringComparison.OrdinalIgnoreCase);
    }
}
