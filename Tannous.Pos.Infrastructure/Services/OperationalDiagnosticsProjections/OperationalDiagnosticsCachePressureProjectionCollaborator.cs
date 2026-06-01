using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Infrastructure.Services.OperationalDiagnosticsProjections;

internal sealed class OperationalDiagnosticsCachePressureProjectionCollaborator
{
    private readonly OperationalDiagnosticsCacheProjectionContextFactory _contextFactory;
    private readonly IOperationalPressureLifecycleTracker _pressureLifecycle;
    private readonly ILogger _logger;

    public OperationalDiagnosticsCachePressureProjectionCollaborator(
        OperationalDiagnosticsCacheProjectionContextFactory contextFactory,
        IOperationalPressureLifecycleTracker pressureLifecycle,
        ILogger logger)
    {
        _contextFactory = contextFactory;
        _pressureLifecycle = pressureLifecycle;
        _logger = logger;
    }

    public OperationalPressureLifecycleDto GetPressureLifecycle()
    {
        var pressureState = _contextFactory.PressureState;
        var lifecycle = _pressureLifecycle.GetSnapshot();
        var dto = OperationalPressureGovernanceProjectionBuilder.BuildLifecycle(pressureState, lifecycle);

        _pressureLifecycle.NotePressureElevated(
            pressureState.QueryDateRangeClamped,
            pressureState.QueryPageSizeClamped,
            pressureState.ForensicExportTruncated);

        _logger.LogInformation(
            "Operational pressure lifecycle: pressure lifecycle queried. State={State}, Epoch={Epoch}, Sticky={Sticky}",
            dto.LifecycleState,
            dto.ActiveEpoch,
            dto.StickyPressureDetected);

        return dto;
    }

    public OperationalPressureRecoveryDto GetPressureRecovery()
    {
        var pressureState = _contextFactory.PressureState;
        var lifecycle = _pressureLifecycle.GetSnapshot();
        var telemetry = _contextFactory.GetTelemetry();
        var recovery = OperationalPressureGovernanceProjectionBuilder.BuildRecovery(
            pressureState,
            telemetry,
            lifecycle);

        _logger.LogInformation(
            "Operational pressure recovery: pressure recovery queried. Classification={Classification}, FlagsCleared={FlagsCleared}",
            recovery.RecoveryClassification,
            recovery.PressureFlagsCleared);

        foreach (var signal in recovery.StabilizationWindow.StabilizationSignals.Take(3))
        {
            _logger.LogInformation(
                "Operational recovery stabilization: stabilization signal observed. Signal={Signal}",
                signal);
        }

        return recovery;
    }

    public OperationalPressureConvergenceDto GetPressureConvergence()
    {
        var context = _contextFactory.BuildFullContext();
        var lifecycle = _pressureLifecycle.GetSnapshot();
        var convergence = OperationalPressureGovernanceProjectionBuilder.BuildConvergence(
            _contextFactory.PressureState,
            context.Telemetry,
            lifecycle,
            context.Overview,
            context.Stability);

        _logger.LogInformation(
            "Operational pressure convergence: pressure convergence queried. Classification={Classification}, Score={Score}",
            convergence.ConvergenceClassification,
            convergence.ConvergenceScore);

        return convergence;
    }
}
