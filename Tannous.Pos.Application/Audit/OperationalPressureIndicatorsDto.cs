namespace Tannous.Pos.Application.Audit;

public sealed class OperationalPressureIndicatorsDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public IReadOnlyDictionary<string, bool> Indicators { get; init; } =
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string> Diagnostics { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
