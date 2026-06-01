using Tannous.Pos.Application.Audit;

namespace Tannous.Pos.Infrastructure.Services;

public sealed class OperationalResiliencePressureState : IOperationalResiliencePressureState, IOperationalResiliencePressureGovernanceReset
{
    private volatile bool _queryDateRangeClamped;
    private volatile bool _queryPageSizeClamped;
    private volatile bool _forensicExportTruncated;

    public bool QueryDateRangeClamped => _queryDateRangeClamped;
    public bool QueryPageSizeClamped => _queryPageSizeClamped;
    public bool ForensicExportTruncated => _forensicExportTruncated;

    public void NoteQueryPressure(bool dateRangeClamped, bool pageSizeClamped)
    {
        if (dateRangeClamped)
            _queryDateRangeClamped = true;
        if (pageSizeClamped)
            _queryPageSizeClamped = true;
    }

    public void NoteForensicExportTruncation(bool truncated)
    {
        if (truncated)
            _forensicExportTruncated = true;
    }

    public void ResetGovernancePressureFlags()
    {
        _queryDateRangeClamped = false;
        _queryPageSizeClamped = false;
        _forensicExportTruncated = false;
    }
}
