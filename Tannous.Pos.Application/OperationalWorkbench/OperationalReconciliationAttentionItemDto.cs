namespace Tannous.Pos.Application.OperationalWorkbench;

/// <summary>Priority-ordered advisory attention item for reconciliation operators.</summary>
public sealed class OperationalReconciliationAttentionItemDto
{
    public int Priority { get; init; }
    public OperationalWorkbenchAttentionState AttentionState { get; init; }
    public OperationalWorkbenchSeverity Severity { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Guidance { get; init; } = string.Empty;
}
