using Tannous.Pos.Application.OperationalAttention;
using Tannous.Pos.Application.OperationalEquilibrium;
using Tannous.Pos.Application.OperationalStrategy;

namespace Tannous.Pos.Application.OperationalBriefing;

/// <summary>
/// Point-in-time operator briefing package for shift handoff or escalation review.
/// Composed from existing cognition snapshot stores — no recomputation triggered.
/// </summary>
public sealed class OperationalBriefingPackageDto
{
    /// <summary>Unique identifier for this briefing instance (for correlation/logging).</summary>
    public Guid BriefingId { get; init; } = Guid.NewGuid();

    /// <summary>UTC timestamp when this briefing was generated.</summary>
    public DateTime GeneratedAtUtc { get; init; }

    /// <summary>Staleness classification of the cognition data used.</summary>
    public BriefingCognitionAge CognitionAge { get; init; }

    /// <summary>How many of the 3 cognition sources had snapshot data.</summary>
    public int AvailableSourceCount { get; init; }

    /// <summary>Age in minutes of the oldest available latest snapshot, if any.</summary>
    public double? OldestSourceAgeMinutes { get; init; }

    public OperationalEquilibriumState SystemicBalance { get; init; }
    public OperationalStrainLevel SystemicStrainLevel { get; init; }
    public string HighestImbalanceArea { get; init; } = string.Empty;
    public int ImbalanceCount { get; init; }

    public OperationalStrategicPostureType StrategicPosture { get; init; }
    public OperationalCoordinationStrength OperationalAlignment { get; init; }
    public string StrategicFocus { get; init; } = string.Empty;

    public OperationalPriorityType DominantPriority { get; init; }
    public OperationalUrgencyLevel AttentionPressure { get; init; }
    public string HighestUrgencyArea { get; init; } = string.Empty;
    public int PriorityCount { get; init; }

    /// <summary>Single operator-readable briefing summary line.</summary>
    public string BriefingSummary { get; init; } = string.Empty;
}
