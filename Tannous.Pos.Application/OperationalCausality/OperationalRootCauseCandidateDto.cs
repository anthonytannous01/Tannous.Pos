namespace Tannous.Pos.Application.OperationalCausality;

/// <summary>Advisory root-cause candidate from deterministic heuristics.</summary>
public sealed class OperationalRootCauseCandidateDto
{
    public string Area { get; init; } = string.Empty;
    public string Explanation { get; init; } = string.Empty;
    public OperationalCausalityConfidence Confidence { get; init; }
    public int EvidenceCount { get; init; }
    public IReadOnlyList<string> SupportingSignals { get; init; } = Array.Empty<string>();
    public string RecoveryAlignment { get; init; } = string.Empty;
}
