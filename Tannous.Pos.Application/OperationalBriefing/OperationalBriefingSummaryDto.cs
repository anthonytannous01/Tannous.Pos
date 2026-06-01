using Tannous.Pos.Application.OperationalAttention;
using Tannous.Pos.Application.OperationalEquilibrium;
using Tannous.Pos.Application.OperationalStrategy;

namespace Tannous.Pos.Application.OperationalBriefing;

/// <summary>
/// Compact briefing summary for shift handoff consumption.
/// Contains only the highest-signal fields from the full briefing package.
/// </summary>
public sealed class OperationalBriefingSummaryDto
{
    public Guid BriefingId { get; init; } = Guid.NewGuid();
    public DateTime GeneratedAtUtc { get; init; }
    public BriefingCognitionAge CognitionAge { get; init; }
    public OperationalEquilibriumState SystemicBalance { get; init; }
    public OperationalStrategicPostureType StrategicPosture { get; init; }
    public OperationalPriorityType DominantPriority { get; init; }
    public string HighestUrgencyArea { get; init; } = string.Empty;
    public string BriefingSummary { get; init; } = string.Empty;
}
