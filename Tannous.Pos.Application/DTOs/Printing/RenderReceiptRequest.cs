namespace Tannous.Pos.Application.DTOs.Printing;

public class RenderReceiptRequest
{
    public Guid OrderId { get; set; }
    public int? LineWidth { get; set; }  // overrides template
}
