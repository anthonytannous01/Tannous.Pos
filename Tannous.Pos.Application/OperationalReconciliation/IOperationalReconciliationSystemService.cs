namespace Tannous.Pos.Application.OperationalReconciliation;

/// <summary>System-level reconciliation health view. Advisory only — does not mutate conflict state.</summary>
public interface IOperationalReconciliationSystemService
{
    Task<OperationalReconciliationSystemDto> GetReconciliationSystemAsync(
        CancellationToken cancellationToken = default);
}
