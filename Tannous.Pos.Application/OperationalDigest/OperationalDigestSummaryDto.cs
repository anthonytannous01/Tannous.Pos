namespace Tannous.Pos.Application.OperationalDigest;

/// <summary>Compact operational digest summary.</summary>
public sealed class OperationalDigestSummaryDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalDigestState OperationalState { get; init; }
    public string DominantNarrative { get; init; } = string.Empty;
    public string EscalationAlignment { get; init; } = string.Empty;
    public string RecoveryAlignment { get; init; } = string.Empty;
    public string IntegrityAlignment { get; init; } = string.Empty;
    public string OperatorAttentionLevel { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string DigestNote { get; init; } =
        "Advisory deterministic operational condensation composed from bounded process-local intelligence.";
}
