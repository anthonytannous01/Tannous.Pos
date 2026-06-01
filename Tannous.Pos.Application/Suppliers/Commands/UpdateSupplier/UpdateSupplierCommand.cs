using MediatR;
using Tannous.Pos.Application.DTOs.Suppliers;

namespace Tannous.Pos.Application.Suppliers.Commands.UpdateSupplier;

public class UpdateSupplierCommand : IRequest<SupplierDto>
{
    public Guid Id { get; set; }
    public UpdateSupplierDto Supplier { get; set; } = new();
}
