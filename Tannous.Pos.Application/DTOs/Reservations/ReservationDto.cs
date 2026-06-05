namespace Tannous.Pos.Application.DTOs.Reservations;

public class ReservationDto
{
    public Guid     Id                  { get; set; }
    public string   CustomerName        { get; set; } = string.Empty;
    public string?  CustomerPhone       { get; set; }
    public int      PartySize           { get; set; }
    public DateTime ReservationDateTime { get; set; }
    public string?  Notes               { get; set; }
    public int      Status              { get; set; }
    public string   StatusName          { get; set; } = string.Empty;
    public Guid?    TableId             { get; set; }
    public string?  TableNumber         { get; set; }
    public string?  FloorPlanName       { get; set; }
    public Guid?    BranchId            { get; set; }
    public DateTime CreatedAt           { get; set; }
}

public class AvailableTableDto
{
    public Guid   Id          { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public string? Label      { get; set; }
    public int    Capacity    { get; set; }
    public string FloorPlan   { get; set; } = string.Empty;
}
