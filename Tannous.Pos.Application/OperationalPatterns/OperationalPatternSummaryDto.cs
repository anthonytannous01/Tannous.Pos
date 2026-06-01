using Tannous.Pos.Application.OperationalSituationRoom;

namespace Tannous.Pos.Application.OperationalPatterns;

/// <summary>Platform-wide operational pattern summary.</summary>
public sealed class OperationalPatternSummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int ActivePatternCount { get; init; }
    public int RecurringPatternCount { get; init; }
    public string DominantArchetype { get; init; } = string.Empty;
    public string HighestRiskPattern { get; init; } = string.Empty;
    public OperationalPatternConfidence RecoveryPatternStrength { get; init; }
    public OperationalPatternConfidence EscalationPatternStrength { get; init; }
    public OperationalAttentionLevel OperatorAttentionLevel { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string PatternNote { get; init; } =
        "Advisory deterministic pattern interpretation composed from bounded process-local continuity. Not ML, anomaly detection, or adaptive learning.";
}
