namespace Tannous.Pos.Application.Audit;

/// <summary>Heuristic cache pressure severity (no machine memory inspection).</summary>
public enum OperationalCachePressureSeverity
{
    Nominal = 0,
    Elevated = 1,
    High = 2,
    Critical = 3
}
