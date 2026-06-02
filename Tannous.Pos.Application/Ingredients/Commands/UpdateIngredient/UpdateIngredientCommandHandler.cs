using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Ingredients.Commands.UpdateIngredient;

public class UpdateIngredientCommandHandler : IRequestHandler<UpdateIngredientCommand, IngredientDto>
{
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateIngredientCommandHandler(
        IIngredientRepository ingredientRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _ingredientRepository = ingredientRepository;
        _inventoryRepository  = inventoryRepository;
        _unitOfWork           = unitOfWork;
    }

    public async Task<IngredientDto> Handle(UpdateIngredientCommand request, CancellationToken cancellationToken)
    {
        var ingredient = await _ingredientRepository.GetByIdAsync(request.Id);
        if (ingredient == null)
            throw new ArgumentException($"Ingredient with ID {request.Id} not found");

        ingredient.Name        = request.Ingredient.Name;
        ingredient.Description = request.Ingredient.Description;
        ingredient.CostPerUnit = request.Ingredient.CostPerUnit;
        ingredient.Unit        = request.Ingredient.Unit;
        ingredient.IsActive    = request.Ingredient.IsActive;
        ingredient.UpdatedAt   = DateTime.UtcNow;

        await _ingredientRepository.UpdateAsync(ingredient);

        // Keep InventoryItem in sync with ingredient cost and unit so that
        // the stock screen and future COGS movements reflect the latest values.
        var inventoryItem = await _inventoryRepository.GetByIngredientAsync(request.Id);
        if (inventoryItem != null)
        {
            inventoryItem.AverageCost = request.Ingredient.CostPerUnit;
            inventoryItem.Unit        = request.Ingredient.Unit;
            inventoryItem.LastUpdated = DateTime.UtcNow;
            await _inventoryRepository.UpdateAsync(inventoryItem);
        }

        await _unitOfWork.SaveChangesAsync();

        return new IngredientDto
        {
            Id          = ingredient.Id,
            Name        = ingredient.Name,
            Description = ingredient.Description,
            CostPerUnit = ingredient.CostPerUnit,
            Unit        = ingredient.Unit,
            IsActive    = ingredient.IsActive,
            CreatedAt   = ingredient.CreatedAt
        };
    }
}
