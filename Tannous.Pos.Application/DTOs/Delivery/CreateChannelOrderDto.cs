namespace Tannous.Pos.Application.DTOs.Delivery;

/// <summary>
/// Normalised order payload from any external delivery channel.
/// Populated by IDeliveryChannelAdapter.ParseOrder().
/// </summary>
public class CreateChannelOrderDto
{
    public string  ExternalOrderId   { get; set; } = string.Empty; // platform's own order id
    public string  CustomerName      { get; set; } = string.Empty;
    public string? CustomerPhone     { get; set; }
    public string  DeliveryAddress   { get; set; } = string.Empty;
    public string? ApartmentDetails  { get; set; }
    public string? Notes             { get; set; }
    public decimal DeliveryFee       { get; set; } = 0m;
    public int?    EstimatedMinutes  { get; set; }
    public List<ChannelOrderLineDto> Lines { get; set; } = new();
}

public class ChannelOrderLineDto
{
    public string  ItemName       { get; set; } = string.Empty; // platform item name — best-effort match
    public string? ExternalItemId { get; set; }                 // platform SKU/id for future menu sync
    public int     Quantity       { get; set; } = 1;
    public decimal UnitPrice      { get; set; } = 0m;
    public string? Notes          { get; set; }
}
