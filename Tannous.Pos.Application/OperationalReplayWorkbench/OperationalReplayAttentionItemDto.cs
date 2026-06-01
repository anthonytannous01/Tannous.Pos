namespace Tannous.Pos.Application.OperationalReplayWorkbench;

/// <summary>Priority-ordered advisory attention item for replay operators.</summary>
public sealed class OperationalReplayAttentionItemDto
{
    public int Priority { get; init; }
    public OperationalReplayPressureLevel Severity { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Guidance { get; init; } = string.Empty;
}
