namespace Tannous.Pos.Application.OperationalInventoryWorkbench;

/// <summary>Priority-ordered advisory attention item for inventory drift operators.</summary>
public sealed class OperationalInventoryAttentionItemDto
{
    public int Priority { get; init; }
    public OperationalInventoryDriftSeverity Severity { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Guidance { get; init; } = string.Empty;
}
