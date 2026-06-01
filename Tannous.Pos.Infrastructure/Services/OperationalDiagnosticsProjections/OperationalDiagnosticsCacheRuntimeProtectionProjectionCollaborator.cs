using Microsoft.Extensions.Logging;
using Tannous.Pos.Application.Audit;
using Tannous.Pos.Application.Audit.Governance;

namespace Tannous.Pos.Infrastructure.Services.OperationalDiagnosticsProjections;

internal sealed class OperationalDiagnosticsCacheRuntimeProtectionProjectionCollaborator
{
    private const int ProjectionCollaboratorCount = 8;

    private readonly OperationalDiagnosticsCacheProjectionContextFactory _contextFactory;
    private readonly IOperationalDiagnosticsCacheTelemetry _telemetry;
    private readonly ILogger _logger;

    public OperationalDiagnosticsCacheRuntimeProtectionProjectionCollaborator(
        OperationalDiagnosticsCacheProjectionContextFactory contextFactory,
        IOperationalDiagnosticsCacheTelemetry telemetry,
        ILogger logger)
    {
        _contextFactory = contextFactory;
        _telemetry = telemetry;
        _logger = logger;
    }

    public OperationalGovernanceRuntimeProtectionDto GetRuntimeProtection()
    {
        var access = _contextFactory.AcquireSnapshot();
        var context = access.Composition.Context;
        RecordRuntimeTelemetry(context);

        var telemetrySnapshot = _contextFactory.GetTelemetry();

        var determinism = OperationalGovernanceDeterminismAudit.AuditComposition(
            access.Composition,
            access.WasReused);
        var consistency = OperationalGovernanceRuntimeConsistencyGuard.Validate(
            access,
            priorComposition: null,
            governanceResetOccurred: false);

        var advisorySignals = new List<string>();
        if (determinism.IsDeterministic)
            advisorySignals.Add("DeterminismAuditPass");
        else
            advisorySignals.AddRange(determinism.Issues.Select(i => $"Determinism:{i}"));

        if (consistency.IsConsistent)
            advisorySignals.Add("RuntimeConsistencyPass");
        else
            advisorySignals.AddRange(consistency.Issues.Select(i => $"Consistency:{i}"));

        advisorySignals.Add(OperationalGovernanceFreezePolicy.RegistryModuleCount() == OperationalGovernanceFreezePolicy.FrozenModuleCount
            ? "GovernanceFreezeCompliant"
            : "GovernanceFreezeViolation");

        var runtimeBaseline = OperationalGovernanceRuntimeBaselineBuilder.Build(
            access.Composition,
            telemetrySnapshot,
            ProjectionCollaboratorCount,
            access.WasReused,
            advisorySignals);
        var productionReadiness = BuildProductionReadiness(access.Composition.Context, telemetrySnapshot);

        var dto = OperationalGovernanceRuntimeProtectionEnricher.Enrich(
            access.Composition.RuntimeProtection,
            runtimeBaseline,
            productionReadiness);

        _logger.LogInformation(
            "Operational governance runtime protection: projection queried. ExecutionState={ExecutionState}, BudgetPressure={BudgetPressure}, Complexity={Complexity}, Saturation={Saturation}, FailsafeActive={FailsafeActive}, ExecutionBudget={ExecutionBudget}",
            dto.ExecutionState,
            dto.BudgetPressure,
            dto.ProjectionComplexity,
            dto.TelemetrySaturationLevel,
            dto.Failsafe.FailsafeActive,
            dto.RuntimeBaseline.ExecutionBudgetState);

        _logger.LogInformation(
            "Operational governance runtime baseline: baseline queried. TimingBand={TimingBand}, BuildElapsedMs={BuildElapsedMs}, ReuseRatio={ReuseRatio}, Collaborators={Collaborators}, PipelineStages={PipelineStages}",
            dto.RuntimeBaseline.ProjectionTiming.TimingBand,
            dto.RuntimeBaseline.ProjectionTiming.BuildElapsedMilliseconds,
            dto.RuntimeBaseline.SnapshotReuseRatio,
            dto.RuntimeBaseline.ProjectionCollaboratorCount,
            dto.RuntimeBaseline.PipelineStageCount);

        if (dto.Failsafe.FailsafeActive)
        {
            _logger.LogWarning(
                "Operational governance failsafe: failsafe advisory active. WarmRecommendationsSuppressed={WarmRecommendationsSuppressed}, ExplainabilityTruncated={ExplainabilityTruncated}",
                dto.Failsafe.WarmRecommendationsSuppressed,
                dto.Failsafe.ExplainabilityTruncated);
        }

        return dto;
    }

    public OperationalGovernanceExecutionDiagnosticsDto GetExecutionDiagnostics()
    {
        var access = _contextFactory.AcquireSnapshot();
        RecordRuntimeTelemetry(access.Composition.Context);

        var dto = access.Composition.ExecutionDiagnostics;

        _logger.LogInformation(
            "Operational governance execution diagnostics: projection queried. ExecutionState={ExecutionState}, BudgetPressure={BudgetPressure}, Complexity={Complexity}, StabilityScore={StabilityScore}",
            dto.ExecutionState,
            dto.BudgetPressure,
            dto.ProjectionComplexity,
            dto.StabilityScore);

        return dto;
    }

    public OperationalGovernanceTelemetrySaturationDto GetTelemetrySaturation()
    {
        var access = _contextFactory.AcquireSnapshot();
        RecordRuntimeTelemetry(access.Composition.Context);

        var dto = access.Composition.TelemetrySaturation;

        _logger.LogInformation(
            "Operational telemetry saturation: projection queried. SaturationLevel={SaturationLevel}, ActiveCategories={ActiveCategories}, ScopedKeys={ScopedKeys}",
            dto.SaturationLevel,
            dto.ActiveTelemetryCategories,
            dto.ActiveScopedKeyCount);

        return dto;
    }

    private OperationalGovernanceProductionReadinessDto BuildProductionReadiness(
        OperationalGovernanceCompositionContext context,
        OperationalDiagnosticsCacheTelemetrySnapshotDto telemetrySnapshot)
    {
        var reuseRatio = OperationalGovernanceProjectionReuseClassifier.ComputeHitRatio(
            telemetrySnapshot.ProjectionReuseHits,
            telemetrySnapshot.ProjectionReuseMisses);

        return OperationalGovernanceProductionReadinessClassifier.Classify(
            context,
            telemetrySnapshot,
            reuseRatio,
            telemetrySnapshot.GovernanceFingerprintTransitions);
    }

    private void RecordRuntimeTelemetry(OperationalGovernanceCompositionContext context)
    {
        if (context.ExecutionState is OperationalGovernanceExecutionState.Constrained
            or OperationalGovernanceExecutionState.Saturated
            or OperationalGovernanceExecutionState.Failsafe)
            _telemetry.RecordRuntimeBudgetConstrainedEvent();

        if (context.ProjectionComplexity >= OperationalGovernanceProjectionComplexity.Heavy)
            _telemetry.RecordProjectionComplexityElevation();

        if (context.TelemetrySaturationLevel >= OperationalGovernanceTelemetrySaturationLevel.Elevated)
            _telemetry.RecordTelemetrySaturationEvent();

        if (context.FailsafeActive)
            _telemetry.RecordGovernanceFailsafeActivation();
    }
}
