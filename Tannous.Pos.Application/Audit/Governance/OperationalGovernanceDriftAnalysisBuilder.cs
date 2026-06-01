namespace Tannous.Pos.Application.Audit.Governance;

public static class OperationalGovernanceDriftAnalysisBuilder
{
    public static OperationalGovernanceDriftAnalysisDto Build(
        OperationalGovernanceSnapshotComposition composition,
        OperationalGovernanceFingerprintComparisonDto comparison,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetry)
    {
        var stability = Enum.TryParse<OperationalGovernanceFingerprintStability>(
            comparison.FingerprintStability,
            out var parsedStability)
            ? parsedStability
            : OperationalGovernanceFingerprintStability.Transitional;

        var driftSignals = BuildDriftSignals(comparison, stability);
        var explainability = OperationalGovernanceFingerprintExplainabilityBuilder.Build(
            stability,
            comparison.DriftDirection,
            comparison.FingerprintChanged,
            comparison.PreviousFingerprintHash != null);

        var (fingerprintHash, _, _) = OperationalGovernanceFingerprintBuilder.BuildFingerprintParts(composition);

        return new OperationalGovernanceDriftAnalysisDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            SnapshotKey = composition.SnapshotKey,
            FingerprintHash = fingerprintHash,
            FingerprintStability = comparison.FingerprintStability,
            DriftDirection = comparison.DriftDirection,
            FingerprintChanged = comparison.FingerprintChanged,
            HasPreviousFingerprint = comparison.PreviousFingerprintHash != null,
            PreviousFingerprintHash = comparison.PreviousFingerprintHash,
            GovernanceFingerprintTransitions = telemetry.GovernanceFingerprintTransitions,
            GovernanceDriftEscalations = telemetry.GovernanceDriftEscalations,
            GovernanceStableFingerprintHits = telemetry.GovernanceStableFingerprintHits,
            DriftSignals = driftSignals,
            ExplainabilityCodes = explainability,
            GovernanceNotes = OperationalGovernanceRuntimeBudget.ClampOrdered(new[]
            {
                "Drift analysis is process-local and advisory.",
                "No persistence or cross-node comparison."
            }, 2)
        };
    }

    private static IReadOnlyList<string> BuildDriftSignals(
        OperationalGovernanceFingerprintComparisonDto comparison,
        OperationalGovernanceFingerprintStability stability)
    {
        var signals = new List<string>(comparison.ComparisonSignals);

        if (stability == OperationalGovernanceFingerprintStability.Fragmented)
            signals.Add("ProjectionFragmentationDetected");

        if (string.Equals(comparison.DriftDirection, OperationalGovernanceDriftDirection.Degrading.ToString(), StringComparison.Ordinal))
            signals.Add("DriftDirection:Degrading");

        if (string.Equals(comparison.DriftDirection, OperationalGovernanceDriftDirection.Improving.ToString(), StringComparison.Ordinal))
            signals.Add("DriftDirection:Improving");

        return OperationalGovernanceRuntimeBudget.ClampExplainabilityOrdered(
            signals,
            OperationalGovernanceFingerprintConstants.MaxDriftSignals);
    }
}

public static class OperationalGovernanceFingerprintExplainabilityBuilder
{
    public static IReadOnlyList<string> Build(
        OperationalGovernanceFingerprintStability stability,
        string? driftDirection,
        bool fingerprintChanged,
        bool hasPrevious)
    {
        var signals = new List<string>();

        if (!fingerprintChanged)
            signals.Add("FingerprintStable");
        else if (hasPrevious)
            signals.Add("FingerprintTransition");

        if (!string.IsNullOrWhiteSpace(driftDirection))
            signals.Add($"DriftDirection:{driftDirection}");

        switch (stability)
        {
            case OperationalGovernanceFingerprintStability.Fragmented:
                signals.Add("ProjectionFragmentationDetected");
                break;
            case OperationalGovernanceFingerprintStability.Diverging:
                signals.Add("FingerprintDiverging");
                break;
            case OperationalGovernanceFingerprintStability.Transitional:
                signals.Add("FingerprintTransitional");
                break;
        }

        return OperationalGovernanceRuntimeBudget.ClampExplainabilityOrdered(
            signals,
            OperationalGovernanceRuntimeBudget.MaxExplainabilitySignals);
    }
}
