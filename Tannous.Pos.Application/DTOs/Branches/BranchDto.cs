namespace Tannous.Pos.Application.DTOs.Branches;

public class BranchDto
{
    public Guid   Id           { get; set; }
    public string Name         { get; set; } = string.Empty;
    public string? Address     { get; set; }
    public string? Phone       { get; set; }
    public bool   IsActive     { get; set; }
    public bool   IsDefault    { get; set; }
    public int    DisplayOrder { get; set; }
    public DateTime CreatedAt  { get; set; }
}
