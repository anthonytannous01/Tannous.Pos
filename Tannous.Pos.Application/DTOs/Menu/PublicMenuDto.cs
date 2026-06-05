namespace Tannous.Pos.Application.DTOs.Menu;

/// <summary>Full public menu returned to unauthenticated customers (QR scan).</summary>
public class PublicMenuDto
{
    public string BusinessName { get; set; } = string.Empty;
    public string? Address     { get; set; }
    public string? Phone       { get; set; }
    public string Currency     { get; set; } = "USD";
    /// <summary>LBP per USD rate; 0 if not configured.</summary>
    public decimal ExchangeRateLbpPerUsd { get; set; }
    public List<PublicMenuCategoryDto> Categories { get; set; } = new();
}

public class PublicMenuCategoryDto
{
    public Guid   Id           { get; set; }
    public string Name         { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int    DisplayOrder { get; set; }
    public List<PublicMenuItemDto> Items { get; set; } = new();
}

public class PublicMenuItemDto
{
    public Guid    Id          { get; set; }
    public string  Name        { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price       { get; set; }
    public string? ImageUrl    { get; set; }
    public int     DisplayOrder { get; set; }
}
