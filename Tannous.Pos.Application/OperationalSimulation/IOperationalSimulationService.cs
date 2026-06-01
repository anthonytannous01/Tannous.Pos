namespace Tannous.Pos.Application.OperationalSimulation;

/// <summary>Operator hypothetical stabilization analysis (advisory, process-local).</summary>
public interface IOperationalSimulationService
{
    Task<OperationalSimulationScenariosDto> GetSimulationScenariosAsync(CancellationToken cancellationToken = default);

    Task<OperationalSimulationSummaryDto> GetSimulationSummaryAsync(CancellationToken cancellationToken = default);

    Task<OperationalSimulationOutlookDto> GetSimulationOutlookAsync(CancellationToken cancellationToken = default);
}
