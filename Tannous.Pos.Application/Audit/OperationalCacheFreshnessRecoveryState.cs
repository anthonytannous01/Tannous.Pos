namespace Tannous.Pos.Application.Audit;

public enum OperationalCacheFreshnessRecoveryState
{
    Stable = 0,
    Recovering = 1,
    Churned = 2,
    Unstable = 3
}
