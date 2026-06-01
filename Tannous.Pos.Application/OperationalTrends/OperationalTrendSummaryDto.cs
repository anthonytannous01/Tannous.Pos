namespace Tannous.Pos.Application.OperationalTrends;

/// <summary>
/// Operator short-window trend summary (read-only; advisory interpretation).
/// NON-GOAL: not governance infrastructure; not forecasting; no persistence.
/// </summary>
public sealed class OperationalTrendSummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalTrendDirection OverallDirection { get; init; }
    public OperationalTrendSeverity Severity { get; init; }
    public string Summary { get; init; } = string.Empty;
    public OperationalTrendWindowDto Window { get; init; } = new();
    public IReadOnlyList<OperationalTrendAttentionDto> AttentionItems { get; init; } = Array.Empty<OperationalTrendAttentionDto>();
    public string TrendNote { get; init; } =
        "Advisory short-window operational trend interpretation composed from existing diagnostics. Not a forecast and not persisted.";
}
