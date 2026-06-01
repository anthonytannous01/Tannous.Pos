namespace Tannous.Pos.Application.DTOs.Admin;

public class AdminDatabaseStatsDto
{
    public int Orders { get; set; }
    public int Customers { get; set; }
    public int MenuItems { get; set; }
    public int AddOns { get; set; }
    public int Ingredients { get; set; }
    public int InventoryItems { get; set; }
    public int Shifts { get; set; }
    public int Users { get; set; }
    public int AuditEvents { get; set; }
    public AdminDatabaseLatestUpdatesDto LatestUpdates { get; set; } = new();
}

public class AdminDatabaseLatestUpdatesDto
{
    public DateTime? Orders { get; set; }
    public DateTime? Customers { get; set; }
    public DateTime? MenuItems { get; set; }
    public DateTime? InventoryItems { get; set; }
}
