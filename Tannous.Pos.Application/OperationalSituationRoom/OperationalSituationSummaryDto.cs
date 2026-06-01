using Tannous.Pos.Application.OperationalRecovery;

namespace Tannous.Pos.Application.OperationalSituationRoom;

/// <summary>Platform-wide operational situation summary.</summary>
public sealed class OperationalSituationSummaryDto
{
    public OperationalSituationState PlatformState { get; init; }
    public string DominantArea { get; init; } = string.Empty;
    public OperationalSituationDirection OverallRecoveryDirection { get; init; }
    public OperationalExecutiveSeverity EscalationPressure { get; init; }
    public OperationalRecoveryConfidence RecoveryConfidence { get; init; }
    public OperationalAttentionLevel OperatorAttentionLevel { get; init; }
    public bool ExecutiveAttentionRequired { get; init; }
}
