namespace Tannous.Pos.Application.OperationalPlaybooks;

/// <summary>Lightweight process-local playbook snapshot for continuity.</summary>
public sealed class OperationalPlaybookSnapshot
{
    public DateTime GeneratedAtUtc { get; init; }
    public int PlaybookCount { get; init; }
    public int EscalationGuidanceCount { get; init; }
    public string HighestPriorityArea { get; init; } = string.Empty;
    public string DominantConstraint { get; init; } = string.Empty;
    public string OperatorSummary { get; init; } = string.Empty;
}
