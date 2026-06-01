using System.Security.Cryptography;
using System.Text;

namespace Tannous.Pos.Application.Audit.Governance;

public static class OperationalGovernanceProjectionSignatureBuilder
{
    public static OperationalGovernanceProjectionSignatureDto Build(
        OperationalGovernanceSnapshotComposition composition)
    {
        var segments = BuildSegments(composition);
        var normalized = string.Join('|', segments);

        return new OperationalGovernanceProjectionSignatureDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            SnapshotKey = composition.SnapshotKey,
            Profile = composition.Profile.ToString(),
            NormalizedSignature = normalized,
            SignatureSegments = segments,
            SegmentCount = segments.Count,
            GovernanceNotes = OperationalGovernanceRuntimeBudget.ClampOrdered(new[]
            {
                "Signatures exclude timestamps, IDs, and payload values.",
                "Signatures are governance classification only."
            }, 2)
        };
    }

    internal static IReadOnlyList<string> BuildSegments(OperationalGovernanceSnapshotComposition composition)
    {
        var ctx = composition.Context;
        var segments = new List<string>
        {
            $"Budget:{ctx.BudgetPressure}",
            $"Cardinality:{ctx.Overview.CardinalityClassification}",
            $"Degradation:{ctx.DegradationState}",
            $"Drift:{ctx.DriftSummary.DriftSeverity}|Detected:{ctx.DriftSummary.DriftDetected}",
            $"Execution:{ctx.ExecutionState}",
            $"Failsafe:{composition.RuntimeProtection.Failsafe.FailsafeActive}",
            $"GovernanceConsistent:{composition.GovernanceConsistency.IsConsistent}",
            $"InvalidationPressure:{ctx.InvalidationPressureSeverity}",
            $"Pressure:{ctx.PressureSeverity}",
            $"Profile:{composition.Profile}",
            $"ProjectionComplexity:{ctx.ProjectionComplexity}",
            $"Readiness:{ctx.ReadinessState}",
            $"RuntimeBudget:{composition.RuntimeProtection.BudgetPressure}",
            $"RuntimeExecution:{composition.RuntimeProtection.ExecutionState}",
            $"Saturation:{ctx.TelemetrySaturationLevel}",
            $"Stability:{ctx.Stability.StabilityClassification}",
            $"Survivability:{ctx.Survivability.Classification}",
            $"WarmSuppressed:{ctx.WarmRecommendationsSuppressed}"
        };

        var explainability = OperationalGovernanceRuntimeBudget.ClampExplainabilityOrdered(
            ctx.Overview.ReasonCodes
                .Concat(ctx.Overview.TriggerSignals)
                .Concat(ctx.Stability.ReasonCodes)
                .Concat(composition.GovernanceConsistency.ReasonCodes),
            OperationalGovernanceFingerprintConstants.MaxSignatureExplainabilityCodes);

        if (explainability.Count > 0)
            segments.Add($"Signals:{string.Join('+', explainability)}");

        return OperationalGovernanceRuntimeBudget.ClampOrdered(
            segments
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .OrderBy(s => s, StringComparer.Ordinal)
                .Distinct(StringComparer.Ordinal),
            OperationalGovernanceFingerprintConstants.MaxSignatureSegments);
    }
}

public static class OperationalGovernanceFingerprintBuilder
{
    public static (string FingerprintHash, string ExplainabilityHash, OperationalGovernanceProjectionSignatureDto Signature)
        BuildFingerprintParts(OperationalGovernanceSnapshotComposition composition)
    {
        var signature = OperationalGovernanceProjectionSignatureBuilder.Build(composition);
        var explainability = OperationalGovernanceRuntimeBudget.ClampExplainabilityOrdered(
            composition.Context.Overview.ReasonCodes
                .Concat(composition.Context.Overview.TriggerSignals)
                .Concat(composition.GovernanceConsistency.ReasonCodes),
            OperationalGovernanceFingerprintConstants.MaxSignatureExplainabilityCodes);

        return (
            ComputeHash(signature.NormalizedSignature),
            ComputeHash(string.Join('+', explainability)),
            signature);
    }

    public static OperationalGovernanceFingerprintDto BuildDto(
        OperationalGovernanceSnapshotComposition composition,
        OperationalGovernanceFingerprintComparisonDto? comparison,
        OperationalGovernanceFingerprintStability stability)
    {
        var (fingerprintHash, explainabilityHash, signature) = BuildFingerprintParts(composition);
        var explainability = OperationalGovernanceFingerprintExplainabilityBuilder.Build(
            stability,
            comparison?.DriftDirection,
            comparison?.FingerprintChanged ?? false,
            hasPrevious: comparison?.PreviousFingerprintHash != null);

        return new OperationalGovernanceFingerprintDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            SnapshotKey = composition.SnapshotKey,
            Profile = composition.Profile.ToString(),
            FingerprintHash = fingerprintHash,
            ExplainabilityHash = explainabilityHash,
            Signature = signature,
            FingerprintStability = stability.ToString(),
            HasPreviousFingerprint = comparison?.PreviousFingerprintHash != null,
            PreviousFingerprintHash = comparison?.PreviousFingerprintHash,
            FingerprintChanged = comparison?.FingerprintChanged ?? false,
            ExplainabilityCodes = explainability,
            GovernanceNotes = OperationalGovernanceRuntimeBudget.ClampOrdered(new[]
            {
                "Fingerprints are governance-only and advisory.",
                "No persistence or historical guarantees."
            }, 2)
        };
    }

    internal static string ComputeHash(string normalizedInput)
    {
        if (string.IsNullOrEmpty(normalizedInput))
            return new string('0', OperationalGovernanceFingerprintConstants.FingerprintHashHexLength);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedInput));
        return Convert.ToHexString(bytes.AsSpan(0, 8));
    }
}
