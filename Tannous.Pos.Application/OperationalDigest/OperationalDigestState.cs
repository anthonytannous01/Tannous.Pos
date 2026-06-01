namespace Tannous.Pos.Application.OperationalDigest;

/// <summary>Overall condensed operational digest state.</summary>
public enum OperationalDigestState
{
    Stable = 0,
    AttentionRequired = 1,
    Escalating = 2,
    Recovering = 3,
    Fragmented = 4
}
