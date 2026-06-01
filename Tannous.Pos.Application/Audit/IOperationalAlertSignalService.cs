namespace Tannous.Pos.Application.Audit;

/// <summary>Query-time operational alert signals derived from existing diagnostics (no persistence; no delivery).</summary>
public interface IOperationalAlertSignalService
{
    Task<IReadOnlyList<OperationalAlertSignalDto>> GetCurrentSignalsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalAlertSignalDto>> GetCriticalSignalsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalAlertSignalDto>> GetReplayPressureSignalsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalAlertSignalDto>> GetInventoryRiskSignalsAsync(CancellationToken cancellationToken = default);

    Task<OperationalAlertSummaryDto> GetAlertSummaryAsync(CancellationToken cancellationToken = default);
}
