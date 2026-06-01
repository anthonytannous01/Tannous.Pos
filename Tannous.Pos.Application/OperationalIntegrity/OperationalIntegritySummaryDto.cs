namespace Tannous.Pos.Application.OperationalIntegrity;

/// <summary>Compact operational interpretation integrity summary.</summary>
public sealed class OperationalIntegritySummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalIntegrityState IntegrityState { get; init; }
    public string AlignmentStrength { get; init; } = string.Empty;
    public string ContradictionPressure { get; init; } = string.Empty;
    public string DominantOperationalStory { get; init; } = string.Empty;
    public string RecoveryConsistency { get; init; } = string.Empty;
    public string OperatorAttentionLevel { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string IntegrityNote { get; init; } =
        "Advisory deterministic cross-layer interpretation integrity verification. Not policy enforcement, AI validation, or automated remediation.";
}
