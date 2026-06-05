using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tannous.Pos.Application.DTOs.Shifts;
using Tannous.Pos.Application.Shifts.Commands.CashDrop;
using Tannous.Pos.Application.Shifts.Commands.CloseShift;
using Tannous.Pos.Application.Shifts.Commands.KickCashDrawer;
using Tannous.Pos.Application.Shifts.Commands.OpenShift;
using Tannous.Pos.Application.Shifts.Queries.GetCurrentShift;
using Tannous.Pos.Application.Shifts.Queries.GetShifts;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(Policy = PolicyConstants.CanManageShifts)]
public class ShiftsController : ControllerBase
{
    private readonly IMediator          _mediator;
    private readonly IIdempotencyStore  _idempotencyStore;
    private readonly IDeviceValidator   _deviceValidator;

    public ShiftsController(
        IMediator         mediator,
        IIdempotencyStore idempotencyStore,
        IDeviceValidator  deviceValidator)
    {
        _mediator         = mediator;
        _idempotencyStore = idempotencyStore;
        _deviceValidator  = deviceValidator;
    }

    [HttpPost("open")]
    public async Task<ActionResult<ShiftDto>> OpenShift([FromBody] OpenShiftRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return BadRequest("Invalid user token");

        var command = new OpenShiftCommand
        {
            OpeningBalance = request.OpeningBalance,
            UserId         = userId,
            Notes          = request.Notes,
            BranchId       = request.BranchId
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("current")]
    public async Task<ActionResult<ShiftDto>> GetCurrentShift()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return BadRequest("Invalid user token");

        var result = await _mediator.Send(new GetCurrentShiftQuery { UserId = userId });
        if (result == null) return NotFound("No open shift found");
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShiftDto>>> GetShifts(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate   = null,
        [FromQuery] Guid?     branchId  = null)
    {
        var result = await _mediator.Send(new GetShiftsQuery
        {
            StartDate = startDate,
            EndDate   = endDate,
            BranchId  = branchId
        });
        return Ok(result);
    }

    [HttpPost("{id}/close")]
    public async Task<ActionResult<ShiftDto>> CloseShift(Guid id, [FromBody] CloseShiftRequest request)
    {
        // Validate Device-Id header
        if (!Request.Headers.TryGetValue("Device-Id", out var deviceIdHeader) || string.IsNullOrEmpty(deviceIdHeader))
            return BadRequest("Device-Id header is required");

        var deviceId = deviceIdHeader.ToString();
        if (!await _deviceValidator.IsDeviceActiveAsync(deviceId))
            return Forbid("Device is not active");

        // Validate Idempotency-Key header
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKeyHeader) || string.IsNullOrEmpty(idempotencyKeyHeader))
            return BadRequest("Idempotency-Key header is required");

        var idempotencyKey = idempotencyKeyHeader.ToString();
        var endpoint = $"POST /api/shifts/{id}/close";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok(System.Text.Json.JsonSerializer.Deserialize<ShiftDto>(existingResponse));
        }

        var command = new CloseShiftCommand
        {
            ShiftId        = id,
            ClosingCount   = request.ClosingCount,
            Note           = request.Note,
            IdempotencyKey = idempotencyKey
        };

        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return Ok(result);
    }

    [HttpPost("{id}/cash-drop")]
    public async Task<ActionResult<CashDrawerEventDto>> CashDrop(Guid id, [FromBody] CashDropRequest request)
    {
        // Validate Device-Id header
        if (!Request.Headers.TryGetValue("Device-Id", out var deviceIdHeader) || string.IsNullOrEmpty(deviceIdHeader))
            return BadRequest("Device-Id header is required");

        var deviceId = deviceIdHeader.ToString();
        if (!await _deviceValidator.IsDeviceActiveAsync(deviceId))
            return Forbid("Device is not active");

        // Validate Idempotency-Key header
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKeyHeader) || string.IsNullOrEmpty(idempotencyKeyHeader))
            return BadRequest("Idempotency-Key header is required");

        var idempotencyKey = idempotencyKeyHeader.ToString();
        var endpoint = $"POST /api/shifts/{id}/cash-drop";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok(System.Text.Json.JsonSerializer.Deserialize<CashDrawerEventDto>(existingResponse));
        }

        var command = new CashDropCommand
        {
            ShiftId        = id,
            Amount         = request.Amount,
            Note           = request.Note,
            IdempotencyKey = idempotencyKey
        };

        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return Ok(result);
    }

    [HttpPost("cashdrawer/kick")]
    public async Task<ActionResult<CashDrawerEventDto>> KickCashDrawer([FromBody] KickCashDrawerRequest request)
    {
        // Validate Device-Id header
        if (!Request.Headers.TryGetValue("Device-Id", out var deviceIdHeader) || string.IsNullOrEmpty(deviceIdHeader))
            return BadRequest("Device-Id header is required");

        var deviceId = deviceIdHeader.ToString();
        if (!await _deviceValidator.IsDeviceActiveAsync(deviceId))
            return Forbid("Device is not active");

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return BadRequest("Invalid user token");

        var result = await _mediator.Send(new KickCashDrawerCommand
        {
            UserId    = userId,
            EventType = request.EventType,
            Amount    = request.Amount,
            Note      = request.Note
        });

        if (!result.ShiftFound) return BadRequest("No open shift found");
        return Ok(result.Event);
    }
}

public class OpenShiftRequest
{
    public decimal OpeningBalance { get; set; }
    public string? Notes { get; set; }
    /// <summary>Optional branch override. Defaults to the system default branch.</summary>
    public Guid? BranchId { get; set; }
}

public class CloseShiftRequest
{
    public decimal ClosingCount { get; set; }
    public string? Note { get; set; }
}

public class CashDropRequest
{
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public class KickCashDrawerRequest
{
    public string EventType { get; set; } = string.Empty; // NoSale/Open
    public decimal? Amount { get; set; }
    public string? Note { get; set; }
}
