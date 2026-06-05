using MediatR;
using Tannous.Pos.Application.DTOs.Feedback;

namespace Tannous.Pos.Application.Feedback.Queries.GetFeedbackSummary;

public class GetFeedbackSummaryQuery : IRequest<FeedbackSummaryDto>
{
    public Guid?     BranchId  { get; set; }
    public DateTime? From      { get; set; }
    public DateTime? To        { get; set; }
    /// <summary>Number of recent submissions to include. Default 20.</summary>
    public int       RecentMax { get; set; } = 20;
}
