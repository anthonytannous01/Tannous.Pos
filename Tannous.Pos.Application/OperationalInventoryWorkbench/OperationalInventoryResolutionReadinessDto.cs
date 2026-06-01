namespace Tannous.Pos.Application.OperationalInventoryWorkbench;

/// <summary>Operator-facing resolution readiness visibility (advisory; no automation).</summary>
public sealed class OperationalInventoryResolutionReadinessDto
{
    public OperationalInventoryResolutionState ResolutionState { get; init; }
    public bool ReadyForOperatorReview { get; init; }
    public bool StabilizationInProgress { get; init; }
    public bool BlockedByReplayPressure { get; init; }
    public bool BlockedByProtectiveMode { get; init; }
    public bool ManualReconciliationRecommended { get; init; }
    public string Summary { get; init; } = string.Empty;
}
