using MediatR;
using Tannous.Pos.Application.DTOs.Admin;

namespace Tannous.Pos.Application.Admin.Commands.PurgeSoftDeletedRecords;

public class PurgeSoftDeletedRecordsCommand : IRequest<PurgeSoftDeletedResultDto>
{
    /// <summary>Records older than this many days are purged. Matches the controller default of 30.</summary>
    public int Days { get; set; } = 30;
}
