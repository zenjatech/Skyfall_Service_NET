namespace Skyfall.Contracts.Responses;

public sealed class MenuItemResponse
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsVeg { get; set; }
    public int PrepTimeMinutes { get; set; }
    public List<VariantResponse> Variants { get; set; } = [];
    public List<AddonResponse> Addons { get; set; } = [];
}

public sealed class VariantResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceModifier { get; set; }
    public bool IsAvailable { get; set; }
}

public sealed class AddonResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal ExtraPrice { get; set; }
    public bool IsAvailable { get; set; }
}
