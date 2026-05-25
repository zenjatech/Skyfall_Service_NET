using Skyfall.Domain.Entities;

namespace Skyfall.Infrastructure.Repositories;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllAsync(Guid tenantId, CancellationToken ct);
    Task<Customer?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task<Customer?> GetByPhoneAsync(string phone, Guid tenantId, CancellationToken ct);
    Task AddAsync(Customer customer, CancellationToken ct);
    Task UpdateAsync(Customer customer, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, Guid tenantId, CancellationToken ct);
}
