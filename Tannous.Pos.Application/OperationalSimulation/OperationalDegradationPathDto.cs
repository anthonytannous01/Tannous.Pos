namespace Tannous.Pos.Application.OperationalSimulation;

/// <summary>Deterministic hypothetical degradation path.</summary>
public sealed class OperationalDegradationPathDto
{
    public string PathId { get; init; } = string.Empty;
    public string SourceArea { get; init; } = string.Empty;
    public string ExpectedPropagation { get; init; } = string.Empty;
    public OperationalSimulationSeverity EscalationRisk { get; init; }
    public OperationalSimulationSeverity RecoveryRisk { get; init; }
    public IReadOnlyList<string> DownstreamAreas { get; init; } = Array.Empty<string>();
    public OperationalSimulationSeverity OperationalSeverity { get; init; }
    public string OperatorSummary { get; init; } = string.Empty;
}
