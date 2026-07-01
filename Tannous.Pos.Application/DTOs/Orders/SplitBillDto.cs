namespace Tannous.Pos.Application.DTOs.Orders;

public class SplitBillDto
{
    public Guid     OrderId         { get; set; }
    public decimal  OrderTotal      { get; set; }
    public decimal  AlreadyPaid     { get; set; }
    public decimal  Remaining       { get; set; }
    public int      Ways            { get; set; }
    public decimal  AmountPerPerson { get; set; }
    public int      PeopleRemaining { get; set; }
    public bool     IsFullyPaid     { get; set; }
    public List<SplitPortionDto> Portions { get; set; } = new();
}

public class SplitPortionDto
{
    public int     PersonNumber { get; set; }
    public decimal Amount       { get; set; }
    public bool    IsPaid       { get; set; }
}
