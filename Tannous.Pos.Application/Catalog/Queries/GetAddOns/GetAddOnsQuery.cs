using MediatR;
using Tannous.Pos.Application.DTOs.Catalog;

namespace Tannous.Pos.Application.Catalog.Queries.GetAddOns;

public class GetAddOnsQuery : IRequest<IEnumerable<AddOnDto>>
{
}
