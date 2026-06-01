namespace Tannous.Pos.Application.Audit.Governance;

public static class OperationalGovernanceDriftDirectionClassifier
{
    public static OperationalGovernanceDriftDirection Classify(
        bool fingerprintChanged,
        IReadOnlyList<string> divergentSegments,
        IReadOnlyList<string> currentSegments,
        IReadOnlyList<string>? previousSegments)
    {
        if (!fingerprintChanged || previousSegments == null)
            return OperationalGovernanceDriftDirection.Neutral;

        if (divergentSegments.Count >= 4)
            return OperationalGovernanceDriftDirection.Oscillating;

        var improvingSignals = divergentSegments.Count(s =>
            s.Contains("Degradation:Healthy", StringComparison.Ordinal)
            || s.Contains("Pressure:Nominal", StringComparison.Ordinal)
            || s.Contains("Failsafe:False", StringComparison.Ordinal)
            || s.Contains("Drift:None", StringComparison.Ordinal));

        var degradingSignals = divergentSegments.Count(s =>
            s.Contains("Failsafe:True", StringComparison.Ordinal)
            || s.Contains("Pressure:Critical", StringComparison.Ordinal)
            || s.Contains("Pressure:High", StringComparison.Ordinal)
            || s.Contains("Execution:Failsafe", StringComparison.Ordinal)
            || s.Contains("DriftDetected:True", StringComparison.Ordinal));

        if (degradingSignals > improvingSignals)
            return OperationalGovernanceDriftDirection.Degrading;

        if (improvingSignals > degradingSignals)
            return OperationalGovernanceDriftDirection.Improving;

        if (divergentSegments.Count >= 2)
            return OperationalGovernanceDriftDirection.Oscillating;

        return OperationalGovernanceDriftDirection.Neutral;
    }
}

public static class OperationalGovernanceFingerprintStabilityClassifier
{
    public static OperationalGovernanceFingerprintStability Classify(
        bool fingerprintChanged,
        int divergentSegmentCount,
        bool hasPreviousFingerprint)
    {
        if (!hasPreviousFingerprint)
            return OperationalGovernanceFingerprintStability.Transitional;

        if (!fingerprintChanged)
            return OperationalGovernanceFingerprintStability.Stable;

        if (divergentSegmentCount >= 4)
            return OperationalGovernanceFingerprintStability.Fragmented;

        if (divergentSegmentCount >= 2)
            return OperationalGovernanceFingerprintStability.Diverging;

        return OperationalGovernanceFingerprintStability.Transitional;
    }
}

public static class OperationalGovernanceReplayConsistencyClassifier
{
    public static OperationalGovernanceReplayConsistencyLevel Classify(
        bool snapshotWasReused,
        bool fingerprintStable,
        OperationalGovernanceFingerprintStability stability,
        long fragmentationSignals)
    {
        if (fragmentationSignals > 0 || stability == OperationalGovernanceFingerprintStability.Fragmented)
            return OperationalGovernanceReplayConsistencyLevel.Low;

        if (snapshotWasReused && fingerprintStable)
            return OperationalGovernanceReplayConsistencyLevel.High;

        if (fingerprintStable && stability == OperationalGovernanceFingerprintStability.Stable)
            return OperationalGovernanceReplayConsistencyLevel.High;

        if (stability == OperationalGovernanceFingerprintStability.Transitional)
            return OperationalGovernanceReplayConsistencyLevel.Moderate;

        if (stability == OperationalGovernanceFingerprintStability.Diverging)
            return OperationalGovernanceReplayConsistencyLevel.Low;

        return OperationalGovernanceReplayConsistencyLevel.Indeterminate;
    }
}
