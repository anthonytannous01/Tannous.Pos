namespace Tannous.Pos.Application.OperationalNavigation;

/// <summary>Operator navigation route entry (existing diagnostics routes only).</summary>
public sealed class OperationalNavigationRouteDto
{
    public string DisplayName { get; init; } = string.Empty;
    public string RelativeRoute { get; init; } = string.Empty;
    public OperationalNavigationSeverity Severity { get; init; }
    public OperationalNavigationState AttentionState { get; init; }
    public string OperatorSummary { get; init; } = string.Empty;
}
