namespace Tannous.Pos.Application.OperationalDigest;

/// <summary>Condensed executive operational digest.</summary>
public sealed class OperationalExecutiveDigestDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string Headline { get; init; } = string.Empty;
    public string DominantNarrative { get; init; } = string.Empty;
    public string PrimaryOperationalRisk { get; init; } = string.Empty;
    public string RecoveryOutlook { get; init; } = string.Empty;
    public string EscalationSummary { get; init; } = string.Empty;
    public string StabilizationSummary { get; init; } = string.Empty;
    public string LeadershipAttentionRequired { get; init; } = string.Empty;
    public string RecommendedPriority { get; init; } = string.Empty;
    public IReadOnlyList<string> ExecutivePriorities { get; init; } = Array.Empty<string>();
    public string DigestNote { get; init; } =
        "Advisory deterministic executive condensation composed from existing operational intelligence. Not AI summarization, BI reporting, or dashboards.";
}
