using MediatR;
using Tannous.Pos.Application.DTOs.Common;
using Tannous.Pos.Application.DTOs.Users;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Users.Queries.ListUsers;

public class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, PaginatedResponseDto<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public ListUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PaginatedResponseDto<UserDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var skip = (request.Page - 1) * request.PageSize;
        var take = Math.Min(request.PageSize, 100); // Max 100 items per page

        var users = await _userRepository.SearchAsync(request.Search, skip, take);
        var total = await _userRepository.CountAsync(request.Search);

        var userDtos = users.Select(u => new UserDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Role = u.Role.ToString(),
            IsActive = u.IsActive,
            LastLoginDate = u.LastLoginDate,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        }).ToList();

        return new PaginatedResponseDto<UserDto>
        {
            Items = userDtos,
            Total = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}


