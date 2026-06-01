using MediatR;
using Tannous.Pos.Application.DTOs.Catalog;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Catalog.Queries.GetCategory;

public class GetCategoryQueryHandler : IRequestHandler<GetCategoryQuery, CategoryDto?>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoryQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryDto?> Handle(
        GetCategoryQuery query, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(query.Id);
        if (category == null) return null;
        return MapToDto(category);
    }

    private static CategoryDto MapToDto(Category c) => new()
    {
        Id           = c.Id,
        Name         = c.Name,
        Description  = c.Description,
        IsActive     = c.IsActive,
        DisplayOrder = c.DisplayOrder,
        CreatedAt    = c.CreatedAt
    };
}
