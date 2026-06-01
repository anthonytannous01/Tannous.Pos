namespace Tannous.Pos.Application.OperationalRecovery;

/// <summary>Bounded stabilization outlook across operational domains.</summary>
public sealed class OperationalRecoveryOutlookDto
{
    public OperationalRecoveryState OverallState { get; init; }
    public OperationalRecoveryDirection OverallDirection { get; init; }
    public OperationalRecoveryConfidence OverallConfidence { get; init; }
    public OperationalRecoverySeverity OverallSeverity { get; init; }
    public string Summary { get; init; } = string.Empty;
    public int SectionCount { get; init; }
    public int ConvergenceCount { get; init; }
    public IReadOnlyList<OperationalRecoveryOutlookSectionDto> Sections { get; init; } = Array.Empty<OperationalRecoveryOutlookSectionDto>();
    public IReadOnlyList<OperationalRecoveryConvergenceDto> Convergence { get; init; } = Array.Empty<OperationalRecoveryConvergenceDto>();
}
