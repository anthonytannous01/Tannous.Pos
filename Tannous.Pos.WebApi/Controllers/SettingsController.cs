using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Tannous.Pos.Application.DTOs.Settings;
using Tannous.Pos.Application.Settings.Queries.GetSettings;
using Tannous.Pos.Application.Settings.Commands.UpdateSettings;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(Policy = PolicyConstants.CanSell)]
public class SettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SettingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<SettingsDto>> GetSettings()
    {
        var result = await _mediator.Send(new GetSettingsQuery());
        return Ok(result);
    }

    [HttpPut]
    [Authorize(Policy = PolicyConstants.CanManageSettings)]
    public async Task<ActionResult<SettingsDto>> UpdateSettings([FromBody] UpdateSettingsDto updateSettingsDto)
    {
        var result = await _mediator.Send(new UpdateSettingsCommand { Settings = updateSettingsDto });
        return Ok(result);
    }
}
