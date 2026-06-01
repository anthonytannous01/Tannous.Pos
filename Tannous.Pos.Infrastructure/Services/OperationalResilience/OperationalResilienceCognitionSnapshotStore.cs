using Tannous.Pos.Application.OperationalResilience;
using Tannous.Pos.Application.OperationalCognition;
using Tannous.Pos.Infrastructure.Services.OperationalCognition;

namespace Tannous.Pos.Infrastructure.Services.OperationalResilience;

/// <summary>Process-local FIFO resilience cognition snapshot retention (max 8; not persisted).</summary>
public sealed class OperationalResilienceCognitionSnapshotStore : IOperationalResilienceCognitionSnapshotStore
{
    private readonly BoundedFifoSnapshotStore<OperationalResilienceCognitionSnapshot> _store = new(
        OperationalCognitionSnapshotLimits.MaxStoredSnapshots);

    public int MaxSnapshots => _store.MaxSnapshots;

    public void Append(OperationalResilienceCognitionSnapshot snapshot) => _store.Append(snapshot);

    public IReadOnlyList<OperationalResilienceCognitionSnapshot> GetSnapshots() => _store.GetSnapshots();

    public void Clear() => _store.Clear();
}
