using Tannous.Pos.Application.DTOs.Settings;
using Tannous.Pos.Application.DTOs.Catalog;
using Tannous.Pos.Application.DTOs.Inventory;
using Tannous.Pos.Application.DTOs.Customers;

namespace Tannous.Pos.Application.DTOs.Sync;

/// <summary>
/// Server incremental pull payload (upserts + deletes + cursor + pagination).
/// Wire shape is aligned with the Android <c>PullWorker</c> as of Step 64.
/// </summary>
public class PullResponseDto
{
    public string Cursor { get; set; } = string.Empty;
    public string? NextToken { get; set; }
    public bool HasMore { get; set; }
    public UpsertsDto Upserts { get; set; } = new();
    public DeletesDto Deletes { get; set; } = new();
}

public class UpsertsDto
{
    public List<SettingsDto>? Settings { get; set; }
    public List<CategoryDto>? Categories { get; set; }
    public List<MenuItemDto>? Items { get; set; }
    public List<AddOnDto>? AddOns { get; set; }
    public List<IngredientDto>? Ingredients { get; set; }
    public List<RecipeDto>? Recipes { get; set; }
    public List<CustomerDto>? Customers { get; set; }
}

public class DeletesDto
{
    public List<Guid>? Items { get; set; }
    public List<Guid>? Customers { get; set; }
}
