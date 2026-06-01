namespace Tannous.Pos.Application.OperationalCausality;

/// <summary>Propagation analysis with root-cause candidates and stabilization blockers.</summary>
public sealed class OperationalPropagationAnalysisDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int PropagationCount { get; init; }
    public int RootCauseCandidateCount { get; init; }
    public int StabilizationBlockerCount { get; init; }
    public IReadOnlyList<OperationalPressurePropagationDto> Propagations { get; init; } =
        Array.Empty<OperationalPressurePropagationDto>();
    public IReadOnlyList<OperationalRootCauseCandidateDto> RootCauseCandidates { get; init; } =
        Array.Empty<OperationalRootCauseCandidateDto>();
    public IReadOnlyList<OperationalStabilizationBlockerDto> StabilizationBlockers { get; init; } =
        Array.Empty<OperationalStabilizationBlockerDto>();
}
