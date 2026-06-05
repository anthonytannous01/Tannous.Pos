using MediatR;
using Tannous.Pos.Application.DTOs.Feedback;

namespace Tannous.Pos.Application.Feedback.Commands.SubmitFeedback;

/// <summary>Public (unauthenticated) feedback submission — called from receipt screen or QR menu.</summary>
public class SubmitFeedbackCommand : IRequest<FeedbackDto>
{
    public int     Rating       { get; set; }
    public string? Comment      { get; set; }
    public int     Category     { get; set; } = 0; // FeedbackCategory.General
    public Guid?   OrderId      { get; set; }
    public string? OrderNumber  { get; set; }
    public string? CustomerName { get; set; }
    public Guid?   BranchId     { get; set; }
}
