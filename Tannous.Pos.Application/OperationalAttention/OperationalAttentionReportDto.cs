namespace Tannous.Pos.Application.OperationalAttention;

/// <summary>Deterministic operational attention and priority coordination report.</summary>
public sealed class OperationalAttentionReportDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalPriorityType DominantOperationalPriority { get; init; }
    public string HighestUrgencyArea { get; init; } = string.Empty;
    public string StabilizationFocusArea { get; init; } = string.Empty;
    public string EscalationFocusArea { get; init; } = string.Empty;
    public string InvestigationPriorityArea { get; init; } = string.Empty;
    public OperationalUrgencyLevel AttentionPressureLevel { get; init; }
    public IReadOnlyList<OperationalPriorityDto> Priorities { get; init; } = Array.Empty<OperationalPriorityDto>();
    public IReadOnlyList<OperationalAttentionCoordinationDto> AttentionCoordination { get; init; } =
        Array.Empty<OperationalAttentionCoordinationDto>();
    public IReadOnlyList<OperationalEmphasisDto> OperationalEmphasis { get; init; } =
        Array.Empty<OperationalEmphasisDto>();
    public OperationalAttentionContinuityDto AttentionContinuity { get; init; } = new();
    public string OperatorSummary { get; init; } = string.Empty;
    public string AttentionNote { get; init; } =
        "Advisory deterministic operational attention coordination from bounded cognition continuity. Not workflow orchestration, alerting, or AI prioritization.";
}
