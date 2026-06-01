using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Ingredients.Queries.GetIngredients;

public class GetIngredientsQueryHandler : IRequestHandler<GetIngredientsQuery, IEnumerable<IngredientDto>>
{
    private readonly IIngredientRepository _ingredientRepository;

    public GetIngredientsQueryHandler(IIngredientRepository ingredientRepository)
    {
        _ingredientRepository = ingredientRepository;
    }

    public async Task<IEnumerable<IngredientDto>> Handle(
        GetIngredientsQuery query, CancellationToken cancellationToken)
    {
        // GetActiveIngredientsAsync returns flat Ingredient entities — IngredientDto has no navigation fields
        var ingredients = await _ingredientRepository.GetActiveIngredientsAsync();
        return ingredients.Select(MapToDto).ToList();
    }

    private static IngredientDto MapToDto(Ingredient i) => new()
    {
        Id          = i.Id,
        Name        = i.Name,
        Description = i.Description,
        CostPerUnit = i.CostPerUnit,
        Unit        = i.Unit,
        IsActive    = i.IsActive,
        CreatedAt   = i.CreatedAt
    };
}
