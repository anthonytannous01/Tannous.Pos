using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tannous.Pos.Application.DTOs.Scheduling;
using Tannous.Pos.Application.Scheduling.Commands.CancelSchedule;
using Tannous.Pos.Application.Scheduling.Commands.ClockIn;
using Tannous.Pos.Application.Scheduling.Commands.ClockOut;
using Tannous.Pos.Application.Scheduling.Commands.CreateSchedule;
using Tannous.Pos.Application.Scheduling.Commands.PublishSchedule;
using Tannous.Pos.Application.Scheduling.Commands.UpdateSchedule;
using Tannous.Pos.Application.Scheduling.Queries.GetMyClockStatus;
using Tannous.Pos.Application.Scheduling.Queries.GetTimeEntries;
using Tannous.Pos.Application.Scheduling.Queries.GetWeeklySchedule;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

/// <summary>
/// Employee scheduling (planned shifts) and time tracking (clock-in/out).
/// Distinct from ShiftsController, which manages cash register sessions.
///
/// Authorization: class-level [Authorize] keeps every endpoint behind authentication;
/// manager-only endpoints add the CanManageShifts policy at method level.
/// Clock-in/out and self status are deliberately open to all authenticated staff.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
public class ScheduleController : ControllerBase
{
    private readonly IMediator _mediator;

    public ScheduleController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Weekly schedule (Monday–Sunday) with all non-cancelled entries.</summary>
    [HttpGet("week")]
    [Authorize(Policy = PolicyConstants.CanManageShifts)]
    public async Task<ActionResult<WeeklyScheduleDto>> GetWeeklySchedule(
        [FromQuery] DateTime weekStart,
        [FromQuery] Guid? branchId = null)
    {
        var result = await _mediator.Send(new GetWeeklyScheduleQuery
        {
            WeekStart = weekStart,
            BranchId  = branchId
        });
        return Ok(result);
    }

    /// <summary>Create one Draft schedule entry.</summary>
    [HttpPost]
    [Authorize(Policy = PolicyConstants.CanManageShifts)]
    public async Task<ActionResult<EmployeeScheduleDto>> CreateSchedule([FromBody] CreateScheduleCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetWeeklySchedule), new { weekStart = result.ScheduledStart.Date }, result);
    }

    /// <summary>Update a Draft schedule entry (times, position, notes).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyConstants.CanManageShifts)]
    public async Task<ActionResult<EmployeeScheduleDto>> UpdateSchedule(Guid id, [FromBody] UpdateScheduleRequest request)
    {
        var result = await _mediator.Send(new UpdateScheduleCommand
        {
            ScheduleId     = id,
            ScheduledStart = request.ScheduledStart,
            ScheduledEnd   = request.ScheduledEnd,
            Position       = request.Position,
            Notes          = request.Notes
        });
        return Ok(result);
    }

    /// <summary>Publish a batch of Draft entries. Returns the count published.</summary>
    [HttpPost("publish")]
    [Authorize(Policy = PolicyConstants.CanManageShifts)]
    public async Task<ActionResult<object>> PublishSchedule([FromBody] PublishScheduleCommand command)
    {
        var published = await _mediator.Send(command);
        return Ok(new { published });
    }

    /// <summary>Cancel one schedule entry (any status except already Cancelled).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PolicyConstants.CanManageShifts)]
    public async Task<ActionResult<EmployeeScheduleDto>> CancelSchedule(Guid id)
    {
        var result = await _mediator.Send(new CancelScheduleCommand { ScheduleId = id });
        return Ok(result);
    }

    /// <summary>Clock the current user in. 409 when already clocked in.</summary>
    [HttpPost("clock-in")]
    public async Task<ActionResult<TimeEntryDto>> ClockIn([FromBody] ClockInRequest request)
    {
        if (!TryGetUserId(out var userId))
            return BadRequest("Invalid user token");

        var result = await _mediator.Send(new ClockInCommand
        {
            UserId   = userId,
            BranchId = request.BranchId
        });
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Clock the current user out. 404 when no active entry exists.</summary>
    [HttpPost("clock-out")]
    public async Task<ActionResult<TimeEntryDto>> ClockOut([FromBody] ClockOutRequest request)
    {
        if (!TryGetUserId(out var userId))
            return BadRequest("Invalid user token");

        var result = await _mediator.Send(new ClockOutCommand
        {
            UserId       = userId,
            BranchId     = request.BranchId,
            BreakMinutes = request.BreakMinutes,
            Notes        = request.Notes
        });
        return Ok(result);
    }

    /// <summary>Time entries in a range. Managers only.</summary>
    [HttpGet("time-entries")]
    [Authorize(Policy = PolicyConstants.CanManageShifts)]
    public async Task<ActionResult<List<TimeEntryDto>>> GetTimeEntries(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? branchId = null)
    {
        var result = await _mediator.Send(new GetTimeEntriesQuery
        {
            From     = from,
            To       = to,
            UserId   = userId,
            BranchId = branchId
        });
        return Ok(result);
    }

    /// <summary>The current user's own time entries in a range — staff self-service.</summary>
    [HttpGet("my-time-entries")]
    public async Task<ActionResult<List<TimeEntryDto>>> GetMyTimeEntries(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        if (!TryGetUserId(out var userId))
            return BadRequest("Invalid user token");

        var result = await _mediator.Send(new GetTimeEntriesQuery
        {
            From   = from,
            To     = to,
            UserId = userId
        });
        return Ok(result);
    }

    /// <summary>The current user's active clock entry, or 204 when not clocked in.</summary>
    [HttpGet("my-clock-status")]
    public async Task<ActionResult<TimeEntryDto>> GetMyClockStatus()
    {
        if (!TryGetUserId(out var userId))
            return BadRequest("Invalid user token");

        var result = await _mediator.Send(new GetMyClockStatusQuery { UserId = userId });
        if (result == null) return NoContent();
        return Ok(result);
    }

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && Guid.TryParse(claim.Value, out userId);
    }
}

public class UpdateScheduleRequest
{
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
    public string? Position { get; set; }
    public string? Notes { get; set; }
}

public class ClockInRequest
{
    public Guid BranchId { get; set; }
}

public class ClockOutRequest
{
    public Guid BranchId { get; set; }
    public int? BreakMinutes { get; set; }
    public string? Notes { get; set; }
}
