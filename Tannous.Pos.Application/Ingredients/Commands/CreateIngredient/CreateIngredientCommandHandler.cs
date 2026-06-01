using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Ingredients.Commands.CreateIngredient;

public class CreateIngredientCommandHandler : IRequestHandler<CreateIngredientCommand, IngredientDto>
{
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateIngredientCommandHandler(IIngredientRepository ingredientRepository, IUnitOfWork unitOfWork)
    {
        _ingredientRepository = ingredientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IngredientDto> Handle(CreateIngredientCommand request, CancellationToken cancellationToken)
    {
        var ingredient = new Ingredient
        {
            Name = request.Ingredient.Name,
            Description = request.Ingredient.Description,
            CostPerUnit = request.Ingredient.CostPerUnit,
            Unit = request.Ingredient.Unit,
            IsActive = request.Ingredient.IsActive
        };

        await _ingredientRepository.AddAsync(ingredient);
        await _unitOfWork.SaveChangesAsync();

        return new IngredientDto
        {
            Id = ingredient.Id,
            Name = ingredient.Name,
            Description = ingredient.Description,
            CostPerUnit = ingredient.CostPerUnit,
            Unit = ingredient.Unit,
            IsActive = ingredient.IsActive,
            CreatedAt = ingredient.CreatedAt
        };
    }
}
