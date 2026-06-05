using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Collections.Concurrent;
using Tannous.Pos.Application.DTOs.Orders;
using Tannous.Pos.Application.Orders.Commands.CreateOrder;
using Tannous.Pos.Application.Orders.Commands.UpdateOrder;
using Tannous.Pos.Application.Orders.Commands.FinalizeOrder;
using Tannous.Pos.Application.Orders.Commands.VoidOrder;
using Tannous.Pos.Application.Orders.Queries.GetOrders;
using Tannous.Pos.Application.Orders.Queries.GetOrderById;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(Policy = PolicyConstants.CanSell)]
public class OrdersController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> IdempotencyGates = new();

    private readonly IMediator _mediator;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IDeviceValidator _deviceValidator;
    private readonly IAuditService _auditService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IMediator mediator,
        IIdempotencyStore idempotencyStore,
        IDeviceValidator deviceValidator,
        IAuditService auditService,
        ILogger<OrdersController> logger)
    {
        _mediator = mediator;
        _idempotencyStore = idempotencyStore;
        _deviceValidator = deviceValidator;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpPost]
    [EnableRateLimiting("MutationsPerDevice")]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto createOrderDto)
    {
        // Extract user ID from JWT token
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return BadRequest("Invalid user token");

        var command = new CreateOrderCommand
        {
            Order = createOrderDto,
            UserId = userId
        };

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetOrder), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders(
        [FromQuery] OrderStatus? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] Guid? shiftId = null,
        [FromQuery] Guid? branchId = null)
    {
        var query = new GetOrdersQuery
        {
            Status = status,
            StartDate = startDate,
            EndDate = endDate,
            CustomerId = customerId,
            ShiftId = shiftId,
            BranchId = branchId
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid id)
    {
        var query = new GetOrderByIdQuery { Id = id };
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }

    [HttpPut("{id}/status")]
    [EnableRateLimiting("MutationsPerDevice")]
    public async Task<ActionResult<OrderDto>> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return BadRequest("Invalid user token");

        var command = new UpdateOrderCommand
        {
            Id = id,
            Status = request.Status,
            Notes = request.Notes,
            UserId = userId
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("{id}/finalize")]
    [EnableRateLimiting("MutationsPerDevice")]
    public async Task<ActionResult<OrderDto>> FinalizeOrder(Guid id, [FromBody] FinalizeOrderRequest request)
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
        var endpoint = $"POST /api/orders/{id}/finalize";
        var gateKey = $"{endpoint}|{idempotencyKey}";
        var gate = IdempotencyGates.GetOrAdd(gateKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();

        try
        {
            // Check if already processed
            var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
            if (existingResponse != null)
            {
                _logger.LogInformation(
                    "Idempotency coordination: finalized request replay returned cached response. Endpoint={Endpoint}, IdempotencyKey={IdempotencyKey}",
                    endpoint,
                    idempotencyKey);
                return Ok(System.Text.Json.JsonSerializer.Deserialize<OrderDto>(existingResponse));
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return BadRequest("Invalid user token");

            var command = new FinalizeOrderCommand
            {
                OrderId = id,
                Payments = request.Payments,
                IdempotencyKey = idempotencyKey
            };

            var result = await _mediator.Send(command);

            await _auditService.LogEventAsync(
                "FinalizeOrder",
                "Order",
                id,
                new
                {
                    result.OrderNumber,
                    result.Status,
                    result.SubTotal,
                    result.TaxAmount,
                    result.DiscountAmount,
                    result.TotalAmount,
                    PaymentCount = request.Payments.Count,
                    TotalPayments = request.Payments.Sum(p => p.Amount)
                });

            // Store response for idempotency
            var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
            await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

            return Ok(result);
        }
        finally
        {
            gate.Release();
        }
    }

    [HttpPost("{id}/void")]
    [EnableRateLimiting("MutationsPerDevice")]
    public async Task<ActionResult<OrderDto>> VoidOrder(Guid id, [FromBody] VoidOrderRequest request)
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
        var endpoint = $"POST /api/orders/{id}/void";
        var gateKey = $"{endpoint}|{idempotencyKey}";
        var gate = IdempotencyGates.GetOrAdd(gateKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();

        try
        {
            // Check if already processed
            var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
            if (existingResponse != null)
            {
                _logger.LogInformation(
                    "Idempotency coordination: void request replay returned cached response. Endpoint={Endpoint}, IdempotencyKey={IdempotencyKey}",
                    endpoint,
                    idempotencyKey);
                return Ok(System.Text.Json.JsonSerializer.Deserialize<OrderDto>(existingResponse));
            }

            var command = new VoidOrderCommand
            {
                OrderId = id,
                Reason = request.Reason,
                IdempotencyKey = idempotencyKey
            };

            var result = await _mediator.Send(command);

            await _auditService.LogEventAsync(
                "VoidOrder",
                "Order",
                id,
                new { request.Reason, result.Status, result.TotalAmount, result.OrderNumber });

            // Store response for idempotency
            var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
            await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

            return Ok(result);
        }
        finally
        {
            gate.Release();
        }
    }
}

public class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; set; }
    public string? Notes { get; set; }
}

public class FinalizeOrderRequest
{
    public List<PaymentDto> Payments { get; set; } = new();
}

public class VoidOrderRequest
{
    public string Reason { get; set; } = string.Empty;
}
