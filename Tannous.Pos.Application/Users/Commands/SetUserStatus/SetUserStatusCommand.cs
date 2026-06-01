using MediatR;
using Tannous.Pos.Application.DTOs.Users;

namespace Tannous.Pos.Application.Users.Commands.SetUserStatus;

public class SetUserStatusCommand : IRequest<UserDto>
{
    public Guid UserId { get; set; }
    public bool IsActive { get; set; }
}


