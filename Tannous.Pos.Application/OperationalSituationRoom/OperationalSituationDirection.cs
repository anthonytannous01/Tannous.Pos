namespace Tannous.Pos.Application.OperationalSituationRoom;

/// <summary>Operator-facing stabilization trajectory direction.</summary>
public enum OperationalSituationDirection
{
    Improving = 0,
    Stable = 1,
    Degrading = 2,
    Escalating = 3,
    Stabilizing = 4
}
