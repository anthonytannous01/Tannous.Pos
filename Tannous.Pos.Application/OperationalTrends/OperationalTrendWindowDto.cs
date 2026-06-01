namespace Tannous.Pos.Application.OperationalTrends;

/// <summary>Short-window process-local trend retention visibility (advisory only).</summary>
public sealed class OperationalTrendWindowDto
{
    public int SnapshotCount { get; init; }
    public int MaxSnapshots { get; init; } = OperationalTrendAggregation.MaxWindowSnapshots;
    public bool HasComparisonBaseline { get; init; }
    public string WindowNote { get; init; } =
        "Process-local short-window trend comparison. Snapshots are retained in-memory only and are not persisted.";
}
