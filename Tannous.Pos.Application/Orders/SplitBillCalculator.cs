using Tannous.Pos.Application.DTOs.Orders;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Orders;

internal static class SplitBillCalculator
{
    public static SplitBillDto Build(Order order, int ways)
    {
        var orderTotal  = order.TotalAmount;
        var payments    = order.Payments.Where(p => p.IsSuccessful).OrderBy(p => p.PaymentDate).ToList();
        var alreadyPaid = payments.Sum(PaymentAmountUsd);
        var remaining   = Math.Max(0m, orderTotal - alreadyPaid);
        var paidCount   = payments.Count;

        var portions = CalculatePortions(orderTotal, ways);
        for (var i = 0; i < portions.Count; i++)
            portions[i].IsPaid = i < paidCount;

        var peopleRemaining = Math.Max(0, ways - paidCount);
        var nextUnpaid      = portions.FirstOrDefault(p => !p.IsPaid);
        var amountPerPerson = nextUnpaid?.Amount ?? 0m;

        return new SplitBillDto
        {
            OrderId         = order.Id,
            OrderTotal      = orderTotal,
            AlreadyPaid     = alreadyPaid,
            Remaining       = remaining,
            Ways            = ways,
            AmountPerPerson = amountPerPerson,
            PeopleRemaining = peopleRemaining,
            IsFullyPaid     = remaining <= 0.005m,
            Portions        = portions
        };
    }

    public static List<SplitPortionDto> CalculatePortions(decimal total, int ways)
    {
        if (ways <= 0)
            return new List<SplitPortionDto>();

        if (total <= 0)
        {
            return Enumerable.Range(1, ways)
                .Select(i => new SplitPortionDto { PersonNumber = i, Amount = 0m, IsPaid = false })
                .ToList();
        }

        var portions  = new List<SplitPortionDto>(ways);
        var allocated = 0m;

        for (var i = 1; i <= ways; i++)
        {
            decimal amount;
            if (i == ways)
            {
                amount = decimal.Round(total - allocated, 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                amount = Math.Ceiling(total / ways * 100m) / 100m;
                allocated += amount;
            }

            portions.Add(new SplitPortionDto
            {
                PersonNumber = i,
                Amount       = amount,
                IsPaid       = false
            });
        }

        return portions;
    }

    private static decimal PaymentAmountUsd(Payment payment) =>
        payment.AmountInUsd > 0 ? payment.AmountInUsd : payment.Amount;
}
