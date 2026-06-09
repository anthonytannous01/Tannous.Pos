namespace Tannous.Pos.Domain.Enums;

/// <summary>
/// Lifecycle of a loyalty campaign dispatch.
/// </summary>
public enum CampaignStatus
{
    Pending   = 0,
    Sending   = 1,
    Completed = 2,
    Failed    = 3
}
