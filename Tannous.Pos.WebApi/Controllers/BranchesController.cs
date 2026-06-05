using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.Branches.Commands.CreateBranch;
using Tannous.Pos.Application.Branches.Queries.GetBranches;
using Tannous.Pos.Application.DTOs.Branches;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/branches")]
[ApiVersion("1.0")]
[Authorize(Policy = PolicyConstants.CanSell)]
public class BranchesController : ControllerBase
{
    private readonly IMediator _mediator;

    public BranchesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Returns all branches (active only by default).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BranchDto>>> GetBranches(
        [FromQuery] bool activeOnly = true, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetBranchesQuery { ActiveOnly = activeOnly }, ct));

    /// <summary>Create a new branch.</summary>
    [HttpPost]
    [Authorize(Policy = PolicyConstants.CanManageCatalog)]
    public async Task<ActionResult<BranchDto>> CreateBranch(
        [FromBody] CreateBranchCommand command, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetBranches), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
