namespace Tannous.Pos.Application.OperationalCausality;

/// <summary>Operator causality and root-cause explanation (advisory; process-local).</summary>
public interface IOperationalCausalityService
{
    Task<OperationalCausalChainsDto> GetCausalChainsAsync(CancellationToken cancellationToken = default);

    Task<OperationalCausalitySummaryDto> GetCausalitySummaryAsync(CancellationToken cancellationToken = default);

    Task<OperationalPropagationAnalysisDto> GetPropagationAnalysisAsync(CancellationToken cancellationToken = default);
}
