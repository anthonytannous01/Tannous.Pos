using MediatR;
using Tannous.Pos.Application.DTOs.Delivery;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Delivery.Queries.GetDeliveryQueue;

public class GetDeliveryQueueQueryHandler
    : IRequestHandler<GetDeliveryQueueQuery, IEnumerable<DeliveryDto>>
{
    private readonly IDeliveryRepository _repo;

    public GetDeliveryQueueQueryHandler(IDeliveryRepository repo) => _repo = repo;

    public async Task<IEnumerable<DeliveryDto>> Handle(
        GetDeliveryQueueQuery request, CancellationToken ct)
    {
        var items = await _repo.GetQueueAsync(
            request.BranchId, request.Status, request.From, request.To, ct);
        return items.Select(Map);
    }

    internal static DeliveryDto Map(DeliveryInfo d) => new()
    {
        Id               = d.Id,
        OrderId          = d.OrderId,
        OrderNumber      = d.Order?.OrderNumber,
        OrderTotal       = d.Order?.TotalAmount ?? 0m,
        CustomerName     = d.Order?.CustomerName,
        DeliveryAddress  = d.DeliveryAddress,
        ApartmentDetails = d.ApartmentDetails,
        CustomerPhone    = d.CustomerPhone,
        Channel          = (int)d.Channel,
        ChannelName      = d.Channel.ToString(),
        Status           = (int)d.Status,
        StatusName       = d.Status.ToString(),
        DeliveryFee      = d.DeliveryFee,
        EstimatedMinutes = d.EstimatedMinutes,
        Notes            = d.Notes,
        DriverName       = d.DriverName,
        DriverPhone      = d.DriverPhone,
        AssignedAt       = d.AssignedAt,
        PickedUpAt       = d.PickedUpAt,
        DeliveredAt      = d.DeliveredAt,
        BranchId         = d.BranchId,
        ExternalOrderId        = d.ExternalOrderId,
        ExternalOrderReference = d.ExternalOrderReference,
        CreatedAt        = d.CreatedAt
    };
}
