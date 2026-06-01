namespace Tannous.Pos.Application.Audit.Governance;

/// <summary>Bounded governance fingerprint settings (process-local; not persisted).</summary>
public static class OperationalGovernanceFingerprintConstants
{
    public const int MaxSignatureSegments = 16;
    public const int MaxSignatureExplainabilityCodes = 6;
    public const int MaxPreviousFingerprintEntries = 1;
    public const int FingerprintHashHexLength = 16;
    public const int MaxDriftSignals = 6;
    public const int MaxComparisonSignals = 6;
}
