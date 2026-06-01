using Tannous.Pos.Application.OperationalBriefing;

namespace Tannous.Pos.Application.OperationalHandoff;

/// <summary>Compact handoff summary for shift transition consumption.</summary>
public sealed class OperationalHandoffSummaryDto
{
    public Guid HandoffId { get; init; } = Guid.NewGuid();
    public DateTime GeneratedAtUtc { get; init; }
    public BriefingCognitionAge CognitionAge { get; init; }
    public HandoffContinuityTransition EquilibriumTransition { get; init; }
    public HandoffContinuityTransition StrategyTransition { get; init; }
    public HandoffContinuityTransition AttentionTransition { get; init; }
    public int SnapshotWindowCount { get; init; }
    public string CurrentBriefingSummary { get; init; } = string.Empty;
    public string HandoffNarrative { get; init; } = string.Empty;
}
