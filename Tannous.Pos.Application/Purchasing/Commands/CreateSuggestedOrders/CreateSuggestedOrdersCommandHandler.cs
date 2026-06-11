using MediatR;
using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Application.DTOs.Purchasing;
using Tannous.Pos.Application.Purchasing.Queries.GetSupplierIntelligence;
using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Application.Purchasing.Commands.CreateSuggestedOrders;

public class CreateSuggestedOrdersCommandHandler
    : IRequestHandler<CreateSuggestedOrdersCommand, CreateSuggestedOrdersResult>
{
    private readonly DbContext _dbContext;
    private readonly IMediator _mediator;

    public CreateSuggestedOrdersCommandHandler(DbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator  = mediator;
    }

    public async Task<CreateSuggestedOrdersResult> Handle(
        CreateSuggestedOrdersCommand request, CancellationToken cancellationToken)
    {
        var intelligence = await _mediator.Send(new GetSupplierIntelligenceQuery
        {
            ForecastDays = request.ForecastDays,
            BranchId     = request.BranchId
        }, cancellationToken);

        var suggestions = intelligence.OrderSuggestions
            .Where(s => s.SupplierId.HasValue)
            .Where(s => s.Lines.Any(l => l.SuggestedQty > 0))
            .ToList();

        if (request.SupplierIds is { Count: > 0 })
            suggestions = suggestions.Where(s => request.SupplierIds.Contains(s.SupplierId!.Value)).ToList();

        var skippedUnassigned = intelligence.OrderSuggestions.Any(s => !s.SupplierId.HasValue
            && s.Lines.Any(l => l.SuggestedQty > 0));

        if (suggestions.Count == 0)
        {
            return new CreateSuggestedOrdersResult
            {
                OrdersCreated = 0,
                SkippedReason = skippedUnassigned
                    ? "Some suggestions have no preferred supplier assigned."
                    : null
            };
        }

        var result = new CreateSuggestedOrdersResult();

        foreach (var suggestion in suggestions)
        {
            var lines = suggestion.Lines.Where(l => l.SuggestedQty > 0).ToList();
            if (lines.Count == 0)
                continue;

            var purchaseOrder = new PurchaseOrder
            {
                OrderNumber = GenerateOrderNumber(),
                SupplierId  = suggestion.SupplierId!.Value,
                Status      = "Pending",
                OrderDate   = DateTime.UtcNow,
                BranchId    = request.BranchId,
                Notes       = $"Auto-generated from {request.ForecastDays}-day demand forecast — {DateTime.UtcNow:yyyy-MM-dd}"
            };

            foreach (var line in lines)
            {
                purchaseOrder.Lines.Add(new PurchaseOrderLine
                {
                    IngredientId = line.IngredientId,
                    Quantity     = line.SuggestedQty,
                    UnitCost     = line.UnitCost,
                    TotalCost    = line.EstimatedCost,
                    Unit         = line.Unit
                });
            }

            purchaseOrder.SubTotal    = purchaseOrder.Lines.Sum(l => l.TotalCost);
            purchaseOrder.TaxAmount   = 0;
            purchaseOrder.TotalAmount = purchaseOrder.SubTotal;

            _dbContext.Set<PurchaseOrder>().Add(purchaseOrder);
            result.OrderIds.Add(purchaseOrder.Id);
            result.OrderNumbers.Add(purchaseOrder.OrderNumber);
            result.OrdersCreated++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (skippedUnassigned)
            result.SkippedReason = "Unassigned ingredients were skipped — assign a preferred supplier first.";

        return result;
    }

    private static string GenerateOrderNumber() =>
        $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
}
