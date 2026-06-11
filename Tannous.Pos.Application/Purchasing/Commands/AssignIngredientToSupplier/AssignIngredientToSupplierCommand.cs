using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;

namespace Tannous.Pos.Application.Purchasing.Commands.AssignIngredientToSupplier;

public class AssignIngredientToSupplierCommand : IRequest<IngredientDto>
{
    public Guid SupplierId   { get; set; }
    public Guid IngredientId { get; set; }
}
