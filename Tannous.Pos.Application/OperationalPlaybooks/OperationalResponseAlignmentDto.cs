namespace Tannous.Pos.Application.OperationalPlaybooks;

/// <summary>Cross-layer operational response alignment summary.</summary>
public sealed class OperationalResponseAlignmentDto
{
    public string IncidentAlignment { get; init; } = string.Empty;
    public string RecoveryAlignment { get; init; } = string.Empty;
    public string CausalityAlignment { get; init; } = string.Empty;
    public string SimulationAlignment { get; init; } = string.Empty;
    public string SituationRoomAlignment { get; init; } = string.Empty;
    public string OperationalConsistency { get; init; } = string.Empty;
}
