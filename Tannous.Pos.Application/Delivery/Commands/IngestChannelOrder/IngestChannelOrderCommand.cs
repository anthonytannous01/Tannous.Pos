using MediatR;
using Tannous.Pos.Application.DTOs.Delivery;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Delivery.Commands.IngestChannelOrder;

/// <summary>
/// Ingests a normalised order from an external delivery channel (Toters/Talabat/Wolt),
/// creating an Order + DeliveryInfo so it appears on the POS and KDS automatically.
/// Idempotent on (Channel, ExternalOrderId).
/// </summary>
public class IngestChannelOrderCommand : IRequest<IngestChannelOrderResult>
{
    public DeliveryChannel       Channel  { get; set; }
    public CreateChannelOrderDto Payload  { get; set; } = null!;
    public Guid?                 BranchId { get; set; }
}

public class IngestChannelOrderResult
{
    public Guid   OrderId     { get; set; }
    public Guid   DeliveryId  { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public bool   IsDuplicate { get; set; } // true if ExternalOrderId already exists for this channel
}
