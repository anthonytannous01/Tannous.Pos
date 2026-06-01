namespace Tannous.Pos.Application.Audit;

public enum OperationalCacheRecoveryContainmentState
{
    Stable = 0,
    Recovering = 1,
    Contained = 2,
    Escalated = 3
}
