using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;

namespace Tannous.Pos.Application.Inventory.Queries.GetInventorySummary;

public class GetInventorySummaryQuery : IRequest<IEnumerable<InventorySummaryDto>>
{
}
