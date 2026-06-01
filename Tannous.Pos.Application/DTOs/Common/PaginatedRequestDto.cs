namespace Tannous.Pos.Application.DTOs.Common;

public class PaginatedRequestDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Sort { get; set; }
    public string? Dir { get; set; } = "asc";
    /// <summary>Primary search query parameter (<c>q</c>).</summary>
    public string? Q { get; set; }
    /// <summary>Mobile clients use query key <c>search</c>; treated as an alias for <see cref="Q"/>.</summary>
    public string? Search { get; set; }

    /// <summary>Effective search text from <c>q</c> or <c>search</c>.</summary>
    public string? SearchQuery => string.IsNullOrWhiteSpace(Q) ? Search : Q;

    public int Skip => (Page - 1) * PageSize;
    public int Take => Math.Min(PageSize, 100); // Max 100 items per page
}
