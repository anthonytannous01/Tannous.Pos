namespace Tannous.Pos.Application.Audit;

public sealed class OperationalCacheDegradationDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public OperationalCacheDegradationState State { get; init; }
    public string Classification { get; init; } = string.Empty;
    public string RecommendedOperatorAction { get; init; } = string.Empty;
    public bool ExcessiveBypassIndicated { get; init; }
    public bool UnstableHitRatioIndicated { get; init; }
    public bool SaturatedScopedKeysIndicated { get; init; }
    public bool RepeatedInvalidationChurnIndicated { get; init; }
    public bool PersistentColdStartIndicated { get; init; }
    public IReadOnlyList<string> ReasonCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> TriggerSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> GovernanceNotes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RecommendedActions { get; init; } = Array.Empty<string>();
}
