using Microsoft.EntityFrameworkCore;
using Skyfall.Domain.Entities;
using Skyfall.Infrastructure.Data;

namespace Skyfall.Infrastructure.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;

    public OrderRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Order>> GetAllAsync(Guid tenantId, string? status, CancellationToken ct)
    {
        var query = _db.Orders.AsNoTracking()
            .Include(o => o.Table)
            .Include(o => o.Customer)
            .Include(o => o.PlacedByStaff)
            .Include(o => o.Items).ThenInclude(i => i.MenuItem)
            .Include(o => o.Items).ThenInclude(i => i.Variant)
            .Include(o => o.Payments)
            .Where(o => o.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);
        return await query.OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Order>> GetByTableAsync(Guid tableId, Guid tenantId, CancellationToken ct) =>
        await _db.Orders.AsNoTracking()
            .Include(o => o.Items).ThenInclude(i => i.MenuItem)
            .Include(o => o.Items).ThenInclude(i => i.Variant)
            .Where(o => o.TableId == tableId && o.TenantId == tenantId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public async Task<Order?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct) =>
        await _db.Orders
            .Include(o => o.Table)
            .Include(o => o.Customer)
            .Include(o => o.PlacedByStaff)
            .Include(o => o.Items).ThenInclude(i => i.MenuItem)
            .Include(o => o.Items).ThenInclude(i => i.Variant)
            .Include(o => o.KOTs)
            .Include(o => o.Invoice)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId, ct);

    public async Task<int> GetNextKotNumberAsync(Guid tenantId, CancellationToken ct)
    {
        var max = await _db.KOTs.Where(k => k.TenantId == tenantId).MaxAsync(k => (int?)k.KotNumber, ct);
        return (max ?? 0) + 1;
    }

    public async Task AddAsync(Order order, CancellationToken ct)
    {
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Order order, CancellationToken ct)
    {
        _db.Orders.Update(order);
        await _db.SaveChangesAsync(ct);
    }
}
