namespace Tannous.Pos.Application.OperationalNavigation;

/// <summary>Operator navigation attention item (advisory guidance only).</summary>
public sealed class OperationalNavigationAttentionDto
{
    public int Priority { get; init; }
    public OperationalNavigationSeverity Severity { get; init; }
    public OperationalNavigationState State { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public string RelativeRoute { get; init; } = string.Empty;
}
