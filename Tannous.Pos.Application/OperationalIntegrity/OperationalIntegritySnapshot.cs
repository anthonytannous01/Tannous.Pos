namespace Tannous.Pos.Application.OperationalIntegrity;

/// <summary>Lightweight process-local integrity snapshot for short-term continuity.</summary>
public sealed class OperationalIntegritySnapshot
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalIntegrityState IntegrityState { get; init; }
    public int ConsistencyScore { get; init; }
    public int ContradictionCount { get; init; }
    public int AlignmentCount { get; init; }
    public string DominantOperationalNarrative { get; init; } = string.Empty;
    public string AlignmentState { get; init; } = string.Empty;
}
