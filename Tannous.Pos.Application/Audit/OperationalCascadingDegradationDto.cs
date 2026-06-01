namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCascadingDegradationDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int CascadingPatternCount { get; init; }
    public IReadOnlyList<CascadingDegradationPatternDto> Patterns { get; init; } = Array.Empty<CascadingDegradationPatternDto>();
}

public sealed class CascadingDegradationPatternDto
{
    public string CorrelationKey { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public IReadOnlyList<string> Subsystems { get; init; } = Array.Empty<string>();
    public string CausalityHint { get; init; } = string.Empty;
    public int SignalCount { get; init; }
}
