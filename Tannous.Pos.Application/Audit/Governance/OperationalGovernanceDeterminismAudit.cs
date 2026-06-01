namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>Deterministic governance projection audit helpers (no persistence).</summary>
public static class OperationalGovernanceDeterminismAudit
{
    public static OperationalGovernanceDeterminismAuditResult AuditComposition(
        OperationalGovernanceSnapshotComposition composition,
        bool snapshotWasReused)
    {
        var issues = new List<string>();

        if (composition.ExplainabilityCodes
            .OrderBy(s => s, StringComparer.Ordinal)
            .SequenceEqual(composition.ExplainabilityCodes) == false)
            issues.Add("ExplainabilityOrderingNonDeterministic");

        if (composition.SignatureSegments
            .OrderBy(s => s, StringComparer.Ordinal)
            .SequenceEqual(composition.SignatureSegments) == false)
            issues.Add("SignatureSegmentOrderingNonDeterministic");

        if (string.IsNullOrWhiteSpace(composition.FingerprintHash))
            issues.Add("FingerprintMissing");

        var explainabilityDistinct = composition.ExplainabilityCodes
            .Distinct(StringComparer.Ordinal)
            .Count();
        if (explainabilityDistinct != composition.ExplainabilityCodes.Count)
            issues.Add("ExplainabilityDuplicateCodes");

        return new OperationalGovernanceDeterminismAuditResult(
            issues.Count == 0,
            OperationalGovernanceRuntimeBudget.ClampExplainabilityOrdered(issues, 6));
    }

    public static bool ExplainabilityIsDeterministicallyOrdered(IReadOnlyList<string> codes) =>
        codes.OrderBy(s => s, StringComparer.Ordinal).SequenceEqual(codes);

    public sealed record OperationalGovernanceDeterminismAuditResult(
        bool IsDeterministic,
        IReadOnlyList<string> Issues);
}
