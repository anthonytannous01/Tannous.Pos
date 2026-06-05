namespace Tannous.Pos.Application.DTOs.Feedback;

public class FeedbackDto
{
    public Guid     Id           { get; set; }
    public int      Rating       { get; set; }
    public string?  Comment      { get; set; }
    public int      Category     { get; set; }
    public string   CategoryName { get; set; } = string.Empty;
    public Guid?    OrderId      { get; set; }
    public string?  OrderNumber  { get; set; }
    public string?  CustomerName { get; set; }
    public Guid?    BranchId     { get; set; }
    public DateTime CreatedAt    { get; set; }
}

public class FeedbackSummaryDto
{
    public int     TotalCount    { get; set; }
    public double  AverageRating { get; set; }
    public int     FiveStars     { get; set; }
    public int     FourStars     { get; set; }
    public int     ThreeStars    { get; set; }
    public int     TwoStars      { get; set; }
    public int     OneStar       { get; set; }
    public int     Complaints    { get; set; }
    public List<FeedbackDto> Recent { get; set; } = new();
}
