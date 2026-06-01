using Tannous.Pos.Application.OperationalRecovery;
using Tannous.Pos.Application.OperationalSituationRoom;

namespace Tannous.Pos.Application.OperationalCognition;

/// <summary>Normalized operator continuity phrasing — explicit, deterministic, not templated.</summary>
public static class OperationalContinuityPhrasing
{
    public const string BoundedContinuityWindow = "bounded continuity window";

    public static string ConsistentAcrossBoundedWindow(string domainLabel) =>
        $"{domainLabel} consistent across {BoundedContinuityWindow}";

    public static string RemainsConsistentAcrossBoundedWindow(string domainLabel) =>
        $"{domainLabel} remains consistent across {BoundedContinuityWindow}";

    public static string StateShift(string domainLabel, string priorValue, string currentValue) =>
        $"{domainLabel} shifted from {priorValue} to {currentValue}";

    public static string MovedFromTo(string domainLabel, string priorValue, string currentValue) =>
        $"{domainLabel} moved from {priorValue} to {currentValue}";

    public static string NoSignalInBoundedWindow(string signalDescription) =>
        $"No {signalDescription} in {BoundedContinuityWindow}";

    public static string SignalCountInBoundedWindow(int count, string signalName) =>
        $"{count} {signalName} signal(s) in {BoundedContinuityWindow}";

    public static string SignalCountSustainedInBoundedWindow(int count, string signalName) =>
        count > 0
            ? $"{count} {signalName} signal(s) sustained in bounded window"
            : $"{signalName} within normal bounded continuity";

    public static string EscalationMomentumAlignment(
        string escalationMomentum,
        string improvingPhrase,
        string worseningPhrase,
        string stablePhrase)
    {
        if (escalationMomentum.Contains("collapsing", StringComparison.OrdinalIgnoreCase))
            return improvingPhrase;

        if (escalationMomentum.Contains("expanding", StringComparison.OrdinalIgnoreCase))
            return worseningPhrase;

        return stablePhrase;
    }

    public static string RecoveryAlignment(
        OperationalRecoveryPostureDto recovery,
        string improvingPhrase,
        string requiresUpstreamPhrase)
    {
        return recovery.OverallDirection is OperationalRecoveryDirection.Improving
                or OperationalRecoveryDirection.Converging
            ? improvingPhrase
            : requiresUpstreamPhrase;
    }

    public static string StabilizationSituationAlignment(
        OperationalSituationRoomDto situationRoom,
        string strengtheningPhrase,
        string requiresReinforcementPhrase,
        string withinBoundsPhrase)
    {
        return situationRoom.StabilizationDirection switch
        {
            OperationalSituationDirection.Stabilizing or OperationalSituationDirection.Improving =>
                strengtheningPhrase,
            OperationalSituationDirection.Escalating or OperationalSituationDirection.Degrading =>
                requiresReinforcementPhrase,
            _ => withinBoundsPhrase
        };
    }

    public static string PriorityAreaConsistency(bool isConsistent) =>
        isConsistent
            ? "Priority area consistent across bounded continuity"
            : "Priority area evolving within bounded continuity";

    public static string StabilizationWithinNormalBounds() =>
        "Stabilization within normal bounded continuity";

    public static string EscalationRecurringAcrossBoundedWindow() =>
        "Escalation continuity recurring across bounded window";

    public static string CausalityPropagationRecurringAcrossBoundedWindow() =>
        "Causality propagation continuity recurring across bounded window";
}
