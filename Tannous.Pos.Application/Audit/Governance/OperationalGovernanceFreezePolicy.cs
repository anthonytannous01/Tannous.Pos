using Tannous.Pos.Application.Audit.Governance.Modules;

namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>
/// Explicit governance freeze policy (Steps 1–18 complete; no expansion without consolidation review).
/// GOVERNANCE: advisory enforcement only; not deployment gating.
/// </summary>
public static class OperationalGovernanceFreezePolicy
{
    public const int FrozenModuleCount = 6;
    public const int FrozenPipelineStageCount = OperationalGovernanceComplexityMetrics.MaxPipelineStageCount;

    public static readonly IReadOnlyList<string> FreezeRationale =
    [
        "Governance diagnostics are feature-complete for operational cache observability.",
        "Further endpoint growth increases operator cognitive load and maintenance risk.",
        "Projection orchestration is near configured complexity ceilings.",
        "Expansion requires consolidation review — not additive feature work."
    ];

    public static readonly IReadOnlyList<string> ApprovedExtensionPolicy =
    [
        "Bug fixes and determinism hardening only within existing surfaces.",
        "Consolidation that reduces collaborator fanout or DTO count before any addition.",
        "No new Admin GET routes without removing or merging equivalent surface.",
        "No new governance modules, pipeline stages, or explainability profiles while frozen."
    ];

    public static readonly IReadOnlyList<string> IntentionalNonGoals =
    [
        "No persistence, distributed cache, workers, or auto-remediation.",
        "No business replay/reconciliation semantic changes.",
        "No public/mobile API exposure of governance diagnostics."
    ];

    public static int FrozenExplainabilityProfileCount =>
        OperationalGovernanceSurfaceBudget.MaxGovernanceExplainabilityProfiles;

    public static bool IsModuleCountFrozen(int measuredModuleCount) =>
        measuredModuleCount == FrozenModuleCount;

    public static bool IsPipelineStageCountFrozen(int measuredStageCount) =>
        measuredStageCount == FrozenPipelineStageCount;

    public static bool IsExplainabilityProfileCountFrozen(int measuredProfileCount) =>
        measuredProfileCount <= FrozenExplainabilityProfileCount;

    public static int RegistryModuleCount() =>
        OperationalGovernanceModuleRegistry.All.Count;
}
