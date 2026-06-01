using MediatR;
using Tannous.Pos.Application.DTOs.Suppliers;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Suppliers.Queries.GetSupplier;

public class GetSupplierQueryHandler : IRequestHandler<GetSupplierQuery, SupplierDto?>
{
    private readonly ISupplierRepository _supplierRepository;

    public GetSupplierQueryHandler(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<SupplierDto?> Handle(
        GetSupplierQuery query, CancellationToken cancellationToken)
    {
        var supplier = await _supplierRepository.GetByIdAsync(query.Id);
        if (supplier == null) return null;
        return MapToDto(supplier);
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
