using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Tannous.Pos.Application.DTOs.Suppliers;
using Tannous.Pos.Application.DTOs.Inventory;
using Tannous.Pos.Application.DTOs.Purchasing;
using Tannous.Pos.Application.GoodsReceipts.Commands.CreateGoodsReceipt;
using Tannous.Pos.Application.PurchaseOrders.Commands.CreatePurchaseOrder;
using Tannous.Pos.Application.PurchaseOrders.Commands.SubmitPurchaseOrder;
using Tannous.Pos.Application.PurchaseOrders.Queries.GetPurchaseOrders;
using Tannous.Pos.Application.Purchasing.Commands.AssignIngredientToSupplier;
using Tannous.Pos.Application.Purchasing.Commands.CreateSuggestedOrders;
using Tannous.Pos.Application.Purchasing.Commands.UnassignIngredientFromSupplier;
using Tannous.Pos.Application.Purchasing.Queries.GetSupplierIntelligence;
using Tannous.Pos.Application.Suppliers.Commands.CreateSupplier;
using Tannous.Pos.Application.Suppliers.Commands.DeleteSupplier;
using Tannous.Pos.Application.Suppliers.Commands.UpdateSupplier;
using Tannous.Pos.Application.Suppliers.Queries.GetSupplier;
using Tannous.Pos.Application.Suppliers.Queries.GetSuppliers;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PolicyConstants.CanManageUsers)]
public class SuppliersController : ControllerBase
{
    private readonly IMediator         _mediator;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IDeviceValidator  _deviceValidator;

    public SuppliersController(
        IMediator         mediator,
        IIdempotencyStore idempotencyStore,
        IDeviceValidator  deviceValidator)
    {
        _mediator         = mediator;
        _idempotencyStore = idempotencyStore;
        _deviceValidator  = deviceValidator;
    }

    // Suppliers endpoints
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SupplierDto>>> GetSuppliers()
    {
        var result = await _mediator.Send(new GetSuppliersQuery());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SupplierDto>> GetSupplier(Guid id)
    {
        var result = await _mediator.Send(new GetSupplierQuery { Id = id });
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<SupplierDto>> CreateSupplier([FromBody] CreateSupplierDto createSupplierDto)
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
        var endpoint = "POST /api/suppliers";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok(System.Text.Json.JsonSerializer.Deserialize<SupplierDto>(existingResponse));
        }

        var command = new CreateSupplierCommand { Supplier = createSupplierDto };
        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return CreatedAtAction(nameof(GetSupplier), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SupplierDto>> UpdateSupplier(Guid id, [FromBody] UpdateSupplierDto updateSupplierDto)
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
        var endpoint = $"PUT /api/suppliers/{id}";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok(System.Text.Json.JsonSerializer.Deserialize<SupplierDto>(existingResponse));
        }

        var command = new UpdateSupplierCommand { Id = id, Supplier = updateSupplierDto };
        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteSupplier(Guid id, [FromQuery] bool force = false)
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
        var endpoint = $"DELETE /api/suppliers/{id}";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok();
        }

        var command = new DeleteSupplierCommand { Id = id, Force = force };
        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return NoContent();
    }

    // Purchase Orders endpoints
    [HttpGet("purchase-orders")]
    public async Task<ActionResult<IEnumerable<PurchaseOrderDto>>> GetPurchaseOrders()
    {
        var result = await _mediator.Send(new GetPurchaseOrdersQuery());
        return Ok(result);
    }

    [HttpPost("purchase-orders")]
    public async Task<ActionResult<PurchaseOrderDto>> CreatePurchaseOrder([FromBody] CreatePurchaseOrderDto createPurchaseOrderDto)
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
        var endpoint = "POST /api/suppliers/purchase-orders";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok(System.Text.Json.JsonSerializer.Deserialize<PurchaseOrderDto>(existingResponse));
        }

        var command = new CreatePurchaseOrderCommand { PurchaseOrder = createPurchaseOrderDto };
        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return CreatedAtAction(nameof(GetPurchaseOrders), new { }, result);
    }

    [HttpPost("purchase-orders/{id}/submit")]
    public async Task<ActionResult<PurchaseOrderDto>> SubmitPurchaseOrder(Guid id)
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
        var endpoint = $"POST /api/suppliers/purchase-orders/{id}/submit";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok(System.Text.Json.JsonSerializer.Deserialize<PurchaseOrderDto>(existingResponse));
        }

        var command = new SubmitPurchaseOrderCommand { Id = id };
        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return Ok(result);
    }

    // Goods Receipts endpoints
    [HttpPost("goods-receipts")]
    public async Task<ActionResult<GoodsReceiptDto>> CreateGoodsReceipt([FromBody] CreateGoodsReceiptDto createGoodsReceiptDto)
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
        var endpoint = "POST /api/suppliers/goods-receipts";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok(System.Text.Json.JsonSerializer.Deserialize<GoodsReceiptDto>(existingResponse));
        }

        var command = new CreateGoodsReceiptCommand { GoodsReceipt = createGoodsReceiptDto };
        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return CreatedAtAction(nameof(GetPurchaseOrders), new { }, result);
    }

    [HttpGet("intelligence")]
    public async Task<ActionResult<SupplierIntelligenceDto>> GetIntelligence(
        [FromQuery] int forecastDays = 7,
        [FromQuery] Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetSupplierIntelligenceQuery
        {
            ForecastDays = forecastDays,
            BranchId     = branchId
        }, cancellationToken);

        return Ok(result);
    }

    [HttpPost("intelligence/create-orders")]
    public async Task<ActionResult<CreateSuggestedOrdersResult>> CreateSuggestedOrders(
        [FromBody] CreateSuggestedOrdersDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new CreateSuggestedOrdersCommand
        {
            ForecastDays = request.ForecastDays,
            BranchId     = request.BranchId,
            SupplierIds  = request.SupplierIds
        }, cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{supplierId:guid}/ingredients/{ingredientId:guid}")]
    public async Task<ActionResult<IngredientDto>> AssignIngredient(
        Guid supplierId,
        Guid ingredientId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new AssignIngredientToSupplierCommand
        {
            SupplierId   = supplierId,
            IngredientId = ingredientId
        }, cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{supplierId:guid}/ingredients/{ingredientId:guid}")]
    public async Task<IActionResult> UnassignIngredient(
        Guid supplierId,
        Guid ingredientId,
        CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new UnassignIngredientFromSupplierCommand
        {
            SupplierId   = supplierId,
            IngredientId = ingredientId
        }, cancellationToken);

        return NoContent();
    }
}
