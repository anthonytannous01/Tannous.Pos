namespace Tannous.Pos.Application.Audit.Governance;

public static class OperationalGovernanceFingerprintComparer
{
    public static OperationalGovernanceFingerprintComparisonDto Compare(
        string currentFingerprintHash,
        string? previousFingerprintHash,
        IReadOnlyList<string> currentSegments,
        IReadOnlyList<string>? previousSegments)
    {
        var changed = previousFingerprintHash != null
            && !string.Equals(currentFingerprintHash, previousFingerprintHash, StringComparison.Ordinal);

        var divergent = changed && previousSegments != null
            ? currentSegments
                .Except(previousSegments, StringComparer.Ordinal)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList()
            : new List<string>();

        var driftDirection = OperationalGovernanceDriftDirectionClassifier.Classify(
            changed,
            divergent,
            currentSegments,
            previousSegments);

        var stability = OperationalGovernanceFingerprintStabilityClassifier.Classify(
            changed,
            divergent.Count,
            previousFingerprintHash != null);

        var signals = BuildComparisonSignals(changed, driftDirection, stability, divergent.Count);

        return new OperationalGovernanceFingerprintComparisonDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            CurrentFingerprintHash = currentFingerprintHash,
            PreviousFingerprintHash = previousFingerprintHash,
            FingerprintChanged = changed,
            DriftDirection = driftDirection.ToString(),
            FingerprintStability = stability.ToString(),
            DivergentSegmentCount = divergent.Count,
            DivergentSegments = OperationalGovernanceRuntimeBudget.ClampOrdered(
                divergent,
                OperationalGovernanceFingerprintConstants.MaxComparisonSignals),
            ComparisonSignals = signals,
            GovernanceNotes = OperationalGovernanceRuntimeBudget.ClampOrdered(new[]
            {
                "Comparison is process-local only.",
                "Drift analysis is advisory."
            }, 2)
        };
    }

    private static IReadOnlyList<string> BuildComparisonSignals(
        bool changed,
        OperationalGovernanceDriftDirection driftDirection,
        OperationalGovernanceFingerprintStability stability,
        int divergentCount)
    {
        var signals = new List<string>();

        if (!changed)
            signals.Add("FingerprintStable");
        else
            signals.Add("FingerprintTransition");

        signals.Add($"DriftDirection:{driftDirection}");
        signals.Add($"Stability:{stability}");

        if (divergentCount >= 4)
            signals.Add("ProjectionFragmentationDetected");

        return OperationalGovernanceRuntimeBudget.ClampExplainabilityOrdered(
            signals,
            OperationalGovernanceFingerprintConstants.MaxComparisonSignals);
    }
}
