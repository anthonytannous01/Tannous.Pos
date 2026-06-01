using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PolicyConstants.CanManageUsers)]
public class DevicesController : ControllerBase
{
    private readonly IDeviceValidator _deviceValidator;

    public DevicesController(IDeviceValidator deviceValidator)
    {
        _deviceValidator = deviceValidator;
    }

    [HttpPost("register")]
    public async Task<ActionResult<DeviceRegistrationResponse>> RegisterDevice([FromBody] DeviceRegistrationRequest request)
    {
        var deviceId = await _deviceValidator.RegisterDeviceAsync(request.Name);

        return Ok(new DeviceRegistrationResponse
        {
            DeviceId = deviceId,
            RegisteredAt = DateTime.UtcNow
        });
    }
}

public class DeviceRegistrationRequest
{
    public string Name { get; set; } = string.Empty;
}

public class DeviceRegistrationResponse
{
    public string DeviceId { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
}
