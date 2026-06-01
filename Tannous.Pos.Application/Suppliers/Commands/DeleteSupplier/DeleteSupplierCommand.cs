using MediatR;

namespace Tannous.Pos.Application.Suppliers.Commands.DeleteSupplier;

public class DeleteSupplierCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public bool Force { get; set; } = false;
}
