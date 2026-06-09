using MediatR;
using Tannous.Pos.Application.DTOs.Common;
using Tannous.Pos.Application.DTOs.Loyalty;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Loyalty.Queries.GetCustomersBySegment;

/// <summary>
/// Returns the loyalty customers assigned to a given behavioural segment, paginated.
/// Uses the same segmentation logic as <c>GetCustomerAnalyticsQuery</c>.
/// </summary>
public class GetCustomersBySegmentQuery : IRequest<PaginatedResponseDto<TopCustomerDto>>
{
    public CustomerSegment Segment { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
