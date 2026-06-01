namespace Tannous.Pos.Application.OperationalDigest;

/// <summary>Operator operational condensation and executive digest (advisory, process-local).</summary>
public interface IOperationalDigestService
{
    Task<OperationalDigestDto> GetOperationalDigestAsync(CancellationToken cancellationToken = default);

    Task<OperationalExecutiveDigestDto> GetExecutiveDigestAsync(CancellationToken cancellationToken = default);

    Task<OperationalDigestSummaryDto> GetDigestSummaryAsync(CancellationToken cancellationToken = default);
}
