namespace Tannous.Pos.Application.OperationalAttention;

/// <summary>Bounded process-local attention snapshot retention.</summary>
public interface IOperationalAttentionSnapshotStore
{
    int MaxSnapshots { get; }
    void Append(OperationalAttentionSnapshot snapshot);
    IReadOnlyList<OperationalAttentionSnapshot> GetSnapshots();
    void Clear();
}
