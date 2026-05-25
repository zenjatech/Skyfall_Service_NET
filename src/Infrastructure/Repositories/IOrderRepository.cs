using Skyfall.Domain.Entities;

namespace Skyfall.Infrastructure.Repositories;

public interface IOrderRepository
{
    Task<IReadOnlyList<Order>> GetAllAsync(Guid tenantId, string? status, CancellationToken ct);
    Task<IReadOnlyList<Order>> GetByTableAsync(Guid tableId, Guid tenantId, CancellationToken ct);
    Task<Order?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task<int> GetNextKotNumberAsync(Guid tenantId, CancellationToken ct);
    Task AddAsync(Order order, CancellationToken ct);
    Task UpdateAsync(Order order, CancellationToken ct);
}
