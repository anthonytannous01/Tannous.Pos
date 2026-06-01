using MediatR;
using Tannous.Pos.Application.DTOs.Users;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Enums;
using Tannous.Pos.Domain.Interfaces;

namespace Tannous.Pos.Application.Users.Commands.CreateUser;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IAuthService authService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _authService = authService;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedUsername = request.User.Username.ToUpperInvariant();
        var normalizedEmail = string.IsNullOrWhiteSpace(request.User.Email) 
            ? null 
            : request.User.Email.ToUpperInvariant();

        // Check for duplicate username
        if (await _userRepository.UsernameExistsAsync(normalizedUsername))
        {
            throw new InvalidOperationException($"Username '{request.User.Username}' is already taken");
        }

        // Check for duplicate email if provided
        if (!string.IsNullOrWhiteSpace(normalizedEmail) && 
            await _userRepository.EmailExistsAsync(normalizedEmail))
        {
            throw new InvalidOperationException($"Email '{request.User.Email}' is already registered");
        }

        // Validate role
        if (!Enum.TryParse<Role>(request.User.Role, ignoreCase: true, out var role))
        {
            throw new ArgumentException($"Invalid role: {request.User.Role}");
        }

        // Hash password
        var passwordHash = await _authService.HashPasswordAsync(request.User.Password);

        // Create user
        var user = new User
        {
            Username = request.User.Username,
            NormalizedUsername = normalizedUsername,
            Email = request.User.Email,
            NormalizedEmail = normalizedEmail,
            PasswordHash = passwordHash,
            FirstName = request.User.FirstName,
            LastName = request.User.LastName,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            LastLoginDate = user.LastLoginDate,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}

