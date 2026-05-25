namespace Skyfall.Domain.Entities;

public sealed class ItemVariant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceModifier { get; set; } = 0;
    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public MenuItem? Item { get; set; }
}
