using MediatR;
using Tannous.Pos.Application.DTOs.Sync;

namespace Tannous.Pos.Application.Inventory.Commands.AdjustInventory;

public class AdjustInventoryCommand : IRequest<OpResultDto>
{
    public string  OpId         { get; set; } = string.Empty;
    public Guid    IngredientId { get; set; }
    public decimal Quantity     { get; set; }
    public string  Reason       { get; set; } = string.Empty;
}
