namespace Tannous.Pos.Application.Audit.Governance;

public sealed class OperationalGovernanceRuntimeBaselineDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string ExecutionBudgetState { get; init; } = string.Empty;
    public OperationalGovernanceProjectionTimingDto ProjectionTiming { get; init; } = new();
    public double SnapshotReuseRatio { get; init; }
    public int ProjectionCollaboratorCount { get; init; }
    public int PipelineStageCount { get; init; }
    public long ExplainabilityTruncations { get; init; }
    public long RuntimeFailsafeActivations { get; init; }
    public long RuntimeBudgetConstrainedEvents { get; init; }
    public IReadOnlyList<string> BaselineSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
