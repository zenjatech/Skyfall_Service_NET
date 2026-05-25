using Skyfall.Domain.Entities;

namespace Skyfall.Infrastructure.Repositories;

public interface IPaymentRepository
{
    Task<IReadOnlyList<Payment>> GetByOrderAsync(Guid orderId, Guid tenantId, CancellationToken ct);
    Task<Payment?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task AddAsync(Payment payment, CancellationToken ct);
    Task UpdateAsync(Payment payment, CancellationToken ct);
}
