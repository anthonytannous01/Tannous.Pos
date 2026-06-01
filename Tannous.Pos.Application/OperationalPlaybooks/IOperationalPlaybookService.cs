namespace Tannous.Pos.Application.OperationalPlaybooks;

/// <summary>Operator operational response playbooks and stabilization guidance (advisory, process-local).</summary>
public interface IOperationalPlaybookService
{
    Task<OperationalPlaybooksDto> GetOperationalPlaybooksAsync(CancellationToken cancellationToken = default);

    Task<OperationalPlaybookSummaryDto> GetPlaybookSummaryAsync(CancellationToken cancellationToken = default);

    Task<OperationalStabilizationGuidanceDto> GetStabilizationGuidanceAsync(CancellationToken cancellationToken = default);
}
