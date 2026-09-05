using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
// Dual route: the versioned form is the convention for new clients, and the unversioned form is
// retained because device registration is done by hand when provisioning a tablet and may exist
// in someone's saved request. No code in this repository calls either form.
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
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
