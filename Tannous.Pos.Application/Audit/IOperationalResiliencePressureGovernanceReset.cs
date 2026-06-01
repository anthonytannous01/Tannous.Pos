namespace Tannous.Pos.Application.Audit;

/// <summary>Internal governance-only reset for sticky resilience pressure flags (not a public API).</summary>
public interface IOperationalResiliencePressureGovernanceReset
{
    /// <summary>Clears query/export pressure flags; idempotent; does not affect business data.</summary>
    void ResetGovernancePressureFlags();
}
