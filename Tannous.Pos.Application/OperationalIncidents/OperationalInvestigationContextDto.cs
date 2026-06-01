namespace Tannous.Pos.Application.OperationalIncidents;

/// <summary>Cross-area investigation alignment context for an incident case.</summary>
public sealed class OperationalInvestigationContextDto
{
    public string TimelineCorrelation { get; init; } = string.Empty;
    public string ReplayAlignment { get; init; } = string.Empty;
    public string RecoveryAlignment { get; init; } = string.Empty;
    public string RuntimePressureAlignment { get; init; } = string.Empty;
    public string DriftAlignment { get; init; } = string.Empty;
    public string TriageAlignment { get; init; } = string.Empty;
    public string FingerprintAlignment { get; init; } = string.Empty;
}
