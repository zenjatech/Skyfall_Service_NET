using Microsoft.EntityFrameworkCore;
using Skyfall.Domain.Entities;
using Skyfall.Infrastructure.Data;

namespace Skyfall.Infrastructure.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _db;

    public PaymentRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Payment>> GetByOrderAsync(Guid orderId, Guid tenantId, CancellationToken ct) =>
        await _db.Payments.AsNoTracking().Where(p => p.OrderId == orderId && p.TenantId == tenantId).ToListAsync(ct);

    public async Task<Payment?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct) =>
        await _db.Payments.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);

    public async Task AddAsync(Payment payment, CancellationToken ct)
    {
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Payment payment, CancellationToken ct)
    {
        _db.Payments.Update(payment);
        await _db.SaveChangesAsync(ct);
    }
}
