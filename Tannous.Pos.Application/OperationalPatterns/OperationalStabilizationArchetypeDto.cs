namespace Tannous.Pos.Application.OperationalPatterns;

/// <summary>Reusable deterministic stabilization archetype.</summary>
public sealed class OperationalStabilizationArchetypeDto
{
    public string ArchetypeId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public OperationalArchetypeType ArchetypeType { get; init; }
    public string TriggerCharacteristics { get; init; } = string.Empty;
    public string RecoveryBehavior { get; init; } = string.Empty;
    public string EscalationBehavior { get; init; } = string.Empty;
    public string DominantConstraint { get; init; } = string.Empty;
    public OperationalPatternSeverity StabilizationLikelihood { get; init; }
    public OperationalPatternConfidence RecoveryConfidence { get; init; }
    public string OperatorInterpretation { get; init; } = string.Empty;
}
