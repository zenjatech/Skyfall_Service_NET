using Microsoft.EntityFrameworkCore;
using Skyfall.Domain.Entities;
using Skyfall.Infrastructure.Data;

namespace Skyfall.Infrastructure.Repositories;

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _db;

    public InvoiceRepository(AppDbContext db) => _db = db;

    public async Task<Invoice?> GetByOrderIdAsync(Guid orderId, Guid tenantId, CancellationToken ct) =>
        await _db.Invoices.Include(i => i.Order).Include(i => i.BilledByStaff)
            .FirstOrDefaultAsync(i => i.OrderId == orderId && i.TenantId == tenantId, ct);

    public async Task<Invoice?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct) =>
        await _db.Invoices.Include(i => i.Order).Include(i => i.BilledByStaff)
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId, ct);

    public async Task<string> GenerateInvoiceNumberAsync(Guid tenantId, CancellationToken ct)
    {
        var today = DateTime.UtcNow;
        var prefix = $"INV-{today:yyyyMM}";
        var count = await _db.Invoices.Where(i => i.TenantId == tenantId && i.InvoiceNumber.StartsWith(prefix)).CountAsync(ct);
        return $"{prefix}-{(count + 1):D4}";
    }

    public async Task AddAsync(Invoice invoice, CancellationToken ct)
    {
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Invoice invoice, CancellationToken ct)
    {
        _db.Invoices.Update(invoice);
        await _db.SaveChangesAsync(ct);
    }
}
