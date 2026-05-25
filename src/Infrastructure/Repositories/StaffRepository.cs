using Microsoft.EntityFrameworkCore;
using Skyfall.Domain.Entities;
using Skyfall.Infrastructure.Data;

namespace Skyfall.Infrastructure.Repositories;

public sealed class StaffRepository : IStaffRepository
{
    private readonly AppDbContext _db;

    public StaffRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Staff>> GetAllAsync(Guid tenantId, CancellationToken ct) =>
        await _db.Staff.AsNoTracking().Where(s => s.TenantId == tenantId).OrderBy(s => s.Name).ToListAsync(ct);

    public async Task<Staff?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct) =>
        await _db.Staff.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);

    public async Task<Staff?> GetByEmailAsync(string email, Guid tenantId, CancellationToken ct) =>
        await _db.Staff.FirstOrDefaultAsync(s => s.Email == email && s.TenantId == tenantId, ct);

    public async Task AddAsync(Staff staff, CancellationToken ct)
    {
        _db.Staff.Add(staff);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Staff staff, CancellationToken ct)
    {
        _db.Staff.Update(staff);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        var entity = await _db.Staff.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);
        if (entity is null) return false;
        _db.Staff.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
