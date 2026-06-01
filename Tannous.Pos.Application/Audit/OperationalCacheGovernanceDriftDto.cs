namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCacheGovernanceDriftDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public bool DriftDetected { get; init; }
    public OperationalCacheGovernanceDriftSeverity DriftSeverity { get; init; }
    public IReadOnlyList<string> DriftSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
