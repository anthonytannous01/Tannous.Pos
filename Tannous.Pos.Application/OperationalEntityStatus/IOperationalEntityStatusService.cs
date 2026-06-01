namespace Tannous.Pos.Application.OperationalEntityStatus;

/// <summary>Pre-correlated health assessment for a specific order or device entity.</summary>
public interface IOperationalEntityStatusService
{
    Task<OperationalOrderStatusDto> GetOrderStatusAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<OperationalDeviceStatusDto> GetDeviceStatusAsync(
        string deviceId,
        CancellationToken cancellationToken = default);
}
