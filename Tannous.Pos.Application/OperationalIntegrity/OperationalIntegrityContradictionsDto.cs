namespace Tannous.Pos.Application.OperationalIntegrity;

/// <summary>Bounded operational interpretation contradictions.</summary>
public sealed class OperationalIntegrityContradictionsDto
{
    public DateTime GeneratedAtUtc { get; init; }
    public int ContradictionCount { get; init; }
    public IReadOnlyList<OperationalContradictionDto> Contradictions { get; init; } =
        Array.Empty<OperationalContradictionDto>();
    public string IntegrityNote { get; init; } =
        "Advisory deterministic contradiction visibility across operational intelligence layers.";
}
