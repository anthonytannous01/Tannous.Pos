namespace Tannous.Pos.Application.OperationalWorkbench;

/// <summary>
/// Operator reconciliation workbench read model (read-only; advisory workflow visibility).
/// NON-GOAL: not governance infrastructure; not authoritative business truth; no mutations.
/// </summary>
public sealed class OperationalReconciliationWorkbenchDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalReconciliationQueueDto Queue { get; init; } = new();
    public IReadOnlyList<OperationalReconciliationHotspotDto> Hotspots { get; init; } = Array.Empty<OperationalReconciliationHotspotDto>();
    public OperationalReconciliationReplayRiskDto ReplayRisk { get; init; } = new();
    public OperationalReconciliationInventoryDriftDto InventoryDrift { get; init; } = new();
    public IReadOnlyList<OperationalReconciliationAttentionItemDto> AttentionItems { get; init; } = Array.Empty<OperationalReconciliationAttentionItemDto>();
    public string WorkbenchNote { get; init; } =
        "Advisory reconciliation workbench composed from existing diagnostics. Review items are guidance only — no automated actions are taken.";
}
