using Tannous.Pos.Application.OperationalExperienceGraph;
using Tannous.Pos.Application.OperationalCognition;
using Tannous.Pos.Infrastructure.Services.OperationalCognition;

namespace Tannous.Pos.Infrastructure.Services.OperationalExperienceGraph;

/// <summary>Process-local FIFO experience graph snapshot retention (max 8; not persisted).</summary>
public sealed class OperationalExperienceSnapshotStore : IOperationalExperienceSnapshotStore
{
    private readonly BoundedFifoSnapshotStore<OperationalExperienceSnapshot> _store = new(
        OperationalCognitionSnapshotLimits.MaxStoredSnapshots);

    public int MaxSnapshots => _store.MaxSnapshots;

    public void Append(OperationalExperienceSnapshot snapshot) => _store.Append(snapshot);

    public IReadOnlyList<OperationalExperienceSnapshot> GetSnapshots() => _store.GetSnapshots();

    public void Clear() => _store.Clear();
}
