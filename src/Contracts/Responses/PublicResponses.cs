namespace Skyfall.Contracts.Responses;

public sealed class PublicMenuResponse
{
    public List<CategoryResponse> Categories { get; set; } = [];
    public List<MenuItemResponse> Items { get; set; } = [];
}

public sealed class CustomerIdentifyResponse
{
    public CustomerResponse Customer { get; set; } = new();
    public bool IsNew { get; set; }
}

public sealed class PublicBillingResponse
{
    public Guid OrderId { get; set; }
    public string? InvoiceNumber { get; set; }
    public List<OrderItemResponse> Items { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public string PaymentStatus { get; set; } = "unpaid";
    public List<PaymentResponse> Payments { get; set; } = [];
}
