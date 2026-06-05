namespace Skyfall.Contracts.Requests;

public sealed class OrderCreateRequest
{
    public Guid TableId { get; set; }
    public Guid? CustomerId { get; set; }
    public string OrderType { get; set; } = "dine_in";
    public string? SpecialInstructions { get; set; }
    public List<OrderItemRequest> Items { get; set; } = [];
    public decimal? TaxRate { get; set; }
}

public sealed class OrderItemRequest
{
    public Guid MenuItemId { get; set; }
    public Guid? VariantId { get; set; }
    public int Quantity { get; set; } = 1;
    public List<Guid> AddonIds { get; set; } = [];
    public string? SpecialInstructions { get; set; }
}
