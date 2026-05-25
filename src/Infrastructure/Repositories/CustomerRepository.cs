using Microsoft.EntityFrameworkCore;
using Skyfall.Domain.Entities;
using Skyfall.Infrastructure.Data;

namespace Skyfall.Infrastructure.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _db;

    public CustomerRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Customer>> GetAllAsync(Guid tenantId, CancellationToken ct) =>
        await _db.Customers.AsNoTracking().Where(c => c.TenantId == tenantId).OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<Customer?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct) =>
        await _db.Customers.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);

    public async Task<Customer?> GetByPhoneAsync(string phone, Guid tenantId, CancellationToken ct) =>
        await _db.Customers.FirstOrDefaultAsync(c => c.Phone == phone && c.TenantId == tenantId, ct);

    public async Task AddAsync(Customer customer, CancellationToken ct)
    {
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Customer customer, CancellationToken ct)
    {
        _db.Customers.Update(customer);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);
        if (entity is null) return false;
        _db.Customers.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
