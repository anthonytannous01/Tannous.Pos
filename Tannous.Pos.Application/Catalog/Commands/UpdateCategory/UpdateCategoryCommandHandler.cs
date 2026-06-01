using MediatR;
using Tannous.Pos.Application.DTOs.Catalog;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Catalog.Commands.UpdateCategory;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategoryCommandHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id);
        if (category == null)
            throw new ArgumentException($"Category with ID {request.Id} not found");

        category.Name = request.Category.Name;
        category.Description = request.Category.Description;
        category.DisplayOrder = request.Category.DisplayOrder;
        category.IsActive = request.Category.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _categoryRepository.UpdateAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            DisplayOrder = category.DisplayOrder,
            CreatedAt = category.CreatedAt
        };
    }
}
