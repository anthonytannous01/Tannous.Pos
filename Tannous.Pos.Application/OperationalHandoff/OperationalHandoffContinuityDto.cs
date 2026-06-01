using Tannous.Pos.Application.OperationalAttention;
using Tannous.Pos.Application.OperationalBriefing;
using Tannous.Pos.Application.OperationalEquilibrium;
using Tannous.Pos.Application.OperationalStrategy;

namespace Tannous.Pos.Application.OperationalHandoff;

/// <summary>
/// Bounded-window continuity assessment for operator shift handoff.
/// Compares first vs. most recent cognition snapshot to classify state transitions.
/// Attaches current point-in-time briefing for incoming operator context.
/// </summary>
public sealed class OperationalHandoffContinuityDto
{
    /// <summary>Unique identifier for this handoff instance.</summary>
    public Guid HandoffId { get; init; } = Guid.NewGuid();

    /// <summary>UTC timestamp when this handoff was generated.</summary>
    public DateTime GeneratedAtUtc { get; init; }

    /// <summary>Staleness of the cognition data used (reuses BriefingCognitionAge).</summary>
    public BriefingCognitionAge CognitionAge { get; init; }

    /// <summary>Total snapshot count across all three stores in the bounded window.</summary>
    public int SnapshotWindowCount { get; init; }

    /// <summary>Timestamp of the oldest snapshot across all stores, if any.</summary>
    public DateTime? WindowStartUtc { get; init; }

    /// <summary>Timestamp of the most recent snapshot across all stores, if any.</summary>
    public DateTime? WindowEndUtc { get; init; }

    /// <summary>Duration of the cognition window in minutes, if any snapshots exist.</summary>
    public double? WindowDurationMinutes { get; init; }

    public HandoffContinuityTransition EquilibriumTransition { get; init; }
    public OperationalEquilibriumState EquilibriumAtWindowStart { get; init; }
    public OperationalEquilibriumState EquilibriumAtWindowEnd { get; init; }

    public HandoffContinuityTransition StrategyTransition { get; init; }
    public OperationalStrategicPostureType StrategyAtWindowStart { get; init; }
    public OperationalStrategicPostureType StrategyAtWindowEnd { get; init; }

    public HandoffContinuityTransition AttentionTransition { get; init; }
    public OperationalPriorityType AttentionAtWindowStart { get; init; }
    public OperationalPriorityType AttentionAtWindowEnd { get; init; }

    /// <summary>Highest urgency area from the most recent attention snapshot, if available.</summary>
    public string HighestUrgencyArea { get; init; } = string.Empty;

    /// <summary>Current point-in-time briefing for the incoming operator.</summary>
    public OperationalBriefingSummaryDto CurrentBriefing { get; init; } = new();

    /// <summary>Operator-readable continuity narrative for the shift handoff.</summary>
    public string HandoffNarrative { get; init; } = string.Empty;
}
