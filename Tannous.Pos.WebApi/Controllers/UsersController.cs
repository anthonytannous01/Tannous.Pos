using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Tannous.Pos.Application.DTOs.Users;
using Tannous.Pos.Application.Users.Commands.CreateUser;
using Tannous.Pos.Application.Users.Commands.ResetPassword;
using Tannous.Pos.Application.Users.Commands.SetUserStatus;
using Tannous.Pos.Application.Users.Queries.GetUserById;
using Tannous.Pos.Application.Users.Queries.ListUsers;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(Policy = PolicyConstants.CanManageUsers)]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [EnableRateLimiting("MutationsPerDevice")]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto createUserDto)
    {
        try
        {
            var command = new CreateUserCommand
            {
                User = createUserDto
            };

            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetUser), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already taken") || ex.Message.Contains("already registered"))
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult> ListUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var query = new ListUsersQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        var query = new GetUserByIdQuery { UserId = id };
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound(new { message = $"User with ID {id} not found" });

        return Ok(result);
    }

    [HttpPatch("{id}/status")]
    [EnableRateLimiting("MutationsPerDevice")]
    public async Task<ActionResult<UserDto>> SetUserStatus(Guid id, [FromBody] SetUserStatusDto request)
    {
        try
        {
            var command = new SetUserStatusCommand
            {
                UserId = id,
                IsActive = request.IsActive
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/reset-password")]
    [EnableRateLimiting("MutationsPerDevice")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordDto request)
    {
        try
        {
            var command = new ResetPasswordCommand
            {
                UserId = id,
                NewPassword = request.NewPassword
            };

            await _mediator.Send(command);
            return Ok(new { message = "Password reset successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

