using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.DTOs.Catalog;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Application.Catalog.Commands.CreateCategory;
using Tannous.Pos.Application.Catalog.Commands.UpdateCategory;
using Tannous.Pos.Application.Catalog.Commands.DeleteCategory;
using Tannous.Pos.Application.Catalog.Commands.CreateMenuItem;
using Tannous.Pos.Application.Catalog.Commands.UpdateMenuItem;
using Tannous.Pos.Application.Catalog.Commands.DeleteMenuItem;
using Tannous.Pos.Application.Catalog.Commands.CreateAddOn;
using Tannous.Pos.Application.Catalog.Commands.UpdateAddOn;
using Tannous.Pos.Application.Catalog.Commands.DeleteAddOn;
using Tannous.Pos.Application.Catalog.Queries.GetCategories;
using Tannous.Pos.Application.Catalog.Queries.GetCategory;
using Tannous.Pos.Application.Catalog.Queries.GetMenuItems;
using Tannous.Pos.Application.Catalog.Queries.GetMenuItem;
using Tannous.Pos.Application.Catalog.Queries.GetAddOns;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(Policy = PolicyConstants.CanSell)]
public class CatalogController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IDeviceValidator _deviceValidator;

    public CatalogController(
        IMediator mediator,
        IIdempotencyStore idempotencyStore,
        IDeviceValidator deviceValidator)
    {
        _mediator = mediator;
        _idempotencyStore = idempotencyStore;
        _deviceValidator = deviceValidator;
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
    {
        var result = await _mediator.Send(new GetCategoriesQuery());
        return Ok(result);
    }

    [HttpGet("categories/{id}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(Guid id)
    {
        var result = await _mediator.Send(new GetCategoryQuery { Id = id });
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("menu-items")]
    public async Task<ActionResult<IEnumerable<MenuItemDto>>> GetMenuItems([FromQuery] Guid? categoryId = null)
    {
        var result = await _mediator.Send(new GetMenuItemsQuery { CategoryId = categoryId });
        return Ok(result);
    }

    [HttpGet("menu-items/{id}")]
    public async Task<ActionResult<MenuItemDto>> GetMenuItem(Guid id)
    {
        var result = await _mediator.Send(new GetMenuItemQuery { Id = id });
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    // Category CRUD endpoints
    [HttpPost("categories")]
    [Authorize(Policy = PolicyConstants.CanManageCatalog)]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryDto createCategoryDto)
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
        var endpoint = "POST /api/catalog/categories";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok(System.Text.Json.JsonSerializer.Deserialize<CategoryDto>(existingResponse));
        }

        var command = new CreateCategoryCommand { Category = createCategoryDto };
        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return CreatedAtAction(nameof(GetCategory), new { id = result.Id }, result);
    }

    [HttpPut("categories/{id}")]
    [Authorize(Policy = PolicyConstants.CanManageCatalog)]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto updateCategoryDto)
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
        var endpoint = $"PUT /api/catalog/categories/{id}";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok(System.Text.Json.JsonSerializer.Deserialize<CategoryDto>(existingResponse));
        }

        var command = new UpdateCategoryCommand { Id = id, Category = updateCategoryDto };
        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return Ok(result);
    }

    [HttpDelete("categories/{id}")]
    [Authorize(Policy = PolicyConstants.CanManageCatalog)]
    public async Task<ActionResult> DeleteCategory(Guid id, [FromQuery] bool force = false)
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
        var endpoint = $"DELETE /api/catalog/categories/{id}";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok();
        }

        var command = new DeleteCategoryCommand { Id = id, Force = force };
        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return NoContent();
    }

    // MenuItem CRUD endpoints
    [HttpPost("menu-items")]
    [Authorize(Policy = PolicyConstants.CanManageCatalog)]
    public async Task<ActionResult<MenuItemDto>> CreateMenuItem([FromBody] CreateMenuItemDto createMenuItemDto)
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
        var endpoint = "POST /api/catalog/menu-items";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok(System.Text.Json.JsonSerializer.Deserialize<MenuItemDto>(existingResponse));
        }

        var command = new CreateMenuItemCommand { MenuItem = createMenuItemDto };
        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return CreatedAtAction(nameof(GetMenuItem), new { id = result.Id }, result);
    }

    [HttpPut("menu-items/{id}")]
    [Authorize(Policy = PolicyConstants.CanManageCatalog)]
    public async Task<ActionResult<MenuItemDto>> UpdateMenuItem(Guid id, [FromBody] UpdateMenuItemDto updateMenuItemDto)
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
        var endpoint = $"PUT /api/catalog/menu-items/{id}";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok(System.Text.Json.JsonSerializer.Deserialize<MenuItemDto>(existingResponse));
        }

        var command = new UpdateMenuItemCommand { Id = id, MenuItem = updateMenuItemDto };
        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return Ok(result);
    }

    [HttpDelete("menu-items/{id}")]
    [Authorize(Policy = PolicyConstants.CanManageCatalog)]
    public async Task<ActionResult> DeleteMenuItem(Guid id, [FromQuery] bool force = false)
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
        var endpoint = $"DELETE /api/catalog/menu-items/{id}";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok();
        }

        var command = new DeleteMenuItemCommand { Id = id, Force = force };
        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return NoContent();
    }

    // AddOn endpoints
    [HttpGet("addons")]
    public async Task<ActionResult<IEnumerable<AddOnDto>>> GetAddOns()
    {
        var result = await _mediator.Send(new GetAddOnsQuery());
        return Ok(result);
    }

    [HttpPost("addons")]
    [Authorize(Policy = PolicyConstants.CanManageCatalog)]
    public async Task<ActionResult<AddOnDto>> CreateAddOn([FromBody] CreateAddOnDto createAddOnDto)
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
        var endpoint = "POST /api/catalog/addons";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok(System.Text.Json.JsonSerializer.Deserialize<AddOnDto>(existingResponse));
        }

        var command = new CreateAddOnCommand { AddOn = createAddOnDto };
        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return CreatedAtAction(nameof(GetAddOns), new { }, result);
    }

    [HttpPut("addons/{id}")]
    [Authorize(Policy = PolicyConstants.CanManageCatalog)]
    public async Task<ActionResult<AddOnDto>> UpdateAddOn(Guid id, [FromBody] UpdateAddOnDto updateAddOnDto)
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
        var endpoint = $"PUT /api/catalog/addons/{id}";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok(System.Text.Json.JsonSerializer.Deserialize<AddOnDto>(existingResponse));
        }

        var command = new UpdateAddOnCommand { Id = id, AddOn = updateAddOnDto };
        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return Ok(result);
    }

    [HttpDelete("addons/{id}")]
    [Authorize(Policy = PolicyConstants.CanManageCatalog)]
    public async Task<ActionResult> DeleteAddOn(Guid id, [FromQuery] bool force = false)
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
        var endpoint = $"DELETE /api/catalog/addons/{id}";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok();
        }

        var command = new DeleteAddOnCommand { Id = id, Force = force };
        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return NoContent();
    }
}
