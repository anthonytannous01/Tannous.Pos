namespace Tannous.Pos.Application.OperationalIntegrity;

/// <summary>Full operational interpretation integrity report.</summary>
public sealed class OperationalIntegrityReportDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalIntegrityState OverallIntegrityState { get; init; }
    public int ConsistencyScore { get; init; }
    public string DominantOperationalNarrative { get; init; } = string.Empty;
    public string AlignmentState { get; init; } = string.Empty;
    public int ContradictionCount { get; init; }
    public int AlignmentCount { get; init; }
    public IReadOnlyList<OperationalInterpretationAlignmentDto> Alignments { get; init; } =
        Array.Empty<OperationalInterpretationAlignmentDto>();
    public OperationalNarrativeConsistencyDto NarrativeConsistency { get; init; } = new();
    public IReadOnlyList<OperationalIntegrityWarningDto> IntegrityWarnings { get; init; } =
        Array.Empty<OperationalIntegrityWarningDto>();
    public string OperatorSummary { get; init; } = string.Empty;
    public string IntegrityNote { get; init; } =
        "Advisory deterministic cross-layer interpretation integrity verification. Not policy enforcement, AI validation, or automated remediation.";
}
