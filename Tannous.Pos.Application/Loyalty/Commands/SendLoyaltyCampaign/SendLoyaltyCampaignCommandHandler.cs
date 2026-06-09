using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.DTOs.Loyalty;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Loyalty.Commands.SendLoyaltyCampaign;

public class SendLoyaltyCampaignCommandHandler
    : IRequestHandler<SendLoyaltyCampaignCommand, LoyaltyCampaignDto>
{
    private readonly DbContext _dbContext;
    private readonly INotificationService _notificationService;
    private readonly IBusinessSettingsRepository _businessSettingsRepository;
    private readonly ILogger<SendLoyaltyCampaignCommandHandler> _logger;

    public SendLoyaltyCampaignCommandHandler(
        DbContext dbContext,
        INotificationService notificationService,
        IBusinessSettingsRepository businessSettingsRepository,
        ILogger<SendLoyaltyCampaignCommandHandler> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _businessSettingsRepository = businessSettingsRepository;
        _logger = logger;
    }

    public async Task<LoyaltyCampaignDto> Handle(
        SendLoyaltyCampaignCommand request, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        // Resolve recipients: active loyalty accounts in the target segment with a phone number.
        var snapshots = await _dbContext.Set<LoyaltyAccount>()
            .Where(la => la.IsActive
                && la.Customer.IsActive
                && !la.Customer.IsDeleted)
            .Select(la => new CustomerSegmentSnapshot
            {
                CustomerId           = la.CustomerId,
                Name                 = la.Customer.FirstName + " " + la.Customer.LastName,
                Phone                = la.Customer.Phone,
                LifetimePointsEarned = la.LifetimePointsEarned,
                PointBalance         = la.PointBalance,
                TotalOrders          = la.Customer.TotalOrders,
                LastVisitDate        = la.Customer.LastVisitDate
            })
            .ToListAsync(cancellationToken);

        var vipThreshold = CustomerSegmentEvaluator.ComputeVipThreshold(snapshots);

        var recipients = snapshots
            .Where(s => !string.IsNullOrWhiteSpace(s.Phone))
            .Where(s => CustomerSegmentEvaluator.DetermineSegment(s, vipThreshold, utcNow) == request.TargetSegment)
            .ToList();

        var businessSettings = await _businessSettingsRepository.GetAsync(cancellationToken);
        var businessName = businessSettings?.BusinessName is { Length: > 0 } name ? name : "Tannous POS";

        var campaign = new LoyaltyCampaign
        {
            Name            = request.Name,
            Message         = request.Message,
            TargetSegment   = request.TargetSegment,
            RecipientCount  = recipients.Count,
            SentCount       = 0,
            Status          = CampaignStatus.Sending,
            CreatedByUserId = request.CreatedByUserId
        };

        _dbContext.Set<LoyaltyCampaign>().Add(campaign);
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var sent = 0;
            foreach (var recipient in recipients)
            {
                var ok = await _notificationService.SendLoyaltyNotificationAsync(
                    toPhone:      recipient.Phone!,
                    message:      request.Message,
                    businessName: businessName,
                    cancellationToken: cancellationToken);

                if (ok) sent++;
            }

            campaign.SentCount = sent;
            campaign.Status    = CampaignStatus.Completed;
            campaign.SentAt    = DateTime.UtcNow;
            campaign.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Loyalty campaign dispatched. CampaignId={CampaignId}, Segment={Segment}, Recipients={Recipients}, Sent={Sent}",
                campaign.Id, request.TargetSegment, recipients.Count, sent);
        }
        catch (Exception ex)
        {
            campaign.Status       = CampaignStatus.Failed;
            campaign.ErrorMessage = ex.Message;
            campaign.UpdatedAt    = DateTime.UtcNow;

            _logger.LogError(ex,
                "Loyalty campaign dispatch failed. CampaignId={CampaignId}, Segment={Segment}",
                campaign.Id, request.TargetSegment);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LoyaltyCampaignDto
        {
            Id             = campaign.Id,
            Name           = campaign.Name,
            Message        = campaign.Message,
            TargetSegment  = campaign.TargetSegment,
            RecipientCount = campaign.RecipientCount,
            SentCount      = campaign.SentCount,
            Status         = campaign.Status,
            CreatedAt      = campaign.CreatedAt,
            SentAt         = campaign.SentAt,
            ErrorMessage   = campaign.ErrorMessage
        };
    }
}
