using MediatR;
using Tannous.Pos.Application.DTOs.Inventory;

namespace Tannous.Pos.Application.Inventory.Queries.GetInventoryItem;

public class GetInventoryItemQuery : IRequest<InventoryItemDto?>
{
    public Guid Id { get; set; }
}
