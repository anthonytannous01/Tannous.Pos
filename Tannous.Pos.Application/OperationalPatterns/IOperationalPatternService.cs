namespace Tannous.Pos.Application.OperationalPatterns;

/// <summary>Operator operational pattern intelligence (advisory, process-local).</summary>
public interface IOperationalPatternService
{
    Task<OperationalPatternsDto> GetOperationalPatternsAsync(CancellationToken cancellationToken = default);

    Task<OperationalPatternSummaryDto> GetPatternSummaryAsync(CancellationToken cancellationToken = default);

    Task<OperationalStabilizationArchetypesDto> GetStabilizationArchetypesAsync(CancellationToken cancellationToken = default);
}
