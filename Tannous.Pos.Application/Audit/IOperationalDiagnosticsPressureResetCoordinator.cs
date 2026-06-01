namespace Tannous.Pos.Application.Audit;

/// <summary>
/// Best-effort in-process reset of governance/pressure diagnostics state only.
/// GOVERNANCE: never mutates EF entities, replay, or reconciliation semantics.
/// </summary>
public interface IOperationalDiagnosticsPressureResetCoordinator
{
    /// <summary>
    /// Idempotent reset of governance pressure flags, optional cache clear, and stabilization counters.
    /// </summary>
    /// <param name="clearDiagnosticsCaches">When true, clears operational diagnostics cache entries only.</param>
    void ResetGovernanceState(bool clearDiagnosticsCaches = true);
}
