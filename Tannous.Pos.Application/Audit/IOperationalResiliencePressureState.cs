namespace Tannous.Pos.Application.Audit;

/// <summary>In-process operational pressure signals (informational; per deployment instance).</summary>
public interface IOperationalResiliencePressureState
{
    void NoteQueryPressure(bool dateRangeClamped, bool pageSizeClamped);
    void NoteForensicExportTruncation(bool truncated);
    bool QueryDateRangeClamped { get; }
    bool QueryPageSizeClamped { get; }
    bool ForensicExportTruncated { get; }
}
