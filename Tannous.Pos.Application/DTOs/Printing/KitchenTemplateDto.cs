namespace Tannous.Pos.Application.DTOs.Printing;

public class KitchenTemplateDto
{
    public int LineWidth { get; set; } = 42;
    public string Header { get; set; } = "KITCHEN TICKET";
    public bool PrintNotes { get; set; } = true;
}
