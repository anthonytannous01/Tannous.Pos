using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Orders;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Orders.Queries.GetSplitBill;

public class GetSplitBillQueryHandler : IRequestHandler<GetSplitBillQuery, SplitBillDto?>
{
    private readonly DbContext _dbContext;

    public GetSplitBillQueryHandler(DbContext dbContext) => _dbContext = dbContext;

    public async Task<SplitBillDto?> Handle(GetSplitBillQuery request, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Set<Order>()
            .AsNoTracking()
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
            return null;

        if (order.Status != OrderStatus.Open)
        {
            throw new ValidationException(
                $"Split bill is only available for open orders. Current status: {order.Status}.");
        }

        return SplitBillCalculator.Build(order, request.Ways);
    }
}
