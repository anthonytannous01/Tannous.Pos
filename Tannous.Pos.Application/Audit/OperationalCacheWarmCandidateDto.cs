namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCacheWarmCandidateDto
{
    public string Category { get; init; } = string.Empty;
    public long HitCount { get; init; }
    public long MissCount { get; init; }
    public long RepeatedColdMissCount { get; init; }
    public string AdvisoryNote { get; init; } = string.Empty;
}
