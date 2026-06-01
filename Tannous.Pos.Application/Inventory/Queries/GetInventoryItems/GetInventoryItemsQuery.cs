using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;

namespace Tannous.Pos.Application.Inventory.Queries.GetInventoryItems;

public class GetInventoryItemsQuery : IRequest<IEnumerable<InventoryItemDto>>
{
}
