using MediatR;
using Tannous.Pos.Application.DTOs.Loyalty;

namespace Tannous.Pos.Application.Loyalty.Commands.RedeemPoints;

public class RedeemPointsCommand : IRequest<LoyaltyAccountDto>
{
    public Guid CustomerId { get; set; }
    public int Points { get; set; }
    public Guid? OrderId { get; set; }
}
