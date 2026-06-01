namespace Tannous.Pos.Application.Audit;

public interface IOperationalResilienceDiagnosticsService
{
    Task<OperationalResilienceSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<OperationalDegradedModesDto> GetDegradedModesAsync(CancellationToken cancellationToken = default);
    Task<OperationalPressureIndicatorsDto> GetPressureIndicatorsAsync(CancellationToken cancellationToken = default);
    Task<OperationalReplayRiskSummaryDto> GetReplayRiskSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>Records transient query pressure signals for the current process (in-memory, informational).</summary>
    void NoteQueryPressure(bool dateRangeClamped, bool pageSizeClamped);

    /// <summary>Records forensic export truncation for pressure classification (in-memory, informational).</summary>
    void NoteForensicExportTruncation(bool truncated);
}
