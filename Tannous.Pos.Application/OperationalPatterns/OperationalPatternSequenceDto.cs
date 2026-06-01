namespace Tannous.Pos.Application.OperationalPatterns;

/// <summary>Deterministic operational pattern sequence interpretation.</summary>
public sealed class OperationalPatternSequenceDto
{
    public string SequenceId { get; init; } = string.Empty;
    public OperationalPatternType SequenceType { get; init; }
    public IReadOnlyList<string> OperationalStages { get; init; } = Array.Empty<string>();
    public string EscalationFlow { get; init; } = string.Empty;
    public string RecoveryFlow { get; init; } = string.Empty;
    public string DominantTransition { get; init; } = string.Empty;
    public string OperatorSummary { get; init; } = string.Empty;
}
