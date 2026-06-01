namespace Tannous.Pos.Application.OperationalSimulation;

/// <summary>Lightweight process-local simulation snapshot for continuity.</summary>
public sealed class OperationalSimulationSnapshot
{
    public DateTime GeneratedAtUtc { get; init; }
    public int ScenarioCount { get; init; }
    public int StabilizationScenarioCount { get; init; }
    public int DegradationScenarioCount { get; init; }
    public string HighestLeverageArea { get; init; } = string.Empty;
    public string DominantConstraint { get; init; } = string.Empty;
    public string OperatorSummary { get; init; } = string.Empty;
}
