namespace Tannous.Pos.Application.Audit.Governance;

public sealed class OperationalGovernanceProjectionTimingDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int BuildElapsedMilliseconds { get; init; }
    public string TimingBand { get; init; } = string.Empty;
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
