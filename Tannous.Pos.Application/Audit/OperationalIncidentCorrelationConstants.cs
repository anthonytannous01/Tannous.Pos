namespace Tannous.Pos.Application.Audit;

public static class OperationalIncidentCorrelationConstants
{
    public const int MaxSignalsPerCorrelationQuery = 500;
    public const int MaxIncidentsReturned = 100;
    public const int RepeatedReplayMismatchThreshold = 2;
    public const int RepeatedUnresolvedConflictThreshold = 3;
    public const int RepeatedInventoryDriftThreshold = 2;
    public const int CascadingSubsystemMinimum = 2;
}
