using Tannous.Pos.Application.OperationalIntegrity;
using Tannous.Pos.Application.OperationalCognition;
using Tannous.Pos.Infrastructure.Services.OperationalCognition;

namespace Tannous.Pos.Infrastructure.Services.OperationalIntegrity;

/// <summary>Process-local FIFO integrity snapshot retention (max 8; not persisted).</summary>
public sealed class OperationalIntegritySnapshotStore : IOperationalIntegritySnapshotStore
{
    private readonly BoundedFifoSnapshotStore<OperationalIntegritySnapshot> _store = new(
        OperationalCognitionSnapshotLimits.MaxStoredSnapshots);

    public int MaxSnapshots => _store.MaxSnapshots;

    public void Append(OperationalIntegritySnapshot snapshot) => _store.Append(snapshot);

    public IReadOnlyList<OperationalIntegritySnapshot> GetSnapshots() => _store.GetSnapshots();

    public void Clear() => _store.Clear();
}
