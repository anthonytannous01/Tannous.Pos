namespace Tannous.Pos.Application.OperationalHandoff;

/// <summary>Deterministic operator handoff continuity from bounded snapshot window history.</summary>
public interface IOperationalHandoffService
{
    Task<OperationalHandoffContinuityDto> GetHandoffContinuityAsync(CancellationToken cancellationToken = default);
    Task<OperationalHandoffSummaryDto> GetHandoffSummaryAsync(CancellationToken cancellationToken = default);
}
