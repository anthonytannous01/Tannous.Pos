namespace Tannous.Pos.Application.Audit;

/// <summary>Heuristic correlated risk classification (no enforcement).</summary>
public static class OperationalIncidentRiskClassifier
{
    public static string ClassifyCorrelatedRisk(IReadOnlyList<OperationalIncidentSignal> signals)
    {
        if (signals.Count == 0)
            return OperationalIncidentSeverity.Low;

        var severity = ClassifySeverity(signals);
        return severity;
    }

    public static string ClassifySeverity(IReadOnlyList<OperationalIncidentSignal> signals)
    {
        var replayCount = signals.Count(s => s.IncidentType == OperationalIncidentTypes.ReplayIncident);
        var reconCount = signals.Count(s => s.IncidentType == OperationalIncidentTypes.ReconciliationIncident);
        var inventoryCount = signals.Count(s => s.IncidentType == OperationalIncidentTypes.InventoryDriftIncident);
        var settlementCount = signals.Count(s => s.IncidentType == OperationalIncidentTypes.SettlementInconsistencyIncident);
        var cascading = signals.Any(s => s.IncidentType == OperationalIncidentTypes.CascadingDegradationIncident);
        var concurrency = signals.Any(s => s.ConflictType?.Contains("Concurrency", StringComparison.OrdinalIgnoreCase) == true
            || s.AuditAction == OperationalAuditActions.ConcurrencyConflict);

        if (cascading && (replayCount >= OperationalIncidentCorrelationConstants.RepeatedReplayMismatchThreshold
            || reconCount >= OperationalIncidentCorrelationConstants.RepeatedUnresolvedConflictThreshold))
            return OperationalIncidentSeverity.Critical;

        if (settlementCount > 0 && concurrency)
            return OperationalIncidentSeverity.Critical;

        if (replayCount >= OperationalIncidentCorrelationConstants.RepeatedReplayMismatchThreshold
            || reconCount >= OperationalIncidentCorrelationConstants.RepeatedUnresolvedConflictThreshold)
            return OperationalIncidentSeverity.High;

        if (inventoryCount >= OperationalIncidentCorrelationConstants.RepeatedInventoryDriftThreshold)
            return OperationalIncidentSeverity.High;

        if (signals.Count >= OperationalIncidentCorrelationConstants.CascadingSubsystemMinimum)
            return OperationalIncidentSeverity.Moderate;

        return OperationalIncidentSeverity.Low;
    }

    public static bool IsHighRisk(string severity) =>
        severity is OperationalIncidentSeverity.High or OperationalIncidentSeverity.Critical;

    public static string GetMaxSeverity(IEnumerable<string> severities)
    {
        var maxRank = 0;
        var maxSeverity = OperationalIncidentSeverity.Low;
        foreach (var severity in severities)
        {
            var rank = GetSeverityRank(severity);
            if (rank > maxRank)
            {
                maxRank = rank;
                maxSeverity = severity;
            }
        }

        return maxSeverity;
    }

    private static int GetSeverityRank(string severity) =>
        severity switch
        {
            OperationalIncidentSeverity.Critical => 4,
            OperationalIncidentSeverity.High => 3,
            OperationalIncidentSeverity.Moderate => 2,
            _ => 1
        };
}
