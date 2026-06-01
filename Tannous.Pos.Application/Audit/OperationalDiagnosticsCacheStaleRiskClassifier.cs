namespace Tannous.Pos.Application.Audit;

public static class OperationalDiagnosticsCacheStaleRiskClassifier
{
    public static OperationalDiagnosticsCacheStaleRisk Classify(
        DateTime createdUtc,
        DateTime expiresUtc,
        DateTime utcNow)
    {
        if (utcNow >= expiresUtc)
            return OperationalDiagnosticsCacheStaleRisk.Expired;

        var total = expiresUtc - createdUtc;
        if (total <= TimeSpan.Zero)
            return OperationalDiagnosticsCacheStaleRisk.Fresh;

        var age = utcNow - createdUtc;
        var ratio = age.TotalMilliseconds / total.TotalMilliseconds;

        if (ratio >= OperationalDiagnosticsCacheConstants.NearExpiryThresholdPercent)
            return OperationalDiagnosticsCacheStaleRisk.NearExpiry;

        if (ratio >= OperationalDiagnosticsCacheConstants.AgingThresholdPercent)
            return OperationalDiagnosticsCacheStaleRisk.Aging;

        return OperationalDiagnosticsCacheStaleRisk.Fresh;
    }
}
