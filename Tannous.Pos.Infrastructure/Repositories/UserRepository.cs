using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(PosDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        var normalized = username.ToUpperInvariant();
        return await _dbSet.FirstOrDefaultAsync(u => u.NormalizedUsername == normalized);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;
            
        var normalized = email.ToUpperInvariant();
        return await _dbSet.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized);
    }

    public async Task<User?> GetByNormalizedUsernameAsync(string normalizedUsername)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.NormalizedUsername == normalizedUsername);
    }

    public async Task<User?> GetByNormalizedEmailAsync(string normalizedEmail)
    {
        if (string.IsNullOrWhiteSpace(normalizedEmail))
            return null;
            
        return await _dbSet.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
    }

    public async Task<bool> UsernameExistsAsync(string normalizedUsername, Guid? excludeUserId = null)
    {
        var query = _dbSet.Where(u => u.NormalizedUsername == normalizedUsername);
        
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }
        
        return await query.AnyAsync();
    }

    public async Task<bool> EmailExistsAsync(string normalizedEmail, Guid? excludeUserId = null)
    {
        if (string.IsNullOrWhiteSpace(normalizedEmail))
            return false;
            
        var query = _dbSet.Where(u => u.NormalizedEmail == normalizedEmail);
        
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }
        
        return await query.AnyAsync();
    }

    public async Task<IEnumerable<User>> SearchAsync(string? searchTerm, int skip, int take)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(search) ||
                u.Email.ToLower().Contains(search) ||
                u.FirstName.ToLower().Contains(search) ||
                u.LastName.ToLower().Contains(search));
        }

        return await query
            .OrderBy(u => u.Username)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountAsync(string? searchTerm)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(search) ||
                u.Email.ToLower().Contains(search) ||
                u.FirstName.ToLower().Contains(search) ||
                u.LastName.ToLower().Contains(search));
        }

        return await query.CountAsync();
    }
}


