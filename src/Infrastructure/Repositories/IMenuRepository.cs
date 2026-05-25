using Skyfall.Domain.Entities;

namespace Skyfall.Infrastructure.Repositories;

public interface IMenuRepository
{
    Task<IReadOnlyList<Category>> GetCategoriesAsync(Guid tenantId, CancellationToken ct);
    Task<Category?> GetCategoryByIdAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task AddCategoryAsync(Category category, CancellationToken ct);
    Task UpdateCategoryAsync(Category category, CancellationToken ct);
    Task<bool> DeleteCategoryAsync(Guid id, Guid tenantId, CancellationToken ct);

    Task<IReadOnlyList<MenuItem>> GetMenuItemsAsync(Guid tenantId, Guid? categoryId, CancellationToken ct);
    Task<MenuItem?> GetMenuItemByIdAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task AddMenuItemAsync(MenuItem item, CancellationToken ct);
    Task UpdateMenuItemAsync(MenuItem item, CancellationToken ct);
    Task<bool> DeleteMenuItemAsync(Guid id, Guid tenantId, CancellationToken ct);

    Task AddVariantAsync(ItemVariant variant, CancellationToken ct);
    Task<ItemVariant?> GetVariantByIdAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task UpdateVariantAsync(ItemVariant variant, CancellationToken ct);
    Task<bool> DeleteVariantAsync(Guid id, Guid tenantId, CancellationToken ct);

    Task AddAddonAsync(ItemAddon addon, CancellationToken ct);
    Task<ItemAddon?> GetAddonByIdAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task UpdateAddonAsync(ItemAddon addon, CancellationToken ct);
    Task<bool> DeleteAddonAsync(Guid id, Guid tenantId, CancellationToken ct);
}
