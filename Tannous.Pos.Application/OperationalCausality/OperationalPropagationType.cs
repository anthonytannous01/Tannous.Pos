namespace Tannous.Pos.Application.OperationalCausality;

/// <summary>Operator-facing pressure propagation category.</summary>
public enum OperationalPropagationType
{
    ReplayPressure = 0,
    InventoryDrift = 1,
    RuntimeProtection = 2,
    ReconciliationPressure = 3,
    OperationalVolatility = 4
}
