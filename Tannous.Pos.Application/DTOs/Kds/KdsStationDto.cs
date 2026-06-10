namespace Tannous.Pos.Application.DTOs.Kds;

public class KdsStationDto
{
    public Guid    Id            { get; set; }
    public string  Name          { get; set; } = string.Empty;
    public string? NameAr        { get; set; }
    public string? Color         { get; set; }
    public int     DisplayOrder  { get; set; }
    public bool    IsActive      { get; set; }
    public Guid?   BranchId      { get; set; }
    /// <summary>Count of active menu items assigned to this station.</summary>
    public int     MenuItemCount { get; set; }
}
