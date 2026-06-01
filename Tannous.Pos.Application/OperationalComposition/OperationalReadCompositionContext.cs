namespace Tannous.Pos.Application.OperationalComposition;

/// <summary>Request-scoped operational read composition diagnostics (internal metrics only).</summary>
public sealed class OperationalReadCompositionContext
{
    public long CompositionReuseHits { get; private set; }
    public long CompositionReuseMisses { get; private set; }
    public long CompositionNestedReadAvoidanceCount { get; private set; }
    public long CompositionSnapshotBuilds { get; private set; }

    public double CompositionReuseRatio =>
        CompositionReuseHits + CompositionReuseMisses == 0
            ? 0d
            : (double)CompositionReuseHits / (CompositionReuseHits + CompositionReuseMisses);

    public void RecordReuseHit() => CompositionReuseHits++;

    public void RecordReuseMiss() => CompositionReuseMisses++;

    public void RecordNestedReadAvoidance() => CompositionNestedReadAvoidanceCount++;

    public void RecordSnapshotBuild() => CompositionSnapshotBuilds++;

}
