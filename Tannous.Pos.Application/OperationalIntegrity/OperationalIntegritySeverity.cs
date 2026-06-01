namespace Tannous.Pos.Application.OperationalIntegrity;

/// <summary>Operator-facing integrity issue severity.</summary>
public enum OperationalIntegritySeverity
{
    Normal = 0,
    Elevated = 1,
    High = 2,
    Critical = 3
}
