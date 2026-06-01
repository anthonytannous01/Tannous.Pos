using MediatR;

namespace Tannous.Pos.Application.Auth.Commands.Logout;

public class LogoutCommand : IRequest<Unit>
{
    public string RefreshToken { get; set; } = string.Empty;
}


