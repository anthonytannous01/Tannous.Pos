namespace Tannous.Pos.Application.OperationalTriage;

/// <summary>
/// Unified operator triage queue (read-only; advisory investigation prioritization).
/// NON-GOAL: not workflow engine; not task assignment; no persistence.
/// </summary>
public sealed class OperationalTriageQueueDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int ItemCount { get; init; }
    public int MaxItems { get; init; } = OperationalTriageAggregation.MaxTriageItems;
    public OperationalTriagePriority OverallPriority { get; init; }
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<OperationalTriageItemDto> Items { get; init; } = Array.Empty<OperationalTriageItemDto>();
    public IReadOnlyList<OperationalTriageAttentionDto> AttentionItems { get; init; } = Array.Empty<OperationalTriageAttentionDto>();
    public IReadOnlyList<OperationalTriageCorrelationDto> Correlations { get; init; } = Array.Empty<OperationalTriageCorrelationDto>();
    public string TriageNote { get; init; } =
        "Advisory operator triage queue composed from existing diagnostics. Investigation order is guidance only — no tasks are assigned or executed.";
}
