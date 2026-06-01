namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>Deterministic governance snapshot keys (no user/session/query scoping).</summary>
public static class OperationalGovernanceSnapshotKeys
{
    public const string Standard = "governance-snapshot:standard";
    public const string Minimal = "governance-snapshot:minimal";
    public const string Detailed = "governance-snapshot:detailed";

    public static string ForProfile(OperationalGovernanceProfile profile) =>
        profile switch
        {
            OperationalGovernanceProfile.Minimal => Minimal,
            OperationalGovernanceProfile.Standard => Standard,
            OperationalGovernanceProfile.Detailed => Detailed,
            _ => Standard
        };

    public static IReadOnlyList<string> All { get; } =
    [
        Minimal,
        Standard,
        Detailed
    ];
}
