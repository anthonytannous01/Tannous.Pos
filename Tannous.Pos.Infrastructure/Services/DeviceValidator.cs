using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Services;

public class DeviceValidator : IDeviceValidator
{
    private readonly PosDbContext _context;

    public DeviceValidator(PosDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsDeviceActiveAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        // In Development, skip DB check so any device can hit mutation endpoints.
        // Remove this bypass before deploying to staging/prod.
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            return !string.IsNullOrWhiteSpace(deviceId);

        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId, cancellationToken);

        return device?.IsActive == true;
    }

    public async Task<string> RegisterDeviceAsync(string name, CancellationToken cancellationToken = default)
    {
        var deviceId = Guid.NewGuid().ToString();

        var device = new Device
        {
            DeviceId = deviceId,
            Name = name,
            DeviceType = "POS",
            IsActive = true
        };

        _context.Devices.Add(device);
        await _context.SaveChangesAsync(cancellationToken);

        return deviceId;
    }
}
