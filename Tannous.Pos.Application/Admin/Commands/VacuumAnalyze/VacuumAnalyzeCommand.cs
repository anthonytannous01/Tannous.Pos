using MediatR;

namespace Tannous.Pos.Application.Admin.Commands.VacuumAnalyze;

public class VacuumAnalyzeCommand : IRequest<VacuumAnalyzeResult>
{
}

public class VacuumAnalyzeResult
{
    public string Message { get; set; } = string.Empty;
    public string? Environment { get; set; }
    public DateTime RequestedAt { get; set; }
}
