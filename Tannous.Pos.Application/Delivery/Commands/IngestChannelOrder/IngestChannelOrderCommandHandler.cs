using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Delivery.Commands.IngestChannelOrder;

public class IngestChannelOrderCommandHandler
    : IRequestHandler<IngestChannelOrderCommand, IngestChannelOrderResult>
{
    private readonly DbContext _dbContext;
    private readonly IReceiptNumberService _receiptNumberService;
    private readonly IBranchRepository _branchRepository;
    private readonly ILogger<IngestChannelOrderCommandHandler> _logger;

    public IngestChannelOrderCommandHandler(
        DbContext dbContext,
        IReceiptNumberService receiptNumberService,
        IBranchRepository branchRepository,
        ILogger<IngestChannelOrderCommandHandler> logger)
    {
        _dbContext = dbContext;
        _receiptNumberService = receiptNumberService;
        _branchRepository = branchRepository;
        _logger = logger;
    }

    public async Task<IngestChannelOrderResult> Handle(
        IngestChannelOrderCommand request, CancellationToken cancellationToken)
    {
        var payload = request.Payload;

        // 1. Deduplication — idempotent on (Channel, ExternalOrderId).
        var existing = await _dbContext.Set<DeliveryInfo>()
            .FirstOrDefaultAsync(
                d => d.Channel == request.Channel && d.ExternalOrderId == payload.ExternalOrderId,
                cancellationToken);

        if (existing != null)
        {
            _logger.LogInformation(
                "Channel order already ingested (idempotent). Channel={Channel}, ExternalOrderId={ExternalOrderId}, OrderId={OrderId}",
                request.Channel, payload.ExternalOrderId, existing.OrderId);

            return new IngestChannelOrderResult
            {
                OrderId     = existing.OrderId,
                DeliveryId  = existing.Id,
                OrderNumber = await _dbContext.Set<Order>()
                    .Where(o => o.Id == existing.OrderId)
                    .Select(o => o.OrderNumber)
                    .FirstOrDefaultAsync(cancellationToken) ?? string.Empty,
                IsDuplicate = true
            };
        }

        // 2. Resolve branch.
        var branchId = request.BranchId
            ?? (await _branchRepository.GetDefaultAsync(cancellationToken))?.Id;

        // External channel lines have no matched catalog item. They are attached to a stable
        // placeholder MenuItem so the required FK is satisfied and KDS/receipts render safely;
        // the real platform item name is carried in each line's Notes for staff.
        var placeholderMenuItemId = await EnsurePlaceholderMenuItemAsync(cancellationToken);

        // 3. Create the order.
        var orderNumber = await _receiptNumberService.GenerateOrderNumberAsync();

        var order = new Order
        {
            OrderNumber   = orderNumber,
            OrderType     = OrderType.Delivery,
            Status        = OrderStatus.Pending,
            CustomerName  = payload.CustomerName,
            CustomerPhone = payload.CustomerPhone,
            Notes         = payload.Notes,
            BranchId      = branchId,
            OrderDate     = DateTime.UtcNow
        };

        decimal subTotal = 0m;
        foreach (var line in payload.Lines)
        {
            var quantity = line.Quantity <= 0 ? 1 : line.Quantity;
            var lineTotal = line.UnitPrice * quantity;
            subTotal += lineTotal;

            // MenuItem is the unmatched placeholder — staff prepares from the item name in Notes.
            order.OrderLines.Add(new OrderLine
            {
                MenuItemId = placeholderMenuItemId,
                Quantity   = quantity,
                UnitPrice  = line.UnitPrice,
                TotalPrice = lineTotal,
                Notes      = string.IsNullOrWhiteSpace(line.Notes)
                    ? line.ItemName
                    : $"{line.ItemName} — {line.Notes}"
            });
        }

        order.SubTotal    = subTotal;
        order.TotalAmount = subTotal + payload.DeliveryFee;

        _dbContext.Set<Order>().Add(order);

        // 4. Create the delivery info.
        var delivery = new DeliveryInfo
        {
            OrderId                = order.Id,
            Channel                = request.Channel,
            Status                 = DeliveryStatus.Pending,
            DeliveryAddress        = payload.DeliveryAddress,
            ApartmentDetails       = payload.ApartmentDetails,
            CustomerPhone          = payload.CustomerPhone,
            DeliveryFee            = payload.DeliveryFee,
            EstimatedMinutes       = payload.EstimatedMinutes,
            Notes                  = payload.Notes,
            BranchId               = branchId,
            ExternalOrderId        = payload.ExternalOrderId,
            ExternalOrderReference = $"{request.Channel} #{payload.ExternalOrderId}"
        };

        _dbContext.Set<DeliveryInfo>().Add(delivery);

        // 5. Persist order + delivery info atomically.
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Channel order ingested. Channel={Channel}, ExternalOrderId={ExternalOrderId}, OrderId={OrderId}, OrderNumber={OrderNumber}",
            request.Channel, payload.ExternalOrderId, order.Id, orderNumber);

        return new IngestChannelOrderResult
        {
            OrderId     = order.Id,
            DeliveryId  = delivery.Id,
            OrderNumber = orderNumber,
            IsDuplicate = false
        };
    }

    // Stable well-known identifiers for the unmatched external-delivery placeholder catalog entries.
    private static readonly Guid PlaceholderCategoryId = new("d0d0d0d0-0000-4000-8000-00000000c0de");
    private static readonly Guid PlaceholderMenuItemId = new("d0d0d0d0-0000-4000-8000-00000000d15e");

    /// <summary>
    /// Returns the id of the shared "External Delivery Item" placeholder MenuItem, creating it
    /// (and its placeholder Category) on first use. Inactive so it never appears in the POS catalog.
    /// </summary>
    private async Task<Guid> EnsurePlaceholderMenuItemAsync(CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Set<MenuItem>()
            .AnyAsync(m => m.Id == PlaceholderMenuItemId, cancellationToken);
        if (exists) return PlaceholderMenuItemId;

        var categoryExists = await _dbContext.Set<Category>()
            .AnyAsync(c => c.Id == PlaceholderCategoryId, cancellationToken);
        if (!categoryExists)
        {
            _dbContext.Set<Category>().Add(new Category
            {
                Id           = PlaceholderCategoryId,
                Name         = "External Delivery",
                NameAr       = "توصيل خارجي",
                IsActive     = false,
                DisplayOrder = 9999
            });
        }

        _dbContext.Set<MenuItem>().Add(new MenuItem
        {
            Id           = PlaceholderMenuItemId,
            Name         = "External Delivery Item",
            NameAr       = "صنف توصيل خارجي",
            CategoryId   = PlaceholderCategoryId,
            Price        = 0m,
            IsActive     = false,
            DisplayOrder = 9999
        });

        return PlaceholderMenuItemId;
    }
}
