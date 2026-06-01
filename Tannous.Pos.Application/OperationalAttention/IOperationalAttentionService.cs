namespace Tannous.Pos.Application.OperationalAttention;

/// <summary>Deterministic operational attention and priority coordination.</summary>
public interface IOperationalAttentionService
{
    Task<OperationalAttentionReportDto> GetAttentionReportAsync(CancellationToken cancellationToken = default);
    Task<OperationalAttentionSummaryDto> GetAttentionSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OperationalPriorityDto>> GetOperationalPrioritiesAsync(CancellationToken cancellationToken = default);
}
