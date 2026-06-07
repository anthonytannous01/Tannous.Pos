using MediatR;
using Tannous.Pos.Application.DTOs.Delivery;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Delivery.Queries.GetDeliveryQueue;

public class GetDeliveryQueueQuery : IRequest<IEnumerable<DeliveryDto>>
{
    public Guid?           BranchId { get; set; }
    public DeliveryStatus? Status   { get; set; }
    public DateTime?       From     { get; set; }
    public DateTime?       To       { get; set; }
}
