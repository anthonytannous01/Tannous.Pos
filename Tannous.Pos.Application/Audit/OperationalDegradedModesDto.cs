namespace Tannous.Pos.Application.Audit;

public sealed class OperationalDegradedModesDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public string PrimaryDegradedMode { get; init; } = OperationalDegradedModeTypes.Normal;
    public IReadOnlyList<OperationalDegradedModeEntryDto> Modes { get; init; } = Array.Empty<OperationalDegradedModeEntryDto>();
}

public sealed class OperationalDegradedModeEntryDto
{
    public string Mode { get; init; } = string.Empty;
    public bool Active { get; init; }
    public string SurvivabilityAssumption { get; init; } = string.Empty;
}
