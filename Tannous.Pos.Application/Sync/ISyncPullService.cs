using Tannous.Pos.Application.DTOs.Sync;

namespace Tannous.Pos.Application.Sync;

public interface ISyncPullService
{
    Task<PullResponseDto> PullAsync(
        DateTime sinceDate,
        int      limit,
        int      offset,
        CancellationToken cancellationToken);
}
