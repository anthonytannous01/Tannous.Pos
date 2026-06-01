namespace Tannous.Pos.Application.Audit;

/// <summary>Deterministic pressure inputs for adaptive TTL (no external metrics).</summary>
public sealed class OperationalCacheAdaptivePressureSignals
{
    public bool QueryDateRangeClamped { get; init; }
    public bool QueryPageSizeClamped { get; init; }
    public bool ForensicExportTruncated { get; init; }
    public bool ReplayStormRiskIndicated { get; init; }
    public bool ReconciliationBacklogElevated { get; init; }

    public static OperationalCacheAdaptivePressureSignals FromPressureState(
        IOperationalResiliencePressureState pressureState) =>
        new()
        {
            QueryDateRangeClamped = pressureState.QueryDateRangeClamped,
            QueryPageSizeClamped = pressureState.QueryPageSizeClamped,
            ForensicExportTruncated = pressureState.ForensicExportTruncated
        };

    public int ActiveSignalCount()
    {
        var count = 0;
        if (QueryDateRangeClamped) count++;
        if (QueryPageSizeClamped) count++;
        if (ForensicExportTruncated) count++;
        if (ReplayStormRiskIndicated) count++;
        if (ReconciliationBacklogElevated) count++;
        return count;
    }
}
