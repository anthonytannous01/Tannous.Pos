namespace Tannous.Pos.Application.OperationalTrends;

/// <summary>Operator trend attention item (advisory interpretation only).</summary>
public sealed class OperationalTrendAttentionDto
{
    public int Priority { get; init; }
    public OperationalTrendSeverity Severity { get; init; }
    public OperationalTrendDirection Direction { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
}
