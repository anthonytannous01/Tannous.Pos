using MediatR;
using Tannous.Pos.Application.DTOs.Menu;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Menu.Queries.GetPublicMenu;

public class GetPublicMenuQueryHandler : IRequestHandler<GetPublicMenuQuery, PublicMenuDto>
{
    private readonly ICategoryRepository        _categoryRepository;
    private readonly IMenuItemRepository        _menuItemRepository;
    private readonly IBusinessSettingsRepository _settingsRepository;

    public GetPublicMenuQueryHandler(
        ICategoryRepository        categoryRepository,
        IMenuItemRepository        menuItemRepository,
        IBusinessSettingsRepository settingsRepository)
    {
        _categoryRepository = categoryRepository;
        _menuItemRepository  = menuItemRepository;
        _settingsRepository  = settingsRepository;
    }

    public async Task<PublicMenuDto> Handle(
        GetPublicMenuQuery request, CancellationToken cancellationToken)
    {
        var settings   = await _settingsRepository.GetAsync(cancellationToken);
        var categories = await _categoryRepository.GetActiveCategoriesAsync();
        var items      = await _menuItemRepository.GetActiveMenuItemsAsync();

        var itemsByCategory = items
            .GroupBy(i => i.CategoryId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var categoryDtos = categories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new PublicMenuCategoryDto
            {
                Id           = c.Id,
                Name         = c.Name,
                NameAr       = c.NameAr,
                Description  = c.Description,
                DisplayOrder = c.DisplayOrder,
                Items        = itemsByCategory.TryGetValue(c.Id, out var catItems)
                    ? catItems
                        .Where(i => i.IsActive)
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => new PublicMenuItemDto
                        {
                            Id            = i.Id,
                            Name          = i.Name,
                            NameAr        = i.NameAr,
                            Description   = i.Description,
                            DescriptionAr = i.DescriptionAr,
                            Price         = i.Price,
                            ImageUrl      = i.ImageUrl,
                            DisplayOrder  = i.DisplayOrder
                        }).ToList()
                    : new List<PublicMenuItemDto>()
            })
            .Where(c => c.Items.Count > 0) // skip empty categories
            .ToList();

        return new PublicMenuDto
        {
            BusinessName          = settings?.BusinessName ?? "Our Menu",
            BusinessNameAr        = settings?.BusinessNameAr,
            Address               = settings?.Address,
            Phone                 = settings?.Phone,
            Currency              = settings?.Currency ?? "USD",
            ExchangeRateLbpPerUsd = settings?.ExchangeRateLbpPerUsd ?? 0m,
            Categories            = categoryDtos
        };
    }
}
