using Microsoft.EntityFrameworkCore;
using Skyfall.Domain.Entities;
using Skyfall.Infrastructure.Data;

namespace Skyfall.Infrastructure.Repositories;

public sealed class MenuRepository : IMenuRepository
{
    private readonly AppDbContext _db;

    public MenuRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(Guid tenantId, CancellationToken ct) =>
        await _db.Categories.AsNoTracking().Where(c => c.TenantId == tenantId).OrderBy(c => c.DisplayOrder).ToListAsync(ct);

    public async Task<Category?> GetCategoryByIdAsync(Guid id, Guid tenantId, CancellationToken ct) =>
        await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);

    public async Task AddCategoryAsync(Category category, CancellationToken ct)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateCategoryAsync(Category category, CancellationToken ct)
    {
        _db.Categories.Update(category);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteCategoryAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);
        if (entity is null) return false;
        _db.Categories.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<MenuItem>> GetMenuItemsAsync(Guid tenantId, Guid? categoryId, CancellationToken ct)
    {
        var query = _db.MenuItems.AsNoTracking()
            .Include(m => m.Variants)
            .Include(m => m.Addons)
            .Include(m => m.Category)
            .Where(m => m.TenantId == tenantId);
        if (categoryId.HasValue) query = query.Where(m => m.CategoryId == categoryId.Value);
        return await query.OrderBy(m => m.Name).ToListAsync(ct);
    }

    public async Task<MenuItem?> GetMenuItemByIdAsync(Guid id, Guid tenantId, CancellationToken ct) =>
        await _db.MenuItems
            .Include(m => m.Variants)
            .Include(m => m.Addons)
            .Include(m => m.Category)
            .FirstOrDefaultAsync(m => m.Id == id && m.TenantId == tenantId, ct);

    public async Task AddMenuItemAsync(MenuItem item, CancellationToken ct)
    {
        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateMenuItemAsync(MenuItem item, CancellationToken ct)
    {
        _db.MenuItems.Update(item);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteMenuItemAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        var entity = await _db.MenuItems.FirstOrDefaultAsync(m => m.Id == id && m.TenantId == tenantId, ct);
        if (entity is null) return false;
        _db.MenuItems.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task AddVariantAsync(ItemVariant variant, CancellationToken ct)
    {
        _db.ItemVariants.Add(variant);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ItemVariant?> GetVariantByIdAsync(Guid id, Guid tenantId, CancellationToken ct) =>
        await _db.ItemVariants.FirstOrDefaultAsync(v => v.Id == id && v.TenantId == tenantId, ct);

    public async Task UpdateVariantAsync(ItemVariant variant, CancellationToken ct)
    {
        _db.ItemVariants.Update(variant);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteVariantAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        var entity = await _db.ItemVariants.FirstOrDefaultAsync(v => v.Id == id && v.TenantId == tenantId, ct);
        if (entity is null) return false;
        _db.ItemVariants.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task AddAddonAsync(ItemAddon addon, CancellationToken ct)
    {
        _db.ItemAddons.Add(addon);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ItemAddon?> GetAddonByIdAsync(Guid id, Guid tenantId, CancellationToken ct) =>
        await _db.ItemAddons.FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId, ct);

    public async Task UpdateAddonAsync(ItemAddon addon, CancellationToken ct)
    {
        _db.ItemAddons.Update(addon);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAddonAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        var entity = await _db.ItemAddons.FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId, ct);
        if (entity is null) return false;
        _db.ItemAddons.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
