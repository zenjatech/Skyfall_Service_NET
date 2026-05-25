using Skyfall.Domain.Entities;

namespace Skyfall.Infrastructure.Repositories;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByOrderIdAsync(Guid orderId, Guid tenantId, CancellationToken ct);
    Task<Invoice?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task<string> GenerateInvoiceNumberAsync(Guid tenantId, CancellationToken ct);
    Task AddAsync(Invoice invoice, CancellationToken ct);
    Task UpdateAsync(Invoice invoice, CancellationToken ct);
}
