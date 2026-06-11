using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Inventory;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Purchasing.Commands.AssignIngredientToSupplier;

public class AssignIngredientToSupplierCommandHandler
    : IRequestHandler<AssignIngredientToSupplierCommand, IngredientDto>
{
    private readonly DbContext _dbContext;

    public AssignIngredientToSupplierCommandHandler(DbContext dbContext) => _dbContext = dbContext;

    public async Task<IngredientDto> Handle(
        AssignIngredientToSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = await _dbContext.Set<Supplier>()
            .FirstOrDefaultAsync(s => s.Id == request.SupplierId && s.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException($"Supplier {request.SupplierId} not found.");

        var ingredient = await _dbContext.Set<Ingredient>()
            .FirstOrDefaultAsync(i => i.Id == request.IngredientId, cancellationToken)
            ?? throw new KeyNotFoundException($"Ingredient {request.IngredientId} not found.");

        ingredient.PreferredSupplierId = supplier.Id;
        ingredient.UpdatedAt             = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(ingredient, supplier.Name);
    }

    internal static IngredientDto MapToDto(Ingredient ingredient, string? supplierName) => new()
    {
        Id                    = ingredient.Id,
        Name                  = ingredient.Name,
        Description           = ingredient.Description,
        CostPerUnit           = ingredient.CostPerUnit,
        Unit                  = ingredient.Unit,
        IsActive              = ingredient.IsActive,
        CreatedAt             = ingredient.CreatedAt,
        PreferredSupplierId   = ingredient.PreferredSupplierId,
        PreferredSupplierName = supplierName
    };
}
