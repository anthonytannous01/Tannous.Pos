using MediatR;
using Tannous.Pos.Application.DTOs.Loyalty;

namespace Tannous.Pos.Application.Loyalty.Queries.GetCustomerAnalytics;

/// <summary>
/// Aggregates loyalty-customer behaviour into a single CRM analytics summary
/// (segment counts, average order value, average point balance, top customers).
/// </summary>
public class GetCustomerAnalyticsQuery : IRequest<CustomerAnalyticsDto>
{
}
