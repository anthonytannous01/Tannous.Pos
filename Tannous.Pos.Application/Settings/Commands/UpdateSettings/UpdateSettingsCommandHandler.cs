using MediatR;
using Tannous.Pos.Application.DTOs.Settings;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Settings.Commands.UpdateSettings;

public class UpdateSettingsCommandHandler : IRequestHandler<UpdateSettingsCommand, SettingsDto>
{
    private readonly IBusinessSettingsRepository _settingsRepository;

    public UpdateSettingsCommandHandler(IBusinessSettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task<SettingsDto> Handle(UpdateSettingsCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Settings;
        var settings = await _settingsRepository.GetAsync(cancellationToken);
        var isNew = settings == null;

        if (isNew)
            settings = new BusinessSettings();

        settings!.BusinessName          = dto.StoreName;
        settings.Address               = dto.Address;
        settings.Phone                 = dto.Phone;
        settings.Email                 = dto.Email;
        settings.Website               = dto.Website;
        settings.TaxNumber             = dto.TaxNumber;
        settings.TaxRate               = dto.TaxRate;
        settings.Currency              = dto.Currency;
        settings.ReceiptHeader         = dto.ReceiptHeader;
        settings.ReceiptFooter         = dto.ReceiptFooter;
        settings.RequireCustomerInfo      = dto.RequireCustomerInfo;
        settings.EnableInventoryTracking  = dto.EnableInventoryTracking;
        settings.EnableRecipeManagement   = dto.EnableRecipeManagement;
        settings.ExchangeRateLbpPerUsd    = dto.ExchangeRateLbpPerUsd;
        settings.ShowLbpOnReceipt         = dto.ShowLbpOnReceipt;
        settings.StampDutyEnabled         = dto.StampDutyEnabled;
        settings.StampDutyAmountUsd       = dto.StampDutyAmountUsd > 0 ? dto.StampDutyAmountUsd : 2.00m;
        settings.UpdatedAt                = DateTime.UtcNow;

        if (isNew)
            await _settingsRepository.CreateAsync(settings, cancellationToken);
        else
            await _settingsRepository.UpdateAsync(cancellationToken);

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
        TaxEnabled               = settings.TaxRate > 0,
        ReceiptHeader            = settings.ReceiptHeader,
        ReceiptFooter            = settings.ReceiptFooter,
        RequireCustomerInfo      = settings.RequireCustomerInfo,
        EnableInventoryTracking  = settings.EnableInventoryTracking,
        EnableRecipeManagement   = settings.EnableRecipeManagement,
        ExchangeRateLbpPerUsd    = settings.ExchangeRateLbpPerUsd,
        ShowLbpOnReceipt         = settings.ShowLbpOnReceipt,
        StampDutyEnabled         = settings.StampDutyEnabled,
        StampDutyAmountUsd       = settings.StampDutyAmountUsd,
        CreatedAt                = settings.CreatedAt,
        UpdatedAt                = settings.UpdatedAt ?? DateTime.UtcNow
    };
}
