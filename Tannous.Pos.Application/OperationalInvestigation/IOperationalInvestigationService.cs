namespace Tannous.Pos.Application.OperationalInvestigation;

/// <summary>
/// Correlated investigation view that combines entity health, audit highlights,
/// and system cognition context for a specific order.
/// Advisory and read-only — does not trigger recomputation of cognition layers.
/// </summary>
public interface IOperationalInvestigationService
{
    Task<OperationalOrderInvestigationDto> GetOrderInvestigationAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<OperationalDeviceInvestigationDto> GetDeviceInvestigationAsync(
        string deviceId,
        CancellationToken cancellationToken = default);
}
