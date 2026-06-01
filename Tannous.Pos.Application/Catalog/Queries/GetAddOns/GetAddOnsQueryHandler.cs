using MediatR;
using Tannous.Pos.Application.DTOs.Catalog;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Catalog.Queries.GetAddOns;

public class GetAddOnsQueryHandler : IRequestHandler<GetAddOnsQuery, IEnumerable<AddOnDto>>
{
    private readonly IAddOnRepository _addOnRepository;

    public GetAddOnsQueryHandler(IAddOnRepository addOnRepository)
    {
        _addOnRepository = addOnRepository;
    }

    public async Task<IEnumerable<AddOnDto>> Handle(
        GetAddOnsQuery query, CancellationToken cancellationToken)
    {
        var addOns = await _addOnRepository.GetActiveAddOnsAsync();
        return addOns.Select(MapToDto).ToList();
    }

    private static AddOnDto MapToDto(AddOn a) => new()
    {
        Id          = a.Id,
        Name        = a.Name,
        Description = a.Description,
        Price       = a.Price,
        IsActive    = a.IsActive,
        CreatedAt   = a.CreatedAt
    };
}
