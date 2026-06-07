using MediatR;
using Tannous.Pos.Application.DTOs.Delivery;
using Tannous.Pos.Application.Delivery.Queries.GetDeliveryQueue;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Delivery.Commands.UpdateDeliveryStatus;

public class UpdateDeliveryStatusCommandHandler
    : IRequestHandler<UpdateDeliveryStatusCommand, DeliveryDto>
{
    private readonly IDeliveryRepository _repo;

    public UpdateDeliveryStatusCommandHandler(IDeliveryRepository repo) => _repo = repo;

    public async Task<DeliveryDto> Handle(
        UpdateDeliveryStatusCommand request, CancellationToken ct)
    {
        var delivery = await _repo.GetByIdAsync(request.DeliveryId, ct)
            ?? throw new InvalidOperationException($"Delivery {request.DeliveryId} not found.");

        delivery.Status    = request.NewStatus;
        delivery.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.DriverName))
            delivery.DriverName = request.DriverName.Trim();

        if (!string.IsNullOrWhiteSpace(request.DriverPhone))
            delivery.DriverPhone = request.DriverPhone.Trim();

        // Stamp timestamps
        switch (request.NewStatus)
        {
            case DeliveryStatus.Assigned:
                delivery.AssignedAt ??= DateTime.UtcNow;
                break;
            case DeliveryStatus.PickedUp:
            case DeliveryStatus.OnWay:
                delivery.PickedUpAt ??= DateTime.UtcNow;
                break;
            case DeliveryStatus.Delivered:
                delivery.DeliveredAt ??= DateTime.UtcNow;
                break;
        }

        await _repo.CommitAsync(ct);

        return GetDeliveryQueueQueryHandler.Map(delivery);
    }
}
