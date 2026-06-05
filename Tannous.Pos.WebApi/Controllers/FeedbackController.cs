using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tannous.Pos.Application.DTOs.Feedback;
using Tannous.Pos.Application.Feedback.Commands.SubmitFeedback;
using Tannous.Pos.Application.Feedback.Queries.GetFeedbackSummary;
using Tannous.Pos.WebApi.Constants;

namespace Tannous.Pos.WebApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/feedback")]
[ApiVersion("1.0")]
public class FeedbackController : ControllerBase
{
    private readonly IMediator _mediator;

    public FeedbackController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Submit customer feedback — public, no authentication required.
    /// Called from the receipt screen or QR menu page.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<FeedbackDto>> Submit(
        [FromBody] SubmitFeedbackCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetSummary), result);
    }

    /// <summary>
    /// Owner dashboard summary — average rating, star breakdown, recent submissions.
    /// Requires CanViewReports policy.
    /// </summary>
    [HttpGet("summary")]
    [Authorize(Policy = PolicyConstants.CanViewReports)]
    public async Task<ActionResult<FeedbackSummaryDto>> GetSummary(
        [FromQuery] Guid?     branchId  = null,
        [FromQuery] DateTime? from      = null,
        [FromQuery] DateTime? to        = null,
        [FromQuery] int       recentMax = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFeedbackSummaryQuery
        {
            BranchId  = branchId,
            From      = from,
            To        = to,
            RecentMax = recentMax
        }, ct);
        return Ok(result);
    }
}
