using MediatR;
using Tannous.Pos.Application.DTOs.Loyalty;

namespace Tannous.Pos.Application.Loyalty.Commands.EarnPoints;

/// <summary>
/// Credits loyalty points to a customer's account.
/// Called automatically by FinalizeOrderCommandHandler when loyalty is enabled and order has a customer.
/// Can also be called manually by staff.
/// </summary>
public class EarnPointsCommand : IRequest<LoyaltyAccountDto>
{
    public Guid CustomerId { get; set; }
    public int Points { get; set; }
    public Guid? OrderId { get; set; }
    public string? Notes { get; set; }
}
