using MediatR;
using Microsoft.Extensions.Configuration;
using Tannous.Pos.Application.DTOs.Auth;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResponseDto>
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public RefreshTokenCommandHandler(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    public async Task<LoginResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        
        if (result == null)
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        var (user, accessToken, refreshToken) = result.Value;
        var expiresInMinutes = int.Parse(_configuration["JWT_ACCESS_TOKEN_EXPIRY_MINUTES"] ?? 
                                       _configuration["Jwt:AccessTokenExpiryInMinutes"] ?? "15");

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresInMinutes * 60,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.ToString()
            }
        };
    }
}

