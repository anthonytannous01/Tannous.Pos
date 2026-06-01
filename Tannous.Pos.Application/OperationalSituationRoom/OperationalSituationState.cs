namespace Tannous.Pos.Application.OperationalSituationRoom;

/// <summary>Operator-facing platform condition state.</summary>
public enum OperationalSituationState
{
    Stable = 0,
    Stressed = 1,
    Degrading = 2,
    Critical = 3,
    Recovering = 4
}
