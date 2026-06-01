using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByNormalizedUsernameAsync(string normalizedUsername);
    Task<User?> GetByNormalizedEmailAsync(string normalizedEmail);
    Task<bool> UsernameExistsAsync(string normalizedUsername, Guid? excludeUserId = null);
    Task<bool> EmailExistsAsync(string normalizedEmail, Guid? excludeUserId = null);
    Task<IEnumerable<User>> SearchAsync(string? searchTerm, int skip, int take);
    Task<int> CountAsync(string? searchTerm);
}


