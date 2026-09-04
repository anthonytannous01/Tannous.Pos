using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Orders;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Orders.Commands.RecordSplitPayment;

public class RecordSplitPaymentCommandHandler : IRequestHandler<RecordSplitPaymentCommand, SplitBillDto>
{
    private readonly DbContext _dbContext;
    private readonly IBusinessSettingsRepository _businessSettingsRepository;

    public RecordSplitPaymentCommandHandler(
        DbContext dbContext,
        IBusinessSettingsRepository businessSettingsRepository)
    {
        _dbContext = dbContext;
        _businessSettingsRepository = businessSettingsRepository;
    }

    public async Task<SplitBillDto> Handle(RecordSplitPaymentCommand request, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Set<Order>()
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
            throw new ValidationException("Order not found.");

        if (!order.Status.IsUnsettled())
        {
            throw new ValidationException(
                $"Split payments can only be recorded on unsettled orders. Current status: {order.Status}.");
        }

        var alreadyPaid = order.Payments.Where(p => p.IsSuccessful).Sum(PaymentAmountUsd);
        var remaining   = order.TotalAmount - alreadyPaid;

        var settings     = await _businessSettingsRepository.GetAsync(cancellationToken);
        var exchangeRate = settings?.ExchangeRateLbpPerUsd ?? 0m;
        var method       = request.Method.Trim();
        var isLbp        = method.Contains("LBP", StringComparison.OrdinalIgnoreCase);
        var tenderedCurrency = isLbp ? "LBP" : "USD";

        var paymentUsd = isLbp && exchangeRate > 0
            ? decimal.Round(request.Amount / exchangeRate, 4, MidpointRounding.AwayFromZero)
            : request.Amount;

        if (paymentUsd > remaining + 0.005m)
        {
            throw new ValidationException(
                $"Payment amount exceeds remaining balance {remaining:N2}.");
        }

        decimal? exchangeRateUsed = isLbp && exchangeRate > 0 ? exchangeRate : null;

        var payment = new Payment
        {
            OrderId          = order.Id,
            Amount           = request.Amount,
            PaymentMethod    = NormalizeMethod(method),
            TransactionId    = request.Reference,
            PaymentDate      = DateTime.UtcNow,
            IsSuccessful     = true,
            TenderedCurrency = tenderedCurrency,
            ExchangeRateUsed = exchangeRateUsed,
            AmountInUsd      = paymentUsd
        };

        await _dbContext.Set<Payment>().AddAsync(payment, cancellationToken);
        order.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _dbContext.Entry(order).Collection(o => o.Payments).LoadAsync(cancellationToken);
        return SplitBillCalculator.Build(order, request.TotalWays);
    }

    private static decimal PaymentAmountUsd(Payment payment) =>
        payment.AmountInUsd > 0 ? payment.AmountInUsd : payment.Amount;

    private static string NormalizeMethod(string method)
    {
        if (method.Contains("LBP", StringComparison.OrdinalIgnoreCase))
            return "LBP Cash";
        if (method.Contains("Card", StringComparison.OrdinalIgnoreCase) ||
            method.Contains("Visa", StringComparison.OrdinalIgnoreCase))
            return "Card";
        return "Cash";
    }
}
