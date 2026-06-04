using MediatR;
using Tannous.Pos.Application.DTOs.Loyalty;

namespace Tannous.Pos.Application.Loyalty.Queries.GetLoyaltyAccount;

public class GetLoyaltyAccountQuery : IRequest<LoyaltyAccountDto?>
{
    public Guid CustomerId { get; set; }
}
