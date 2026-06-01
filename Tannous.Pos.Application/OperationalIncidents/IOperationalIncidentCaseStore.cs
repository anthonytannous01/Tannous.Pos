namespace Tannous.Pos.Application.OperationalIncidents;

/// <summary>Process-local bounded incident snapshot retention (FIFO; not persisted).</summary>
public interface IOperationalIncidentCaseStore
{
    int MaxSnapshots { get; }

    void Append(OperationalIncidentCaseSnapshot snapshot);

    IReadOnlyList<OperationalIncidentCaseSnapshot> GetSnapshots();

    void Clear();
}
