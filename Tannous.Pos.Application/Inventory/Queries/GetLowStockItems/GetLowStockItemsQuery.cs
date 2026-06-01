using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;

namespace Tannous.Pos.Application.Inventory.Queries.GetLowStockItems;

public class GetLowStockItemsQuery : IRequest<IEnumerable<InventoryItemDto>>
{
}
