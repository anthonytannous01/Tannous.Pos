namespace Tannous.Pos.Application.OperationalPatterns;

/// <summary>Operator-facing pattern stability direction.</summary>
public enum OperationalPatternDirection
{
    Improving = 0,
    Stable = 1,
    Degrading = 2,
    Escalating = 3,
    Stabilizing = 4
}
