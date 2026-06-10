namespace Tannous.Pos.Application.DTOs.Delivery;

public class DeliveryDto
{
    public Guid     Id               { get; set; }
    public Guid     OrderId          { get; set; }
    public string?  OrderNumber      { get; set; }
    public decimal  OrderTotal       { get; set; }
    public string   DeliveryAddress  { get; set; } = string.Empty;
    public string?  ApartmentDetails { get; set; }
    public string?  CustomerPhone    { get; set; }
    public string?  CustomerName     { get; set; }
    public int      Channel          { get; set; }
    public string   ChannelName      { get; set; } = string.Empty;
    public int      Status           { get; set; }
    public string   StatusName       { get; set; } = string.Empty;
    public decimal  DeliveryFee      { get; set; }
    public int?     EstimatedMinutes { get; set; }
    public string?  Notes            { get; set; }
    public string?  DriverName       { get; set; }
    public string?  DriverPhone      { get; set; }
    public DateTime? AssignedAt      { get; set; }
    public DateTime? PickedUpAt      { get; set; }
    public DateTime? DeliveredAt     { get; set; }
    public Guid?    BranchId         { get; set; }
    public string?  ExternalOrderId        { get; set; }
    public string?  ExternalOrderReference { get; set; }
    public DateTime CreatedAt        { get; set; }
}
