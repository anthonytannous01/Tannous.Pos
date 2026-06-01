using Tannous.Pos.Application.OperationalPatterns;
using Tannous.Pos.Application.OperationalCognition;
using Tannous.Pos.Infrastructure.Services.OperationalCognition;

namespace Tannous.Pos.Infrastructure.Services.OperationalPatterns;

/// <summary>Process-local FIFO pattern snapshot retention (max 8; not persisted).</summary>
public sealed class OperationalPatternSnapshotStore : IOperationalPatternSnapshotStore
{
    private readonly BoundedFifoSnapshotStore<OperationalPatternSnapshot> _store = new(
        OperationalCognitionSnapshotLimits.MaxStoredSnapshots);

    public int MaxSnapshots => _store.MaxSnapshots;

    public void Append(OperationalPatternSnapshot snapshot) => _store.Append(snapshot);

    public IReadOnlyList<OperationalPatternSnapshot> GetSnapshots() => _store.GetSnapshots();

    public void Clear() => _store.Clear();
}
