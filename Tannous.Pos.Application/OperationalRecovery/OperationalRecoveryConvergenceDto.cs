namespace Tannous.Pos.Application.OperationalRecovery;

/// <summary>Convergence or divergence outlook for an operational domain.</summary>
public sealed class OperationalRecoveryConvergenceDto
{
    public string Domain { get; init; } = string.Empty;
    public OperationalRecoveryDirection Direction { get; init; }
    public OperationalRecoveryConfidence Confidence { get; init; }
    public string Summary { get; init; } = string.Empty;
}
