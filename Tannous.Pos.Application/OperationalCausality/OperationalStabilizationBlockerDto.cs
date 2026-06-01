namespace Tannous.Pos.Application.OperationalCausality;

/// <summary>Stabilization blocker preventing operational recovery convergence.</summary>
public sealed class OperationalStabilizationBlockerDto
{
    public string Area { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public OperationalCausalitySeverity Severity { get; init; }
    public bool PreventingRecovery { get; init; }
    public string EscalationRisk { get; init; } = string.Empty;
    public string SuggestedOperatorFocus { get; init; } = string.Empty;
}
