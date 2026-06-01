using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Infrastructure.Services;

internal static class OperationalCacheAdaptiveTtlHelper
{
    public static TimeSpan ResolveEffectiveTtl(
        string category,
        OperationalCacheAdaptivePressureSignals signals,
        IOperationalDiagnosticsCache cache,
        IOperationalDiagnosticsCacheTelemetry telemetry,
        ILogger logger,
        out OperationalCacheTtlMode mode)
    {
        var snapshot = telemetry.GetSnapshot();
        var entries = cache.GetDiagnosticsEntryMetadata();
        var cardinality = OperationalCacheCardinalityClassifier.BuildSnapshot(entries);
        var pressureSeverity = OperationalCachePressureClassifier.Classify(
            snapshot,
            entries,
            cardinality.Classification);

        var ttl = OperationalCacheAdaptiveTtlClassifier.GetEffectiveTtl(category, signals, out mode);
        ttl = OperationalCacheAdaptiveTtlClassifier.ApplyCachePressureSeverity(ttl, category, pressureSeverity);

        if (OperationalCacheAdaptiveTtlClassifier.IsTtlReduced(category, mode)
            || pressureSeverity >= OperationalCachePressureSeverity.Elevated)
        {
            telemetry.RecordAdaptiveTtlReduction(category);
            logger.LogInformation(
                "Operational adaptive TTL reduction: adaptive TTL applied. Category={Category}, TtlMode={TtlMode}, PressureSeverity={PressureSeverity}, EffectiveTtlSeconds={EffectiveTtlSeconds}",
                category,
                mode,
                pressureSeverity,
                (int)ttl.TotalSeconds);
        }

        logger.LogDebug(
            "Operational adaptive cache governance: TTL mode classified. Category={Category}, TtlMode={TtlMode}, PressureSeverity={PressureSeverity}",
            category,
            mode,
            pressureSeverity);

        return ttl;
    }
}
