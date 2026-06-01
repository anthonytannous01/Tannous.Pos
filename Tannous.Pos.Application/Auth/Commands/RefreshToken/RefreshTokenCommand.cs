using MediatR;
using Tannous.Pos.Application.DTOs.Auth;

namespace Tannous.Pos.Application.Auth.Commands.RefreshToken;

public class RefreshTokenCommand : IRequest<LoginResponseDto>
{
    public string RefreshToken { get; set; } = string.Empty;
}


