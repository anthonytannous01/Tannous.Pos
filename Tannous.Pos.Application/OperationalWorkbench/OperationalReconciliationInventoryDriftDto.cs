namespace Tannous.Pos.Application.OperationalWorkbench;

/// <summary>Operator-facing inventory drift visibility for reconciliation workflow.</summary>
public sealed class OperationalReconciliationInventoryDriftDto
{
    public OperationalWorkbenchSeverity DriftSeverity { get; init; }
    public int ActiveInventoryMismatchCount { get; init; }
    public OperationalWorkbenchAttentionState AttentionState { get; init; }
    public bool ManualReviewRecommended { get; init; }
    public string Summary { get; init; } = string.Empty;
}
