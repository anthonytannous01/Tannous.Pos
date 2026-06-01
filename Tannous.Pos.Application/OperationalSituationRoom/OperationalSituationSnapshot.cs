namespace Tannous.Pos.Application.OperationalSituationRoom;

/// <summary>Lightweight process-local situation briefing snapshot for continuity.</summary>
public sealed class OperationalSituationSnapshot
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalSituationState PlatformCondition { get; init; }
    public string DominantOperationalRisk { get; init; } = string.Empty;
    public OperationalSituationDirection StabilizationDirection { get; init; }
    public int ActiveIncidentCount { get; init; }
    public int EscalatingPropagationCount { get; init; }
    public OperationalAttentionLevel AttentionLevel { get; init; }
    public string OperatorSummary { get; init; } = string.Empty;
}
