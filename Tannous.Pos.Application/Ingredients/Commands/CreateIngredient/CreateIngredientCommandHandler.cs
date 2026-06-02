using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Ingredients.Commands.CreateIngredient;

public class CreateIngredientCommandHandler : IRequestHandler<CreateIngredientCommand, IngredientDto>
{
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateIngredientCommandHandler(
        IIngredientRepository ingredientRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _ingredientRepository = ingredientRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IngredientDto> Handle(CreateIngredientCommand request, CancellationToken cancellationToken)
    {
        var ingredient = new Ingredient
        {
            Name        = request.Ingredient.Name,
            Description = request.Ingredient.Description,
            CostPerUnit = request.Ingredient.CostPerUnit,
            Unit        = request.Ingredient.Unit,
            IsActive    = request.Ingredient.IsActive
        };

        await _ingredientRepository.AddAsync(ingredient);

        // Auto-seed a zero-stock InventoryItem so the ingredient appears in the
        // stock screen immediately. Owner can adjust stock from the mobile app.
        var inventoryItem = new InventoryItem
        {
            IngredientId = ingredient.Id,
            CurrentStock = 0,
            MinimumStock = 0,
            MaximumStock = 0,
            AverageCost  = request.Ingredient.CostPerUnit,
            Unit         = request.Ingredient.Unit,
            LastUpdated  = DateTime.UtcNow
        };

        await _inventoryRepository.AddAsync(inventoryItem);
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
