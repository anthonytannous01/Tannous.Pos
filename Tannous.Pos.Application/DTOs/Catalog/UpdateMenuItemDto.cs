namespace Tannous.Pos.Application.DTOs.Catalog;

public class UpdateMenuItemDto
{
    public string  Name          { get; set; } = string.Empty;
    public string? Description   { get; set; }
    public string? NameAr        { get; set; }
    public string? DescriptionAr { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool HasAddOns { get; set; } = false;
    public bool HasIngredients { get; set; } = false;
    public Guid CategoryId { get; set; }
}
