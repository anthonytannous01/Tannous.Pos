using MediatR;
using Tannous.Pos.Application.DTOs.Purchasing;

namespace Tannous.Pos.Application.Purchasing.Queries.GetSupplierIntelligence;

public class GetSupplierIntelligenceQuery : IRequest<SupplierIntelligenceDto>
{
    public int   ForecastDays { get; set; } = 7;
    public Guid? BranchId     { get; set; }
}
