using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.DTOs.Integrations;
using Tannous.Pos.Application.Integrations.ApiKeys.Commands.CreateApiKey;
using Tannous.Pos.Application.Integrations.ApiKeys.Commands.RevokeApiKey;
using Tannous.Pos.Application.Integrations.ApiKeys.Queries.GetApiKeys;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/apikeys")]
[ApiVersion("1.0")]
[Authorize(Policy = PolicyConstants.CanManageSettings)]
public class ApiKeysController : ControllerBase
{
    private readonly IMediator _mediator;

    public ApiKeysController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<ApiKeyDto>>> GetApiKeys(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetApiKeysQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CreateApiKeyResponse>> CreateApiKey(
        [FromBody] CreateApiKeyDto apiKey,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateApiKeyCommand { ApiKey = apiKey }, cancellationToken);
        return CreatedAtAction(nameof(GetApiKeys), result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RevokeApiKey(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RevokeApiKeyCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}
