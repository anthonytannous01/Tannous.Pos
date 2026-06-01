using MediatR;
using Tannous.Pos.Application.DTOs.Sync;

namespace Tannous.Pos.Application.Sync.Queries.GetSyncPullData;

public class GetSyncPullDataQuery : IRequest<PullResponseDto>
{
    public DateTime SinceDate { get; set; }
    public int      Limit     { get; set; }
    public int      Offset    { get; set; }
}
