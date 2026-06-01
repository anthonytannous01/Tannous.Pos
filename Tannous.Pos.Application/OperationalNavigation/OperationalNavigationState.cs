namespace Tannous.Pos.Application.OperationalNavigation;

public enum OperationalNavigationState
{
    Stable = 0,
    Monitoring = 1,
    AttentionRequired = 2,
    ActionNeeded = 3,
    Protective = 4,
    Degraded = 5
}
