using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Printing;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Application.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Services.Printing;

public class PrintingService : IPrintingService
{
    private readonly PosDbContext _context;

    public PrintingService(PosDbContext context)
    {
        _context = context;
    }

    public ReceiptTemplateDto GetReceiptTemplate()
    {
        var settings = _context.BusinessSettings.FirstOrDefault();

        return new ReceiptTemplateDto
        {
            LineWidth = 42,
            PrintLogo = false,
            StoreName = settings?.BusinessName ?? "Tannous POS",
            Address = settings?.Address,
            Phone = settings?.Phone,
            Currency = settings?.Currency ?? "USD",
            TaxEnabled = settings?.TaxRate > 0,
            Footer = settings?.ReceiptFooter,
            ExchangeRateLbpPerUsd = settings?.ExchangeRateLbpPerUsd ?? 0m,
            ShowLbpOnReceipt = settings?.ShowLbpOnReceipt ?? false,
            StampDutyEnabled = settings?.StampDutyEnabled ?? false,
            StampDutyAmountUsd = settings?.StampDutyAmountUsd ?? 2.00m
        };
    }

    public KitchenTemplateDto GetKitchenTemplate()
    {
        return new KitchenTemplateDto
        {
            LineWidth = 42,
            Header = "KITCHEN TICKET",
            PrintNotes = true
        };
    }

