namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>Operational ownership boundaries for governance maintenance.</summary>
public static class OperationalGovernanceOwnershipBoundaries
{
    public static IReadOnlyList<string> All { get; } =
    [
        "Application/Audit/Governance: projection builders, classifiers, freeze policy, determinism audits.",
        "Infrastructure/OperationalDiagnosticsProjections: collaborators, memoizer, snapshot store only.",
        "WebApi/Internal/OperationalAuditCacheDiagnosticsController: GET transport only; no business logic.",
        "Tests/Tannous.Pos.Architecture.Tests: freeze enforcement and measured ceiling guards.",
        "governance/*.ps1: debt scan metrics and budget reconciliation reporting only."
    ];

    public static IReadOnlyList<string> MaintenanceGuidance { get; } =
    [
        "Prefer consolidation over new endpoints or collaborators.",
        "Run architecture tests and governance scans before merging governance changes.",
        "Do not widen budgets during freeze; reduce surface first.",
        "Preserve explainability ordering and fingerprint determinism in all changes."
    ];

    public static IReadOnlyList<string> OperationalExpectations { get; } =
    [
        "Governance diagnostics are Admin GET-only and advisory.",
        "Snapshots are process-local and do not guarantee business freshness.",
        "Production readiness and runtime baselines are non-authoritative."
    ];
}
