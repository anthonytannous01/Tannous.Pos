namespace Tannous.Pos.Domain.Enums;

/// <summary>
/// Lifecycle of an employee clock-in/out record.
/// </summary>
public enum TimeEntryStatus
{
    Active    = 0,
    Completed = 1,
    Adjusted  = 2
}
