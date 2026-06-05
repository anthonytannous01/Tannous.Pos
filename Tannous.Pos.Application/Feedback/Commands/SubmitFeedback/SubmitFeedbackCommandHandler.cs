using MediatR;
using Tannous.Pos.Application.DTOs.Feedback;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Feedback.Commands.SubmitFeedback;

public class SubmitFeedbackCommandHandler : IRequestHandler<SubmitFeedbackCommand, FeedbackDto>
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IUnitOfWork         _unitOfWork;

    public SubmitFeedbackCommandHandler(
        IFeedbackRepository feedbackRepository,
        IUnitOfWork         unitOfWork)
    {
        _feedbackRepository = feedbackRepository;
        _unitOfWork         = unitOfWork;
    }

    public async Task<FeedbackDto> Handle(
        SubmitFeedbackCommand request, CancellationToken cancellationToken)
    {
        var feedback = new FeedbackSubmission
        {
            Rating       = Math.Clamp(request.Rating, 1, 5),
            Comment      = request.Comment?.Trim(),
            Category     = (FeedbackCategory)request.Category,
            OrderId      = request.OrderId,
            OrderNumber  = request.OrderNumber,
            CustomerName = request.CustomerName?.Trim(),
            BranchId     = request.BranchId
        };

        await _feedbackRepository.AddAsync(feedback, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(feedback);
    }

    internal static FeedbackDto MapToDto(FeedbackSubmission f) => new()
    {
        Id           = f.Id,
        Rating       = f.Rating,
        Comment      = f.Comment,
        Category     = (int)f.Category,
        CategoryName = f.Category.ToString(),
        OrderId      = f.OrderId,
        OrderNumber  = f.OrderNumber,
        CustomerName = f.CustomerName,
        BranchId     = f.BranchId,
        CreatedAt    = f.CreatedAt
    };
}
