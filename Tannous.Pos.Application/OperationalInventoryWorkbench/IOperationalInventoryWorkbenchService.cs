namespace Tannous.Pos.Application.OperationalInventoryWorkbench;

/// <summary>Operator-facing inventory drift workbench aggregation (read-only; no persistence).</summary>
public interface IOperationalInventoryWorkbenchService
{
    Task<OperationalInventoryWorkbenchDto> GetDriftWorkbenchAsync(CancellationToken cancellationToken = default);
}
