namespace Tannous.Pos.Application.OperationalReplayWorkbench;

public enum OperationalReplayStabilizationState
{
    Stable = 0,
    Stabilizing = 1,
    Escalating = 2,
    Contained = 3,
    InterventionRecommended = 4
}
