using MediatR;
using Tannous.Pos.Application.DTOs.Purchasing;

namespace Tannous.Pos.Application.Purchasing.Commands.CreateSuggestedOrders;

public class CreateSuggestedOrdersCommand : IRequest<CreateSuggestedOrdersResult>
{
    public int         ForecastDays { get; set; } = 7;
    public Guid?       BranchId     { get; set; }
    public List<Guid>? SupplierIds  { get; set; }
}
