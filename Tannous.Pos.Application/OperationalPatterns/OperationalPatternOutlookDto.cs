using Tannous.Pos.Application.OperationalRecovery;

namespace Tannous.Pos.Application.OperationalPatterns;

/// <summary>Platform-wide operational pattern outlook.</summary>
public sealed class OperationalPatternOutlookDto
{
    public string DominantPattern { get; init; } = string.Empty;
    public string EmergingPattern { get; init; } = string.Empty;
    public string RecoveryPattern { get; init; } = string.Empty;
    public string EscalationPattern { get; init; } = string.Empty;
    public string StabilizationPattern { get; init; } = string.Empty;
    public OperationalRecoveryConfidence OperationalConfidence { get; init; }
}
