using MediatR;
using Tannous.Pos.Application.DTOs.Suppliers;

namespace Tannous.Pos.Application.Suppliers.Commands.CreateSupplier;

public class CreateSupplierCommand : IRequest<SupplierDto>
{
    public CreateSupplierDto Supplier { get; set; } = new();
}
