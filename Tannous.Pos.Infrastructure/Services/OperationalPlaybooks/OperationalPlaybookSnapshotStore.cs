using Tannous.Pos.Application.OperationalPlaybooks;
using Tannous.Pos.Application.OperationalCognition;
using Tannous.Pos.Infrastructure.Services.OperationalCognition;

namespace Tannous.Pos.Infrastructure.Services.OperationalPlaybooks;

/// <summary>Process-local FIFO playbook snapshot retention (max 8; not persisted).</summary>
public sealed class OperationalPlaybookSnapshotStore : IOperationalPlaybookSnapshotStore
{
    private readonly BoundedFifoSnapshotStore<OperationalPlaybookSnapshot> _store = new(
        OperationalCognitionSnapshotLimits.MaxStoredSnapshots);

    public int MaxSnapshots => _store.MaxSnapshots;

    public void Append(OperationalPlaybookSnapshot snapshot) => _store.Append(snapshot);

    public IReadOnlyList<OperationalPlaybookSnapshot> GetSnapshots() => _store.GetSnapshots();

    public void Clear() => _store.Clear();
}
