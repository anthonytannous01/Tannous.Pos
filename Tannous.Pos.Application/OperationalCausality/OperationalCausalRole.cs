namespace Tannous.Pos.Application.OperationalCausality;

/// <summary>Role of an area within a causal chain (advisory heuristic).</summary>
public enum OperationalCausalRole
{
    Origin = 0,
    Upstream = 1,
    Downstream = 2,
    Amplifier = 3,
    Blocker = 4,
    Stabilizer = 5
}
