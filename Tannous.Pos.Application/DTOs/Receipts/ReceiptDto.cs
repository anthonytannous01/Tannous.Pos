namespace Tannous.Pos.Application.DTOs.Receipts;

public class ReceiptDto
{
    public Guid     OrderId       { get; set; }
    public string   OrderNumber   { get; set; } = string.Empty;
    public string   OrderType     { get; set; } = string.Empty;
    public DateTime PrintedAt     { get; set; }
    public bool     IsReprint     { get; set; }

    public string  BusinessName   { get; set; } = string.Empty;
    public string? BusinessNameAr { get; set; }
    public string? BusinessPhone  { get; set; }
    public string? BusinessAddress { get; set; }
    public string? TaxId          { get; set; }

    public string? CustomerName { get; set; }
    public string? TableLabel   { get; set; }

    public List<ReceiptLineDto> Lines { get; set; } = new();

    public decimal SubTotal       { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount      { get; set; }
    public decimal StampDuty      { get; set; }
    public decimal TotalUsd       { get; set; }
    public decimal TotalLbp       { get; set; }
    public bool    StampDutyEnabled { get; set; }

    public List<ReceiptPaymentDto> Payments { get; set; } = new();
    public decimal AmountTendered  { get; set; }
    public decimal ChangeDue       { get; set; }

    public string FooterMessage   { get; set; } = "Thank you for visiting!";
    public string FooterMessageAr { get; set; } = "شكراً لزيارتكم!";
}

public class ReceiptLineDto
{
    public string  Name     { get; set; } = string.Empty;
    public string? NameAr   { get; set; }
    public int     Qty      { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes    { get; set; }
}

public class ReceiptPaymentDto
{
    public string  Method  { get; set; } = string.Empty;
    public decimal Amount  { get; set; }
}
