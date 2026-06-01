namespace Tannous.Pos.Application.OperationalStrategy;

/// <summary>Deterministic operational strategic posture coordination.</summary>
public interface IOperationalStrategyService
{
    Task<OperationalStrategyReportDto> GetStrategyReportAsync(CancellationToken cancellationToken = default);
    Task<OperationalStrategySummaryDto> GetStrategySummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OperationalCoordinationDto>> GetOperationalCoordinationAsync(CancellationToken cancellationToken = default);
}
