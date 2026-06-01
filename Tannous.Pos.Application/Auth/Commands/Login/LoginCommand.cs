using MediatR;
using Tannous.Pos.Application.DTOs.Auth;

namespace Tannous.Pos.Application.Auth.Commands.Login;

public class LoginCommand : IRequest<LoginResponseDto>
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}


