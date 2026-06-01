using MediatR;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Admin.Commands.VacuumAnalyze;

public class VacuumAnalyzeCommandHandler : IRequestHandler<VacuumAnalyzeCommand, VacuumAnalyzeResult>
{
    private readonly IAuditService _auditService;

    public VacuumAnalyzeCommandHandler(IAuditService auditService)
    {
        _auditService = auditService;
    }

    public async Task<VacuumAnalyzeResult> Handle(VacuumAnalyzeCommand request, CancellationToken cancellationToken)
    {
        var requestedAt = DateTime.UtcNow;
        var environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        await _auditService.LogEventAsync("VacuumAnalyze", "Database", null, new
        {
            RequestedAt = requestedAt,
            Environment = environment
        });

        return new VacuumAnalyzeResult
        {
            Message     = "Vacuum analyze request logged. In production, this should be handled by database maintenance scripts.",
            Environment = environment,
            RequestedAt = requestedAt
        };
    }
}
