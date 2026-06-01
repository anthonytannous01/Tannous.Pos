namespace Tannous.Pos.Domain.Interfaces;

public interface IDeviceValidator
{
    Task<bool> IsDeviceActiveAsync(string deviceId, CancellationToken cancellationToken = default);
    Task<string> RegisterDeviceAsync(string name, CancellationToken cancellationToken = default);
}
