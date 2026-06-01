namespace Tannous.Pos.Application.OperationalBriefing;

/// <summary>Staleness classification for briefing cognition data.</summary>
public enum BriefingCognitionAge
{
    /// <summary>No snapshot data available (stores empty — cognition APIs not yet called).</summary>
    NoData,

    /// <summary>Newest available snapshot is less than 5 minutes old.</summary>
    Fresh,

    /// <summary>Newest available snapshot is between 5 and 30 minutes old.</summary>
    Warm,

    /// <summary>Newest available snapshot is older than 30 minutes.</summary>
    Stale
}
