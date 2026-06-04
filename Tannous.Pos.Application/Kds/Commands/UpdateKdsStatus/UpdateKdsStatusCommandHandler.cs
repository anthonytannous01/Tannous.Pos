using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.DTOs.Kds;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Kds.Commands.UpdateKdsStatus;

public class UpdateKdsStatusCommandHandler : IRequestHandler<UpdateKdsStatusCommand, KdsTicketDto>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<UpdateKdsStatusCommandHandler> _logger;

    public UpdateKdsStatusCommandHandler(DbContext dbContext, ILogger<UpdateKdsStatusCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<KdsTicketDto> Handle(UpdateKdsStatusCommand request, CancellationToken cancellationToken)
    {
        var line = await _dbContext.Set<OrderLine>()
            .Include(ol => ol.Order)
            .Include(ol => ol.MenuItem)
            .Include(ol => ol.OrderLineAddOns)
                .ThenInclude(ola => ola.AddOn)
            .FirstOrDefaultAsync(ol => ol.Id == request.OrderLineId, cancellationToken);

        if (line == null)
            throw new InvalidOperationException($"OrderLine {request.OrderLineId} not found");

        // Guard: cannot update a cancelled line
        if (line.KdsStatus == KdsStatus.Cancelled)
            throw new InvalidOperationException(
                $"OrderLine {request.OrderLineId} is cancelled and cannot be updated");

        var previous = line.KdsStatus;
        line.KdsStatus = request.NewStatus;
        line.UpdatedAt = DateTime.UtcNow;

        if (request.NewStatus == KdsStatus.InProgress && line.KdsAcknowledgedAt == null)
            line.KdsAcknowledgedAt = DateTime.UtcNow;

        if (request.NewStatus == KdsStatus.Done)
            line.KdsDoneAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "KDS status updated. OrderLineId={OrderLineId}, OrderNumber={OrderNumber}, Item={Item}, {From} → {To}",
            line.Id, line.Order.OrderNumber, line.MenuItem.Name, previous, request.NewStatus);

        return new KdsTicketDto
        {
            OrderLineId       = line.Id,
            OrderId           = line.OrderId,
            OrderNumber       = line.Order.OrderNumber,
            OrderType         = line.Order.OrderType,
            MenuItemName      = line.MenuItem.Name,
            Quantity          = line.Quantity,
            Notes             = line.Notes,
            AddOns            = line.OrderLineAddOns.Select(a => a.AddOn.Name).ToList(),
            KdsStatus         = line.KdsStatus,
            OrderCreatedAt    = line.Order.CreatedAt,
            KdsAcknowledgedAt = line.KdsAcknowledgedAt,
            KdsDoneAt         = line.KdsDoneAt
        };
    }
}
