namespace Tannous.Pos.Application.OperationalReplayWorkbench;

/// <summary>Operator-facing replay pressure workbench aggregation (read-only; no persistence).</summary>
public interface IOperationalReplayWorkbenchService
{
    Task<OperationalReplayWorkbenchDto> GetPressureWorkbenchAsync(CancellationToken cancellationToken = default);
}
