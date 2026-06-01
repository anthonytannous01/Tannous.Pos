namespace Tannous.Pos.Application.OperationalPatterns;

/// <summary>Lightweight process-local pattern snapshot for continuity.</summary>
public sealed class OperationalPatternSnapshot
{
    public DateTime GeneratedAtUtc { get; init; }
    public int PatternCount { get; init; }
    public int RecurringPatternCount { get; init; }
    public string DominantArchetype { get; init; } = string.Empty;
    public string DominantArea { get; init; } = string.Empty;
    public string HighestRiskPattern { get; init; } = string.Empty;
    public string OperatorSummary { get; init; } = string.Empty;
}
