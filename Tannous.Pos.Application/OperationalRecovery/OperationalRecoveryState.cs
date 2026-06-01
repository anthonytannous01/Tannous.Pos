namespace Tannous.Pos.Application.OperationalRecovery;

/// <summary>Operator-facing operational recovery posture state.</summary>
public enum OperationalRecoveryState
{
    Recovering = 0,
    Stable = 1,
    Degrading = 2,
    Volatile = 3,
    Saturated = 4,
    Stabilizing = 5
}
