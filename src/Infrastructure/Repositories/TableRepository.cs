using Microsoft.EntityFrameworkCore;
using Skyfall.Domain.Entities;
using Skyfall.Infrastructure.Data;

namespace Skyfall.Infrastructure.Repositories;

public sealed class TableRepository : ITableRepository
{
    private readonly AppDbContext _db;

    public TableRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CafeTable>> GetAllAsync(Guid tenantId, CancellationToken ct) =>
        await _db.Tables.AsNoTracking().Where(t => t.TenantId == tenantId).OrderBy(t => t.TableNumber).ToListAsync(ct);

    public async Task<CafeTable?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct) =>
        await _db.Tables.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, ct);

    public async Task<bool> TableNumberExistsAsync(int tableNumber, Guid tenantId, Guid? excludeId, CancellationToken ct)
    {
        var query = _db.Tables.Where(t => t.TableNumber == tableNumber && t.TenantId == tenantId);
        if (excludeId.HasValue) query = query.Where(t => t.Id != excludeId.Value);
        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(CafeTable table, CancellationToken ct)
    {
        _db.Tables.Add(table);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(CafeTable table, CancellationToken ct)
    {
        _db.Tables.Update(table);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        var entity = await _db.Tables.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, ct);
        if (entity is null) return false;
        _db.Tables.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
