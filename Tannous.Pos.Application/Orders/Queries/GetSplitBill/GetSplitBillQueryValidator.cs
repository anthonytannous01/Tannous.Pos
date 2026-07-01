using FluentValidation;

namespace Tannous.Pos.Application.Orders.Queries.GetSplitBill;

public class GetSplitBillQueryValidator : AbstractValidator<GetSplitBillQuery>
{
    public GetSplitBillQueryValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Ways).InclusiveBetween(2, 20);
    }
}
