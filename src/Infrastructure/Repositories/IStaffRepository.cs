using Skyfall.Domain.Entities;

namespace Skyfall.Infrastructure.Repositories;

public interface IStaffRepository
{
    Task<IReadOnlyList<Staff>> GetAllAsync(Guid tenantId, CancellationToken ct);
    Task<Staff?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task<Staff?> GetByEmailAsync(string email, Guid tenantId, CancellationToken ct);
    Task AddAsync(Staff staff, CancellationToken ct);
    Task UpdateAsync(Staff staff, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, Guid tenantId, CancellationToken ct);
}