    public async Task<RenderResultDto> RenderReceiptAsync(Guid orderId, int lineWidth)
    {
        var order = await _context.Orders
            .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.MenuItem)
            .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.OrderLineAddOns)
                    .ThenInclude(ola => ola.AddOn)
            .Include(o => o.Customer)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new ArgumentException("Order not found");

        var settings = await _context.BusinessSettings.FirstOrDefaultAsync();
        var template = GetReceiptTemplate();
        template.LineWidth = lineWidth;

        var receipt = new System.Text.StringBuilder();

        // Header
        receipt.AppendLine(CenterText(template.StoreName, lineWidth));
        if (!string.IsNullOrEmpty(template.Address))
            receipt.AppendLine(CenterText(template.Address, lineWidth));
        if (!string.IsNullOrEmpty(template.Phone))
            receipt.AppendLine(CenterText(template.Phone, lineWidth));

        receipt.AppendLine(new string('-', lineWidth));

        // Order info
        receipt.AppendLine($"Date: {order.CreatedAt:MM/dd/yyyy}");
        receipt.AppendLine($"Time: {order.CreatedAt:HH:mm:ss}");
        if (!string.IsNullOrEmpty(order.ReceiptNumber))
            receipt.AppendLine($"Receipt: {order.ReceiptNumber}");
        else
            receipt.AppendLine("Status: Provisional");

        if (order.Customer != null)
            receipt.AppendLine($"Customer: {order.Customer.FirstName} {order.Customer.LastName}");

        receipt.AppendLine(new string('-', lineWidth));

        // Order lines
        foreach (var line in order.OrderLines)
        {
            var itemText = $"{line.Quantity} x {line.MenuItem.Name}";
            var lineTotal = line.TotalPrice.ToString("C");
            receipt.AppendLine(AlignText(itemText, lineTotal, lineWidth));

            // Add-ons
            foreach (var addon in line.OrderLineAddOns)
            {
                var addonText = $"  + {addon.AddOn.Name}";
                var addonTotal = addon.Price.ToString("C");
                receipt.AppendLine(AlignText(addonText, addonTotal, lineWidth));
            }
        }

        receipt.AppendLine(new string('-', lineWidth));

        // Totals — use stored order amounts (single source of truth, avoids recompute drift)
        var subtotal = order.SubTotal > 0 ? order.SubTotal : order.OrderLines.Sum(ol => ol.TotalPrice);
        receipt.AppendLine(AlignText("Subtotal", subtotal.ToString("N2"), lineWidth));

        // GOVERNANCE / RISK: Receipt tax uses order.TaxAmount (stored at finalize from BusinessSettings.TaxRate).
        // This is independent of OrderFinancialGovernance (legacy 10% fallback on create/finalize) — intentional
        // split; the stored TaxAmount is the authoritative value for receipt display. Do not recompute here.
        if (order.TaxAmount > 0)
        {
            var taxLabel = settings?.TaxRate > 0
                ? $"VAT ({settings.TaxRate}%)"
                : "VAT";
            receipt.AppendLine(AlignText(taxLabel, order.TaxAmount.ToString("N2"), lineWidth));
        }

        // Stamp duty (Lebanon 2025 Budget Law)
        if (order.StampDutyAmount > 0)
            receipt.AppendLine(AlignText("Stamp Duty", order.StampDutyAmount.ToString("N2"), lineWidth));

        receipt.AppendLine(AlignText($"TOTAL ({template.Currency})", order.TotalAmount.ToString("N2"), lineWidth));

        // LBP equivalent line
        if (template.ShowLbpOnReceipt && template.ExchangeRateLbpPerUsd > 0)
        {
            var totalLbp = order.TotalAmount * template.ExchangeRateLbpPerUsd;
            var lbpFormatted = totalLbp.ToString("N0");
            receipt.AppendLine(AlignText("TOTAL (LBP)", lbpFormatted, lineWidth));
            receipt.AppendLine(AlignText($"Rate: 1 USD =", $"{template.ExchangeRateLbpPerUsd:N0} LBP", lineWidth));
        }

        // Footer
        if (!string.IsNullOrEmpty(template.Footer))
        {
            receipt.AppendLine(new string('-', lineWidth));
            receipt.AppendLine(CenterText(template.Footer, lineWidth));
        }

        receipt.AppendLine(new string('-', lineWidth));
        receipt.AppendLine(CenterText("Thank you for your business!", lineWidth));

        return new RenderResultDto
        {
            PlainText = receipt.ToString(),
            SuggestedCodePage = "cp437"
        };
    }

    public async Task<RenderResultDto> RenderKitchenAsync(Guid orderId, int lineWidth)
    {
        var order = await _context.Orders
            .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.MenuItem)
            .Include(o => o.OrderLines)
                .ThenInclude(ol => ol.OrderLineAddOns)
                    .ThenInclude(ola => ola.AddOn)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new ArgumentException("Order not found");

        var template = GetKitchenTemplate();
        template.LineWidth = lineWidth;

        var kitchen = new System.Text.StringBuilder();

        // Header
        kitchen.AppendLine(CenterText(template.Header, lineWidth));
        kitchen.AppendLine(new string('=', lineWidth));

        // Order info
        kitchen.AppendLine($"Order: {order.OrderNumber}");
        kitchen.AppendLine($"Type: {order.OrderType}");
        kitchen.AppendLine($"Time: {order.CreatedAt:HH:mm:ss}");
        kitchen.AppendLine($"Date: {order.CreatedAt:MM/dd/yyyy}");

        if (!string.IsNullOrEmpty(order.Notes))
            kitchen.AppendLine($"Notes: {order.Notes}");

        kitchen.AppendLine(new string('-', lineWidth));

        // Order lines
        foreach (var line in order.OrderLines)
        {
            kitchen.AppendLine($"{line.Quantity} x {line.MenuItem.Name}");

            // Add-ons
            foreach (var addon in line.OrderLineAddOns)
            {
                kitchen.AppendLine($"  + {addon.AddOn.Name}");
            }

            // Line notes
            if (template.PrintNotes && !string.IsNullOrEmpty(line.Notes))
            {
                kitchen.AppendLine($"  Note: {line.Notes}");
            }

            kitchen.AppendLine();
        }

        kitchen.AppendLine(new string('=', lineWidth));
        kitchen.AppendLine(CenterText($"Order #{order.OrderNumber}", lineWidth));

        return new RenderResultDto
        {
            PlainText = kitchen.ToString(),
            SuggestedCodePage = "cp437"
        };
    }

    private static string CenterText(string text, int width)
    {
        if (text.Length >= width) return text;
        var padding = (width - text.Length) / 2;
        return text.PadLeft(padding + text.Length).PadRight(width);
    }

    private static string AlignText(string leftText, string rightText, int width)
    {
        var availableWidth = width - rightText.Length - 1; // -1 for space
        if (leftText.Length > availableWidth)
        {
            leftText = leftText.Substring(0, availableWidth - 3) + "...";
        }
        return leftText.PadRight(availableWidth) + " " + rightText;
    }
}
