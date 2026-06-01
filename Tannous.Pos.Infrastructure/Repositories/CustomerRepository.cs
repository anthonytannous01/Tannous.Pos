using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(PosDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Customer>> GetActiveCustomersAsync()
    {
        return await _dbSet
            .Where(c => c.IsActive)
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .ToListAsync();
    }

    public async Task<Customer?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Email == email);
    }

    public async Task<Customer?> GetByPhoneAsync(string phone)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Phone == phone);
    }

    public async Task<IEnumerable<Customer>> SearchByNameAsync(string searchTerm)
    {
        return await _dbSet
            .Where(c => c.IsActive && 
                       (c.FirstName.Contains(searchTerm) || 
                        c.LastName.Contains(searchTerm) ||
                        (c.FirstName + " " + c.LastName).Contains(searchTerm)))
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .ToListAsync();
    }

    public async Task<(IReadOnlyList<Customer> Items, int Total)> SearchPagedAsync(
        string? searchText, string? sort, string? dir,
        int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(searchText))
        {
            var searchTerm = searchText.ToLower();
            query = query.Where(c =>
                c.FirstName.ToLower().Contains(searchTerm) ||
                c.LastName.ToLower().Contains(searchTerm) ||
                (c.Phone != null && c.Phone.ToLower().Contains(searchTerm)) ||
                (c.Email != null && c.Email.ToLower().Contains(searchTerm)));
        }

        var total = await query.CountAsync(cancellationToken);

        query = sort?.ToLower() switch
        {
            "name"      => dir?.ToLower() == "desc"
                           ? query.OrderByDescending(c => c.LastName).ThenByDescending(c => c.FirstName)
                           : query.OrderBy(c => c.LastName).ThenBy(c => c.FirstName),
            "email"     => dir?.ToLower() == "desc"
                           ? query.OrderByDescending(c => c.Email)
                           : query.OrderBy(c => c.Email),
            "phone"     => dir?.ToLower() == "desc"
                           ? query.OrderByDescending(c => c.Phone)
                           : query.OrderBy(c => c.Phone),
            "createdat" => dir?.ToLower() == "desc"
                           ? query.OrderByDescending(c => c.CreatedAt)
                           : query.OrderBy(c => c.CreatedAt),
            _           => query.OrderByDescending(c => c.UpdatedAt)
        };

        var items = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
