namespace Tannous.Pos.Application.DTOs.Catalog;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string  Name        { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? NameAr      { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}
