namespace Tannous.Pos.Application.Audit.Governance;

public sealed class OperationalGovernanceFailsafeDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public bool FailsafeActive { get; init; }
    public bool WarmRecommendationsSuppressed { get; init; }
    public bool ExplainabilityTruncated { get; init; }
    public bool RecommendationsReduced { get; init; }
    public long GovernanceFailsafeActivations { get; init; }
    public IReadOnlyList<string> ProtectionSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
}
