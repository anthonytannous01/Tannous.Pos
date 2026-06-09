using Tannous.Pos.Domain.Enums;

namespace Tannous.Pos.Application.DTOs.Loyalty;

public class LoyaltyCampaignDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public CustomerSegment TargetSegment { get; set; }
    public int RecipientCount { get; set; }
    public int SentCount { get; set; }
    public CampaignStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SendCampaignDto
{
    public string Name { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public CustomerSegment TargetSegment { get; set; }
}
