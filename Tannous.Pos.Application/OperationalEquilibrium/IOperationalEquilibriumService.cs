namespace Tannous.Pos.Application.OperationalEquilibrium;

/// <summary>Deterministic operational equilibrium and systemic balance intelligence.</summary>
public interface IOperationalEquilibriumService
{
    Task<OperationalEquilibriumReportDto> GetEquilibriumReportAsync(CancellationToken cancellationToken = default);
    Task<OperationalEquilibriumSummaryDto> GetEquilibriumSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OperationalImbalanceDto>> GetOperationalImbalancesAsync(CancellationToken cancellationToken = default);
}
