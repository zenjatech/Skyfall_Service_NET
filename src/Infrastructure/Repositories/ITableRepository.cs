using Skyfall.Domain.Entities;

namespace Skyfall.Infrastructure.Repositories;

public interface ITableRepository
{
    Task<IReadOnlyList<CafeTable>> GetAllAsync(Guid tenantId, CancellationToken ct);
    Task<CafeTable?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task<bool> TableNumberExistsAsync(int tableNumber, Guid tenantId, Guid? excludeId, CancellationToken ct);
    Task AddAsync(CafeTable table, CancellationToken ct);
    Task UpdateAsync(CafeTable table, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, Guid tenantId, CancellationToken ct);
}
