using MediatR;
using Tannous.Pos.Application.DTOs.Users;

namespace Tannous.Pos.Application.Users.Commands.CreateUser;

public class CreateUserCommand : IRequest<UserDto>
{
    public CreateUserDto User { get; set; } = new();
}


