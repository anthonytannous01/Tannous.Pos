using MediatR;
using Tannous.Pos.Application.DTOs.Feedback;
using Tannous.Pos.Application.Feedback.Commands.SubmitFeedback;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Feedback.Queries.GetFeedbackSummary;

public class GetFeedbackSummaryQueryHandler : IRequestHandler<GetFeedbackSummaryQuery, FeedbackSummaryDto>
{
    private readonly IFeedbackRepository _feedbackRepository;

    public GetFeedbackSummaryQueryHandler(IFeedbackRepository feedbackRepository)
        => _feedbackRepository = feedbackRepository;

    public async Task<FeedbackSummaryDto> Handle(
        GetFeedbackSummaryQuery request, CancellationToken cancellationToken)
    {
        var items = await _feedbackRepository.GetAsync(
            request.BranchId, request.From, request.To, cancellationToken);

        var list = items.ToList();

        var recent = list
            .OrderByDescending(f => f.CreatedAt)
            .Take(Math.Clamp(request.RecentMax, 1, 100))
            .Select(SubmitFeedbackCommandHandler.MapToDto)
            .ToList();

        return new FeedbackSummaryDto
        {
            TotalCount    = list.Count,
            AverageRating = list.Count > 0 ? Math.Round(list.Average(f => f.Rating), 1) : 0,
            FiveStars     = list.Count(f => f.Rating == 5),
            FourStars     = list.Count(f => f.Rating == 4),
            ThreeStars    = list.Count(f => f.Rating == 3),
            TwoStars      = list.Count(f => f.Rating == 2),
            OneStar       = list.Count(f => f.Rating == 1),
            Complaints    = list.Count(f => f.Category == FeedbackCategory.Complaint),
            Recent        = recent
        };
    }
}
