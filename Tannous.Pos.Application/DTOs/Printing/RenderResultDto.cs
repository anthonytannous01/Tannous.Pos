namespace Tannous.Pos.Application.DTOs.Printing;

public class RenderResultDto
{
    public string PlainText { get; set; } = string.Empty;          // preview text (what app can print or inspect)
    public string? SuggestedCodePage { get; set; } = "cp437"; // e.g., cp437
}
