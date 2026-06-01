using MediatR;
using Tannous.Pos.Application.DTOs.Suppliers;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Suppliers.Queries.GetSuppliers;

public class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQuery, IEnumerable<SupplierDto>>
{
    private readonly ISupplierRepository _supplierRepository;

    public GetSuppliersQueryHandler(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<IEnumerable<SupplierDto>> Handle(
        GetSuppliersQuery query, CancellationToken cancellationToken)
    {
        var suppliers = await _supplierRepository.GetActiveSuppliersAsync();
        return suppliers.Select(MapToDto).ToList();
    }

    private static SupplierDto MapToDto(Supplier s) => new()
    {
        Id            = s.Id,
        Name          = s.Name,
        ContactPerson = s.ContactPerson,
        Email         = s.Email,
        Phone         = s.Phone,
        Address       = s.Address,
        IsActive      = s.IsActive,
        CreatedAt     = s.CreatedAt
    };
}
