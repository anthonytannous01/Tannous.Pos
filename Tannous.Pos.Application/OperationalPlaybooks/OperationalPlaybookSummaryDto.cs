using Tannous.Pos.Application.OperationalSituationRoom;

namespace Tannous.Pos.Application.OperationalPlaybooks;

/// <summary>Platform-wide operational playbook summary.</summary>
public sealed class OperationalPlaybookSummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int ActivePlaybookCount { get; init; }
    public int EscalationGuidanceCount { get; init; }
    public int StabilizationGuidanceCount { get; init; }
    public string HighestPriorityArea { get; init; } = string.Empty;
    public string DominantRecoveryConstraint { get; init; } = string.Empty;
    public string RecoveryReadiness { get; init; } = string.Empty;
    public OperationalAttentionLevel OperatorAttentionLevel { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string PlaybookNote { get; init; } =
        "Advisory deterministic operational response guidance composed from existing diagnostics. Sequencing only — not workflow execution, automation, or remediation.";
}
