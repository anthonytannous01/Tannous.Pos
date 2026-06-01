using MediatR;
using Tannous.Pos.Application.DTOs.Sync;

namespace Tannous.Pos.Application.Sync.Queries.GetSyncPullData;

public class GetSyncPullDataQueryHandler : IRequestHandler<GetSyncPullDataQuery, PullResponseDto>
{
    private readonly ISyncPullService _syncPullService;

    public GetSyncPullDataQueryHandler(ISyncPullService syncPullService)
    {
        _syncPullService = syncPullService;
    }

    public async Task<PullResponseDto> Handle(
        GetSyncPullDataQuery query, CancellationToken cancellationToken)
    {
        return await _syncPullService.PullAsync(
            query.SinceDate,
            query.Limit,
            query.Offset,
            cancellationToken);
    }
}
