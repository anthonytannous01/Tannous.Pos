namespace Tannous.Pos.Application.OperationalIncidents;

/// <summary>Correlated signal contributing to an operational incident case.</summary>
public sealed class OperationalIncidentSignalDto
{
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public OperationalIncidentSeverity Severity { get; init; }
    public OperationalIncidentDirection Direction { get; init; }
    public bool IsStabilizing { get; init; }
    public string SourceArea { get; init; } = string.Empty;
}
