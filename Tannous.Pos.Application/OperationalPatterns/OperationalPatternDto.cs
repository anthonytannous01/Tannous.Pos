namespace Tannous.Pos.Application.OperationalPatterns;

/// <summary>Deterministic recurring operational pattern interpretation.</summary>
public sealed class OperationalPatternDto
{
    public string PatternId { get; init; } = string.Empty;
    public OperationalPatternType PatternType { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string DominantArea { get; init; } = string.Empty;
    public OperationalPatternDirection StabilityDirection { get; init; }
    public OperationalPatternSeverity Severity { get; init; }
    public int Frequency { get; init; }
    public OperationalPatternConfidence RecurrenceConfidence { get; init; }
    public string OperationalImpact { get; init; } = string.Empty;
    public string RecoveryAlignment { get; init; } = string.Empty;
    public string OperatorSummary { get; init; } = string.Empty;
}
