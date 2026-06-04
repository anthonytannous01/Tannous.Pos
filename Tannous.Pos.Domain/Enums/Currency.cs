namespace Tannous.Pos.Domain.Enums;

/// <summary>
/// Supported tender currencies. USD is the base (reporting) currency.
/// LBP is the secondary display and tender currency used in the Lebanese market.
/// </summary>
public enum Currency
{
    USD = 0,
    LBP = 1
}
