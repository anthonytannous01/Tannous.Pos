namespace Tannous.Pos.Application.OperationalTopology;

/// <summary>Deterministic operational dependency between two operational areas.</summary>
public sealed class OperationalDependencyDto
{
    public string SourceArea { get; init; } = string.Empty;
    public string TargetArea { get; init; } = string.Empty;
    public OperationalDependencyType DependencyType { get; init; }
    public string InfluenceStrength { get; init; } = string.Empty;
    public string StabilizationInfluence { get; init; } = string.Empty;
    public string EscalationInfluence { get; init; } = string.Empty;
    public OperationalCriticalityLevel OperationalCriticality { get; init; }
    public string OperatorInterpretation { get; init; } = string.Empty;
}
