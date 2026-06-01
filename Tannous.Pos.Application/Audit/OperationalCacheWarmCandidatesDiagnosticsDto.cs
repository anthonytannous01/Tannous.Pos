namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCacheWarmCandidatesDiagnosticsDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int WarmCandidateCount { get; init; }
    public IReadOnlyList<OperationalCacheWarmCandidateDto> Candidates { get; init; } = Array.Empty<OperationalCacheWarmCandidateDto>();
    public string GovernanceNote { get; init; } = string.Empty;
}
