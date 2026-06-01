using Tannous.Pos.Application.OperationalRecovery;

namespace Tannous.Pos.Application.OperationalSituationRoom;

/// <summary>Structured stabilization and recovery outlook.</summary>
public sealed class OperationalSituationOutlookDto
{
    public OperationalSituationDirection RecoveryTrajectory { get; init; }
    public OperationalExecutiveSeverity EscalationLikelihood { get; init; }
    public OperationalExecutiveSeverity StabilizationLikelihood { get; init; }
    public OperationalRecoveryConfidence OperationalConfidence { get; init; }
    public string DominantConstraint { get; init; } = string.Empty;
    public string RecommendedOperatorPriority { get; init; } = string.Empty;
}
