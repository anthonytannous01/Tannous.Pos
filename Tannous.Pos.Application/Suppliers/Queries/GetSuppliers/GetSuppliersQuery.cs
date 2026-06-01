using MediatR;
using Tannous.Pos.Application.DTOs.Suppliers;

namespace Tannous.Pos.Application.Suppliers.Queries.GetSuppliers;

public class GetSuppliersQuery : IRequest<IEnumerable<SupplierDto>>
{
}
