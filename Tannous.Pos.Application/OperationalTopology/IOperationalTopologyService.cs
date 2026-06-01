namespace Tannous.Pos.Application.OperationalTopology;

/// <summary>Deterministic operational topology and dependency intelligence (advisory; GET-only).</summary>
public interface IOperationalTopologyService
{
    Task<OperationalTopologyDto> GetOperationalTopologyAsync(CancellationToken cancellationToken = default);

    Task<OperationalTopologySummaryDto> GetTopologySummaryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalDependencyChainDto>> GetDependencyChainsAsync(CancellationToken cancellationToken = default);
}
