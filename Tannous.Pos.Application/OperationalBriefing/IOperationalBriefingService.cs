namespace Tannous.Pos.Application.OperationalBriefing;

/// <summary>Deterministic operator briefing from existing cognition snapshot stores.</summary>
public interface IOperationalBriefingService
{
    Task<OperationalBriefingPackageDto> GetBriefingPackageAsync(CancellationToken cancellationToken = default);
    Task<OperationalBriefingSummaryDto> GetBriefingSummaryAsync(CancellationToken cancellationToken = default);
}
