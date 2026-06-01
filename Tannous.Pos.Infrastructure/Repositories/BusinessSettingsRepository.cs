using Microsoft.EntityFrameworkCore;
using Tannous.Pos.Domain.Entities;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure.Data;

namespace Tannous.Pos.Infrastructure.Repositories;

public class BusinessSettingsRepository : IBusinessSettingsRepository
{
    private readonly PosDbContext _db;

    public BusinessSettingsRepository(PosDbContext db)
    {
        _db = db;
    }

    public Task<BusinessSettings?> GetAsync(CancellationToken cancellationToken = default) =>
        _db.BusinessSettings.FirstOrDefaultAsync(cancellationToken);

    public async Task CreateAsync(BusinessSettings settings, CancellationToken cancellationToken = default)
    {
        _db.BusinessSettings.Add(settings);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
