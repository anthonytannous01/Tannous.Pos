using MediatR;
using Tannous.Pos.Application.DTOs.Delivery;

namespace Tannous.Pos.Application.Delivery.Commands.CreateDeliveryInfo;

public class CreateDeliveryInfoCommand : IRequest<DeliveryDto>
{
    public Guid    OrderId          { get; set; }
    public string  DeliveryAddress  { get; set; } = string.Empty;
    public string? ApartmentDetails { get; set; }
    public string? CustomerPhone    { get; set; }
    public int     Channel          { get; set; } = 0;  // DeliveryChannel.Own
    public decimal DeliveryFee      { get; set; } = 0m;
    public int?    EstimatedMinutes { get; set; }
    public string? Notes            { get; set; }
    public Guid?   BranchId         { get; set; }
}
