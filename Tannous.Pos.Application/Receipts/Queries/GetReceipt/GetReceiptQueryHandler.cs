using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Receipts;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Receipts.Queries.GetReceipt;

public class GetReceiptQueryHandler : IRequestHandler<GetReceiptQuery, ReceiptDto?>
{
    private readonly DbContext _dbContext;

    public GetReceiptQueryHandler(DbContext dbContext) => _dbContext = dbContext;

    public async Task<ReceiptDto?> Handle(GetReceiptQuery request, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Set<Order>()
            .AsNoTracking()
            .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.MenuItem)
            .Include(o => o.Payments)
            .Include(o => o.Table)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
            return null;

        var settings = await _dbContext.Set<BusinessSettings>()
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        var exchangeRate = settings?.ExchangeRateLbpPerUsd ?? 0m;
        var stampDutyEnabled = settings?.StampDutyEnabled ?? false;
        var totalUsd = order.TotalAmount;

        return new ReceiptDto
        {
            OrderId          = order.Id,
            OrderNumber      = order.OrderNumber,
            OrderType        = FormatOrderType(order.OrderType),
            PrintedAt        = DateTime.UtcNow,
            IsReprint        = order.Status == OrderStatus.Paid,
            BusinessName     = settings?.BusinessName ?? "Tannous POS",
            BusinessNameAr   = settings?.BusinessNameAr,
            BusinessPhone    = settings?.Phone,
            BusinessAddress  = settings?.Address,
            TaxId            = settings?.TaxNumber,
            CustomerName     = ResolveCustomerName(order),
            TableLabel       = order.Table?.Label ?? order.Table?.TableNumber,
            Lines            = order.OrderLines.Select(ol => new ReceiptLineDto
            {
                Name      = ol.MenuItem.Name,
                NameAr    = ol.MenuItem.NameAr,
                Qty       = (int)ol.Quantity,
                UnitPrice = ol.UnitPrice,
                LineTotal = ol.TotalPrice,
                Notes     = ol.Notes
            }).ToList(),
            SubTotal         = order.SubTotal,
            DiscountAmount   = order.DiscountAmount,
            TaxAmount        = order.TaxAmount,
            StampDuty        = order.StampDutyAmount,
            TotalUsd         = totalUsd,
            TotalLbp         = exchangeRate > 0 ? decimal.Round(totalUsd * exchangeRate, 0, MidpointRounding.AwayFromZero) : 0m,
            StampDutyEnabled = stampDutyEnabled,
            Payments         = order.Payments
                .Where(p => p.IsSuccessful)
                .Select(p => new ReceiptPaymentDto
                {
                    Method = FormatPaymentMethod(p),
                    Amount = p.AmountInUsd > 0 ? p.AmountInUsd : p.Amount
                })
                .ToList(),
            AmountTendered   = order.AmountTendered,
            ChangeDue        = order.ChangeDue,
            FooterMessage    = string.IsNullOrWhiteSpace(settings?.ReceiptFooter)
                ? "Thank you for visiting!"
                : settings!.ReceiptFooter!,
            FooterMessageAr  = "شكراً لزيارتكم!"
        };
    }

    private static string? ResolveCustomerName(Order order)
    {
        if (!string.IsNullOrWhiteSpace(order.CustomerName))
            return order.CustomerName;

        if (order.Customer == null)
            return null;

        var name = $"{order.Customer.FirstName} {order.Customer.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private static string FormatOrderType(OrderType type) => type switch
    {
        OrderType.DineIn    => "Dine-In",
        OrderType.Takeaway  => "Takeaway",
        OrderType.Delivery  => "Delivery",
        OrderType.Online    => "Online",
        _                   => type.ToString()
    };

    private static string FormatPaymentMethod(Payment payment)
    {
        if (string.Equals(payment.TenderedCurrency, "LBP", StringComparison.OrdinalIgnoreCase))
            return "LBP Cash";

        return payment.PaymentMethod.ToUpperInvariant() switch
        {
            "CASH" => "Cash",
            "CARD" => "Visa / Card",
            _      => payment.PaymentMethod
        };
    }
}
