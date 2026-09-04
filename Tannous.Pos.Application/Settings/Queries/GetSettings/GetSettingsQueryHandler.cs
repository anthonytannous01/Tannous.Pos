using MediatR;
using Tannous.Pos.Application.DTOs.Settings;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Settings.Queries.GetSettings;

public class GetSettingsQueryHandler : IRequestHandler<GetSettingsQuery, SettingsDto>
{
    private readonly IBusinessSettingsRepository _settingsRepository;

    public GetSettingsQueryHandler(IBusinessSettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task<SettingsDto> Handle(GetSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _settingsRepository.GetAsync(cancellationToken);

        if (settings == null)
        {
            settings = new BusinessSettings
            {
                BusinessName = "Tannous POS",
                Currency = "USD",
                TaxRate = 0.0m,
                EnableInventoryTracking = true,
                EnableRecipeManagement = true
            };
            await _settingsRepository.CreateAsync(settings, cancellationToken);
        }

        return MapToDto(settings);
    }

    private static SettingsDto MapToDto(BusinessSettings settings) => new SettingsDto
    {
        Id                       = settings.Id,
        StoreName                = settings.BusinessName,
        Address                  = settings.Address,
        Phone                    = settings.Phone,
        Email                    = settings.Email,
        Website                  = settings.Website,
        TaxNumber                = settings.TaxNumber,
        TaxRate                  = settings.TaxRate,
        Currency                 = settings.Currency,
        TaxEnabled               = settings.TaxApplies,
        ReceiptHeader            = settings.ReceiptHeader,
        ReceiptFooter            = settings.ReceiptFooter,
        RequireCustomerInfo      = settings.RequireCustomerInfo,
        EnableInventoryTracking  = settings.EnableInventoryTracking,
        EnableRecipeManagement   = settings.EnableRecipeManagement,
        LoyaltyEnabled           = settings.LoyaltyEnabled,
        LoyaltyPointsPerDollar   = settings.LoyaltyPointsPerDollar,
        LoyaltyPointValueUsd     = settings.LoyaltyPointValueUsd,
        LoyaltyMinRedeemPoints   = settings.LoyaltyMinRedeemPoints,
        ExchangeRateLbpPerUsd    = settings.ExchangeRateLbpPerUsd,
        ShowLbpOnReceipt         = settings.ShowLbpOnReceipt,
        StampDutyEnabled         = settings.StampDutyEnabled,
        StampDutyAmountUsd       = settings.StampDutyAmountUsd,
        NotifyOnLoyaltyEarn      = settings.NotifyOnLoyaltyEarn,
        NotifyOnReservationConfirm = settings.NotifyOnReservationConfirm,
        CreatedAt                = settings.CreatedAt,
        UpdatedAt                = settings.UpdatedAt ?? DateTime.UtcNow
    };
}
