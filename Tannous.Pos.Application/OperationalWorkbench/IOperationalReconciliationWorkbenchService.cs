namespace Tannous.Pos.Application.OperationalWorkbench;

/// <summary>Operator-facing reconciliation workbench aggregation (read-only; no persistence).</summary>
public interface IOperationalReconciliationWorkbenchService
{
    Task<OperationalReconciliationWorkbenchDto> GetReconciliationWorkbenchAsync(CancellationToken cancellationToken = default);
}
