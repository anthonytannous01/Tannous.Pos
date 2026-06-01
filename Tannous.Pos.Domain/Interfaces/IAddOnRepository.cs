using Tannous.Pos.Domain.Entities;

namespace Tannous.Pos.Domain.Interfaces;

public interface IAddOnRepository : IRepository<AddOn>
{
    Task<IEnumerable<AddOn>> GetActiveAddOnsAsync();
    Task<AddOn?> GetByNameAsync(string name);
}
