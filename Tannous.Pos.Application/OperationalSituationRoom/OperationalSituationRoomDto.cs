using Tannous.Pos.Application.OperationalRecovery;

namespace Tannous.Pos.Application.OperationalSituationRoom;

/// <summary>Deterministic operational situation room briefing snapshot.</summary>
public sealed class OperationalSituationRoomDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalSituationState PlatformCondition { get; init; }
    public string DominantOperationalRisk { get; init; } = string.Empty;
    public OperationalSituationDirection StabilizationDirection { get; init; }
    public OperationalRecoveryConfidence RecoveryConfidence { get; init; }
    public OperationalExecutiveSeverity EscalationSeverity { get; init; }
    public int ActiveIncidentCount { get; init; }
    public int EscalatingPropagationCount { get; init; }
    public string HighestPriorityFocus { get; init; } = string.Empty;
    public string RecommendedOperationalFocus { get; init; } = string.Empty;
    public string OperatorSummary { get; init; } = string.Empty;
    public string ExecutiveSummary { get; init; } = string.Empty;
    public string OperationalNarrative { get; init; } = string.Empty;
    public OperationalAttentionLevel AttentionLevel { get; init; }
    public string Outlook { get; init; } = string.Empty;
    public OperationalSituationOutlookDto OutlookDetail { get; init; } = new();
    public IReadOnlyList<OperationalNarrativeDto> Narratives { get; init; } = Array.Empty<OperationalNarrativeDto>();
    public IReadOnlyList<OperationalRiskConcentrationDto> RiskConcentrations { get; init; } =
        Array.Empty<OperationalRiskConcentrationDto>();
    public string SituationNote { get; init; } =
        "Advisory deterministic operational briefing composed from existing diagnostics. Not AI summarization, ticketing, or workflow management.";
}
