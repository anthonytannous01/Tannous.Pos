namespace Tannous.Pos.Application.OperationalSimulation;

/// <summary>Bounded hypothetical scenarios, paths, and leverage analysis.</summary>
public sealed class OperationalSimulationScenariosDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int ScenarioCount { get; init; }
    public int StabilizationPathCount { get; init; }
    public int DegradationPathCount { get; init; }
    public int LeveragePointCount { get; init; }
    public IReadOnlyList<OperationalSimulationScenarioDto> Scenarios { get; init; } = Array.Empty<OperationalSimulationScenarioDto>();
    public IReadOnlyList<OperationalStabilizationPathDto> StabilizationPaths { get; init; } = Array.Empty<OperationalStabilizationPathDto>();
    public IReadOnlyList<OperationalDegradationPathDto> DegradationPaths { get; init; } = Array.Empty<OperationalDegradationPathDto>();
    public IReadOnlyList<OperationalLeveragePointDto> LeveragePoints { get; init; } = Array.Empty<OperationalLeveragePointDto>();
    public string SimulationNote { get; init; } =
        "Advisory deterministic hypothetical analysis composed from existing diagnostics. Heuristic what-if only — not prediction, ML, or optimization.";
}
