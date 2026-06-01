using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;
using BCrypt.Net;

namespace Tannous.Pos.Infrastructure.Services;

public class JwtAuthService : IAuthService
{
    private readonly PosDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;

    public JwtAuthService(
        PosDbContext context, 
        IConfiguration configuration,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _configuration = configuration;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> GenerateJwtTokenAsync(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["JWT_KEY"] ?? 
                                        _configuration["Jwt:Key"] ?? 
                                        throw new InvalidOperationException("JWT signing key not configured"));
        
        var expiresInMinutes = int.Parse(_configuration["JWT_ACCESS_TOKEN_EXPIRY_MINUTES"] ?? 
                                        _configuration["Jwt:AccessTokenExpiryInMinutes"] ?? "15");
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _configuration["JWT_ISSUER"] ?? _configuration["Jwt:Issuer"] ?? "TannousPOS",
            Audience = _configuration["JWT_AUDIENCE"] ?? _configuration["Jwt:Audience"] ?? "TannousPOS"
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<string> GenerateRefreshTokenAsync(Guid userId)
    {
        var randomBytes = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        var token = Convert.ToBase64String(randomBytes);

        var refreshTokenDays = int.Parse(_configuration["JWT_REFRESH_TOKEN_EXPIRY_DAYS"] ?? 
                                         _configuration["Jwt:RefreshTokenExpiryInDays"] ?? "30");

        var refreshToken = new RefreshToken
        {
            Token = token,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays),
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        return token;
    }

    public async Task<(User User, string AccessToken, string RefreshToken)?> RefreshTokenAsync(string refreshToken)
    {
        var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        
        if (token == null || !token.IsActive)
            return null;

        // Revoke the old token
        await _refreshTokenRepository.RevokeTokenAsync(refreshToken);

        // Generate new tokens
        var accessToken = await GenerateJwtTokenAsync(token.User);
        var newRefreshToken = await GenerateRefreshTokenAsync(token.UserId);

        return (token.User, accessToken, newRefreshToken);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        await _refreshTokenRepository.RevokeTokenAsync(refreshToken);
    }

    public async Task<bool> ValidatePasswordAsync(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    public async Task<string> HashPasswordAsync(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        var normalizedUsername = username.ToUpperInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedUsername == normalizedUsername && u.IsActive);

        if (user == null)
            return null;

        if (await ValidatePasswordAsync(password, user.PasswordHash))
        {
            user.LastLoginDate = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return user;
        }

        return null;
    }
}
