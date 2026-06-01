using Tannous.Pos.Application.OperationalRecovery;

namespace Tannous.Pos.Application.OperationalSituationRoom;

/// <summary>Concise executive operational briefing.</summary>
public sealed class OperationalExecutiveBriefingDto
{
    public string Headline { get; init; } = string.Empty;
    public string Situation { get; init; } = string.Empty;
    public string DominantRisk { get; init; } = string.Empty;
    public string RecoveryOutlook { get; init; } = string.Empty;
    public string EscalationStatus { get; init; } = string.Empty;
    public string RecommendedAction { get; init; } = string.Empty;
    public OperationalRecoveryConfidence Confidence { get; init; }
    public string Summary { get; init; } = string.Empty;
}
