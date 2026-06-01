namespace Tannous.Pos.Application.DTOs.Admin;

public class PurgeSoftDeletedResultDto
{
    public string Message { get; set; } = string.Empty;
    public int CustomersPurged { get; set; }
    public int MenuItemsPurged { get; set; }
    public int AddOnsPurged { get; set; }
    public int TotalPurged { get; set; }
    public DateTime CutoffDate { get; set; }
}
