using MediatR;

namespace Tannous.Pos.Application.Purchasing.Commands.UnassignIngredientFromSupplier;

public class UnassignIngredientFromSupplierCommand : IRequest<bool>
{
    public Guid SupplierId   { get; set; }
    public Guid IngredientId { get; set; }
}
