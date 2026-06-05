namespace Skyfall.Contracts.Responses;

public sealed class OrderResponse
{
    public Guid Id { get; set; }
    public Guid TableId { get; set; }
    public int TableNumber { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public Guid? PlacedByStaffId { get; set; }
    public string? PlacedByStaffName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? SpecialInstructions { get; set; }
    public List<OrderItemResponse> Items { get; set; } = [];
    public bool IsPaid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class OrderItemResponse
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public Guid? VariantId { get; set; }
    public string? VariantName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? AddonsJson { get; set; }
    public string? SpecialInstructions { get; set; }
    public string ItemStatus { get; set; } = string.Empty;
}
