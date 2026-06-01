using MediatR;
using Tannous.Pos.Application.DTOs.Common;
using Tannous.Pos.Application.DTOs.Users;

namespace Tannous.Pos.Application.Users.Queries.ListUsers;

public class ListUsersQuery : IRequest<PaginatedResponseDto<UserDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
}


