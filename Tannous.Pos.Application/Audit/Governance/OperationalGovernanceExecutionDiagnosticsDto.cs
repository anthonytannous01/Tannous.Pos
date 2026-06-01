namespace Tannous.Pos.Application.Audit.Governance;

public sealed class OperationalGovernanceExecutionDiagnosticsDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string ExecutionState { get; init; } = string.Empty;
    public string BudgetPressure { get; init; } = string.Empty;
    public string ProjectionComplexity { get; init; } = string.Empty;
    public int StabilityScore { get; init; }
    public string PressureSeverity { get; init; } = string.Empty;
    public long TotalInvalidations { get; init; }
    public long TotalBypasses { get; init; }
    public int ActiveTelemetryCategories { get; init; }
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
