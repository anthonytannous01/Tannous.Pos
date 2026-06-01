namespace Tannous.Pos.Application.OperationalIncidents;

/// <summary>Platform-wide incident case summary for operator attention.</summary>
public sealed class OperationalIncidentCasesSummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int ActiveIncidentCount { get; init; }
    public int EscalatingIncidentCount { get; init; }
    public int RecoveringIncidentCount { get; init; }
    public int RecurringIncidentCount { get; init; }
    public OperationalIncidentSeverity HighestSeverity { get; init; }
    public string PlatformStabilityState { get; init; } = string.Empty;
    public string OperatorAttentionLevel { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string IncidentNote { get; init; } =
        "Advisory process-local incident cases composed from existing diagnostics. Cases are ephemeral groupings — not tickets, assignments, or persisted investigations.";
}
