namespace Tannous.Pos.Application.OperationalReplayWorkbench;

/// <summary>Operator-facing replay recovery confidence projection.</summary>
public sealed class OperationalReplayRecoveryConfidenceDto
{
    public OperationalReplayRecoveryConfidence Confidence { get; init; }
    public string Summary { get; init; } = string.Empty;
}
