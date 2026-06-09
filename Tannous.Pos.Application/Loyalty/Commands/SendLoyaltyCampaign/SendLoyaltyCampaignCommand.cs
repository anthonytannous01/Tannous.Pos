using MediatR;
using Tannous.Pos.Application.DTOs.Loyalty;
using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.Loyalty.Commands.SendLoyaltyCampaign;

/// <summary>
/// Resolves the customers in a target segment and dispatches an operator-authored
/// WhatsApp message to each, recording the campaign and its delivery outcome.
/// </summary>
public class SendLoyaltyCampaignCommand : IRequest<LoyaltyCampaignDto>
{
    public string Name { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public CustomerSegment TargetSegment { get; set; }
    public Guid CreatedByUserId { get; set; }
}
