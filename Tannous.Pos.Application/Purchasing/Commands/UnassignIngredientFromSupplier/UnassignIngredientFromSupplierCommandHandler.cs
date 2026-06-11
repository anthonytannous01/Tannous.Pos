using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Purchasing.Commands.UnassignIngredientFromSupplier;

public class UnassignIngredientFromSupplierCommandHandler
    : IRequestHandler<UnassignIngredientFromSupplierCommand, bool>
{
    private readonly DbContext _dbContext;

    public UnassignIngredientFromSupplierCommandHandler(DbContext dbContext) => _dbContext = dbContext;

    public async Task<bool> Handle(
        UnassignIngredientFromSupplierCommand request, CancellationToken cancellationToken)
    {
        var ingredient = await _dbContext.Set<Ingredient>()
            .FirstOrDefaultAsync(
                i => i.Id == request.IngredientId && i.PreferredSupplierId == request.SupplierId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Ingredient {request.IngredientId} is not assigned to supplier {request.SupplierId}.");

        ingredient.PreferredSupplierId = null;
        ingredient.UpdatedAt           = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
