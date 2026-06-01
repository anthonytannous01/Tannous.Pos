namespace Tannous.Pos.Application.OperationalTopology;

/// <summary>Bounded operational topology posture (operator wording).</summary>
public enum OperationalTopologyState
{
    Stable,
    Concentrated,
    Fragmented,
    EscalationDominant,
    RecoveryConverging
}
