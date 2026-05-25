using Skyfall.Domain.Entities;

namespace Skyfall.Infrastructure.Repositories;

public interface IKOTRepository
{
    Task<IReadOnlyList<KOT>> GetAllAsync(Guid tenantId, string? status, CancellationToken ct);
    Task<KOT?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task UpdateAsync(KOT kot, CancellationToken ct);
}
