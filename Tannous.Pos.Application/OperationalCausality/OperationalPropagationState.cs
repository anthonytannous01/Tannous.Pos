namespace Tannous.Pos.Application.OperationalCausality;

/// <summary>Operator-facing propagation state interpretation.</summary>
public enum OperationalPropagationState
{
    Expanding = 0,
    Stabilizing = 1,
    Collapsing = 2,
    Cyclical = 3,
    Recurring = 4
}
