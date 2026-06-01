namespace Tannous.Pos.Application.OperationalPatterns;

/// <summary>Bounded operational patterns, correlations, sequences, and outlook.</summary>
public sealed class OperationalPatternsDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int PatternCount { get; init; }
    public int CorrelationCount { get; init; }
    public int SequenceCount { get; init; }
    public IReadOnlyList<OperationalPatternDto> Patterns { get; init; } = Array.Empty<OperationalPatternDto>();
    public IReadOnlyList<OperationalPatternCorrelationDto> Correlations { get; init; } = Array.Empty<OperationalPatternCorrelationDto>();
    public IReadOnlyList<OperationalPatternSequenceDto> Sequences { get; init; } = Array.Empty<OperationalPatternSequenceDto>();
    public OperationalPatternOutlookDto Outlook { get; init; } = new();
    public string PatternNote { get; init; } =
        "Advisory deterministic pattern interpretation composed from bounded process-local continuity. Not ML, anomaly detection, or adaptive learning.";
}
