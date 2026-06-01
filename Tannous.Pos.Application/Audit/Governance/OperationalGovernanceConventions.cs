namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>Onboarding conventions for operational cache governance extensions.</summary>
public static class OperationalGovernanceConventions
{
    public const string ProjectionBuilderSuffix = "ProjectionBuilder";
    public const string ExplainabilityBuilderSuffix = "ExplainabilityBuilder";
    public const string ClassifierSuffix = "Classifier";
    public const string GovernanceSuffix = "Governance";

    public static IReadOnlyList<string> NamingStandards { get; } =
    [
        "Domain types use Operational{Domain}{Role} naming (e.g. OperationalCacheInvalidationProjectionBuilder).",
        "Explainability codes are PascalCase, max 48 chars, bounded to profile cap.",
        "Governance constants live in *Governance.cs per domain module.",
        "Classifiers are static, deterministic, and avoid throw for advisory paths."
    ];

    public static IReadOnlyList<string> ExplainabilityContributionRules { get; } =
    [
        "Domain explainability builders contribute signals only; composition uses OperationalGovernanceExplainabilityComposer.",
        "No dynamic text blobs; use bounded reason/trigger codes only.",
        "Deterministic ordering: cache profile preserves insertion order; other profiles sort ordinally."
    ];

    public static IReadOnlyList<string> ProjectionBuilderResponsibilities { get; } =
    [
        "Projection builders map composition context to DTOs; no HTTP or persistence concerns.",
        "Reuse OperationalGovernanceCompositionContext; do not rebuild telemetry/stale-risk independently.",
        "Collaborators in Infrastructure delegate to Application projection builders."
    ];

    public static IReadOnlyList<string> TelemetryUsageRules { get; } =
    [
        "Read telemetry via OperationalGovernanceTelemetryAccess.CaptureSnapshot only in governance pipeline/factory paths.",
        "Telemetry counters are advisory, singleton, non-persistent.",
        "Do not mutate business/replay/reconciliation state from governance paths."
    ];

    public static IReadOnlyList<string> ClassifierPlacementRules { get; } =
    [
        "Classifiers belong to their domain module; cross-domain usage goes through composition context outputs.",
        "Shared ratio/score math uses OperationalGovernanceThresholdEvaluator (Core module).",
        "No OS memory APIs or distributed cache references in classifiers."
    ];

    public static IReadOnlyList<string> AllowedDependencyDirections { get; } =
    [
        "Core → (none)",
        "Pressure → Core",
        "Invalidation → Core",
        "Survivability → Core",
        "Consistency → Core, Survivability",
        "Convergence → Core, Pressure, Consistency",
        "Infrastructure collaborators → Application modules only (never WebApi mutation endpoints)."
    ];

    public static IReadOnlyList<string> NonGoals { get; } =
    [
        "No runtime plugin systems, reflection discovery, or dynamic module registration.",
        "No Redis, distributed cache, persistence, hosted services, or automatic remediation.",
        "No payload/body caching; metadata and telemetry projections only."
    ];
}
