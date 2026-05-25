using Microsoft.EntityFrameworkCore;
using Skyfall.Domain.Entities;
using Skyfall.Infrastructure.Data;

namespace Skyfall.Infrastructure.Repositories;

public sealed class KOTRepository : IKOTRepository
{
    private readonly AppDbContext _db;

    public KOTRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<KOT>> GetAllAsync(Guid tenantId, string? status, CancellationToken ct)
    {
        var query = _db.KOTs.AsNoTracking()
            .Include(k => k.Order).ThenInclude(o => o!.Table)
            .Where(k => k.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(k => k.Status == status);
        return await query.OrderByDescending(k => k.CreatedAt).ToListAsync(ct);
    }

    public async Task<KOT?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct) =>
        await _db.KOTs.Include(k => k.Order).FirstOrDefaultAsync(k => k.Id == id && k.TenantId == tenantId, ct);

    public async Task UpdateAsync(KOT kot, CancellationToken ct)
    {
        _db.KOTs.Update(kot);
        await _db.SaveChangesAsync(ct);
    }
}
