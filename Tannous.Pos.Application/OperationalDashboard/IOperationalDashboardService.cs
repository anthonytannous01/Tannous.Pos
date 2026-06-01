namespace Tannous.Pos.Application.OperationalDashboard;

/// <summary>Operator-facing operational dashboard aggregation (read-only; no persistence).</summary>
public interface IOperationalDashboardService
{
    Task<OperationalDashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
