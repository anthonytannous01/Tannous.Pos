using MediatR;
using Tannous.Pos.Application.DTOs.Suppliers;

namespace Tannous.Pos.Application.Suppliers.Queries.GetSupplier;

public class GetSupplierQuery : IRequest<SupplierDto?>
{
    public Guid Id { get; set; }
}
