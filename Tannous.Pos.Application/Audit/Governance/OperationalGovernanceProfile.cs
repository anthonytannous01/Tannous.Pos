namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>
/// Internal governance verbosity profile (does not alter HTTP DTO contracts).
/// Standard preserves existing explainability bounds (Step 1–13 behavior).
/// </summary>
public enum OperationalGovernanceProfile
{
    Minimal,
    Standard,
    Detailed
}

public static class OperationalGovernanceProfileSettings
{
    public const int MinimalExplainabilityCap = 4;
    public const int StandardExplainabilityCap = 8;
    public const int DetailedExplainabilityCap = 8;

    public static int GetExplainabilityCap(OperationalGovernanceProfile profile) =>
        profile switch
        {
            OperationalGovernanceProfile.Minimal => MinimalExplainabilityCap,
            OperationalGovernanceProfile.Standard => StandardExplainabilityCap,
            OperationalGovernanceProfile.Detailed => DetailedExplainabilityCap,
            _ => StandardExplainabilityCap
        };

    public static OperationalGovernanceProfile Default => OperationalGovernanceProfile.Standard;
}
