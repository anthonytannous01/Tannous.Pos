namespace Tannous.Pos.Application.OperationalRecovery;

/// <summary>Operator-facing recovery movement direction.</summary>
public enum OperationalRecoveryDirection
{
    Improving = 0,
    Stable = 1,
    Degrading = 2,
    Converging = 3,
    Diverging = 4
}
