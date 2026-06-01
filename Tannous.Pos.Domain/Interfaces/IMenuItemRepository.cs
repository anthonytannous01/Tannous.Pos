using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Domain.Interfaces;

public interface IMenuItemRepository : IRepository<MenuItem>
{
    Task<IEnumerable<MenuItem>> GetActiveMenuItemsAsync();
    Task<IEnumerable<MenuItem>> GetByCategoryAsync(Guid categoryId);
    Task<MenuItem?> GetByNameAsync(string name);
    Task<MenuItem?> GetByIdWithCategoryAsync(Guid id);
}
