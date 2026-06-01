using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;
using Tannous.Pos.Application.Ingredients.Commands.CreateIngredient;
using Tannous.Pos.Application.Ingredients.Commands.DeleteIngredient;
using Tannous.Pos.Application.Ingredients.Commands.UpdateIngredient;
using Tannous.Pos.Application.Ingredients.Queries.GetIngredients;
using Tannous.Pos.Application.Inventory.Queries.GetInventoryItem;
using Tannous.Pos.Application.Inventory.Queries.GetInventoryItems;
using Tannous.Pos.Application.Inventory.Queries.GetInventorySummary;
using Tannous.Pos.Application.Inventory.Queries.GetLowStockItems;
using Tannous.Pos.Application.Recipes.Commands.CreateRecipe;
using Tannous.Pos.Application.Recipes.Commands.DeleteRecipe;
using Tannous.Pos.Application.Recipes.Commands.UpdateRecipe;
using Tannous.Pos.Application.Recipes.Queries.GetRecipes;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PolicyConstants.CanManageUsers)]
public class InventoryController : ControllerBase
{
    private readonly IMediator         _mediator;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IDeviceValidator  _deviceValidator;

    public InventoryController(
        IMediator         mediator,
        IIdempotencyStore idempotencyStore,
        IDeviceValidator  deviceValidator)
    {
        _mediator         = mediator;
        _idempotencyStore = idempotencyStore;
        _deviceValidator  = deviceValidator;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetInventoryItems()
    {
        var result = await _mediator.Send(new GetInventoryItemsQuery());
        return Ok(result);
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetLowStockItems()
    {
        var result = await _mediator.Send(new GetLowStockItemsQuery());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InventoryItemDto>> GetInventoryItem(Guid id)
    {
        var result = await _mediator.Send(new GetInventoryItemQuery { Id = id });
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<IEnumerable<InventorySummaryDto>>> GetInventorySummary()
    {
        var result = await _mediator.Send(new GetInventorySummaryQuery());
        return Ok(result);
    }

    // Ingredients endpoints
    [HttpGet("ingredients")]
    public async Task<ActionResult<IEnumerable<IngredientDto>>> GetIngredients()
    {
        var result = await _mediator.Send(new GetIngredientsQuery());
        return Ok(result);
    }

    [HttpPost("ingredients")]
    [Authorize(Policy = PolicyConstants.CanManageUsers)]
    public async Task<ActionResult<IngredientDto>> CreateIngredient([FromBody] CreateIngredientDto createIngredientDto)
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
        var endpoint = "POST /api/inventory/ingredients";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok(System.Text.Json.JsonSerializer.Deserialize<IngredientDto>(existingResponse));
        }

        var command = new CreateIngredientCommand { Ingredient = createIngredientDto };
        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return CreatedAtAction(nameof(GetIngredients), new { }, result);
    }

    [HttpPut("ingredients/{id}")]
    [Authorize(Policy = PolicyConstants.CanManageUsers)]
    public async Task<ActionResult<IngredientDto>> UpdateIngredient(Guid id, [FromBody] UpdateIngredientDto updateIngredientDto)
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
        var endpoint = $"PUT /api/inventory/ingredients/{id}";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok(System.Text.Json.JsonSerializer.Deserialize<IngredientDto>(existingResponse));
        }

        var command = new UpdateIngredientCommand { Id = id, Ingredient = updateIngredientDto };
        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return Ok(result);
    }

    [HttpDelete("ingredients/{id}")]
    [Authorize(Policy = PolicyConstants.CanManageUsers)]
    public async Task<ActionResult> DeleteIngredient(Guid id, [FromQuery] bool force = false)
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
        var endpoint = $"DELETE /api/inventory/ingredients/{id}";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok();
        }

        var command = new DeleteIngredientCommand { Id = id, Force = force };
        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return NoContent();
    }

    // Recipes endpoints
    [HttpGet("recipes")]
    public async Task<ActionResult<IEnumerable<RecipeDto>>> GetRecipes()
    {
        var result = await _mediator.Send(new GetRecipesQuery());
        return Ok(result);
    }

    [HttpPost("recipes")]
    [Authorize(Policy = PolicyConstants.CanManageUsers)]
    public async Task<ActionResult<RecipeDto>> CreateRecipe([FromBody] CreateRecipeDto createRecipeDto)
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
        var endpoint = "POST /api/inventory/recipes";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok(System.Text.Json.JsonSerializer.Deserialize<RecipeDto>(existingResponse));
        }

        var command = new CreateRecipeCommand { Recipe = createRecipeDto };
        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return CreatedAtAction(nameof(GetRecipes), new { }, result);
    }

    [HttpPut("recipes/{id}")]
    [Authorize(Policy = PolicyConstants.CanManageUsers)]
    public async Task<ActionResult<RecipeDto>> UpdateRecipe(Guid id, [FromBody] UpdateRecipeDto updateRecipeDto)
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
        var endpoint = $"PUT /api/inventory/recipes/{id}";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok(System.Text.Json.JsonSerializer.Deserialize<RecipeDto>(existingResponse));
        }

        var command = new UpdateRecipeCommand { Id = id, Recipe = updateRecipeDto };
        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return Ok(result);
    }

    [HttpDelete("recipes/{id}")]
    [Authorize(Policy = PolicyConstants.CanManageUsers)]
    public async Task<ActionResult> DeleteRecipe(Guid id, [FromQuery] bool force = false)
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
        var endpoint = $"DELETE /api/inventory/recipes/{id}";

        // Check if already processed
        var existingResponse = await _idempotencyStore.GetResponseAsync(idempotencyKey, endpoint);
        if (existingResponse != null)
        {
            return Ok();
        }

        var command = new DeleteRecipeCommand { Id = id, Force = force };
        var result = await _mediator.Send(command);

        // Store response for idempotency
        var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
        await _idempotencyStore.StoreResponseAsync(idempotencyKey, endpoint, responseJson);

        return NoContent();
    }
}
