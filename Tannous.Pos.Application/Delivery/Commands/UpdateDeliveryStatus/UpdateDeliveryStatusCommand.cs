using MediatR;
using Tannous.Pos.Application.DTOs.Delivery;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Delivery.Commands.UpdateDeliveryStatus;

public class UpdateDeliveryStatusCommand : IRequest<DeliveryDto>
{
    public Guid           DeliveryId  { get; set; }
    public DeliveryStatus NewStatus   { get; set; }
    public string?        DriverName  { get; set; }
    public string?        DriverPhone { get; set; }
}
