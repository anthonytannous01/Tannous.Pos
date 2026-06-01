using MediatR;
using Tannous.Pos.Application.DTOs.Users;

namespace Tannous.Pos.Application.Users.Queries.GetUserById;

public class GetUserByIdQuery : IRequest<UserDto?>
{
    public Guid UserId { get; set; }
}


