namespace Skyfall.Domain.Entities;

public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid TableId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? PlacedByStaffId { get; set; }
    public string Status { get; set; } = OrderStatus.Pending;
    public string OrderType { get; set; } = "dine_in";
    public decimal Subtotal { get; set; } = 0;
    public decimal TaxAmount { get; set; } = 0;
    public decimal DiscountAmount { get; set; } = 0;
    public decimal TotalAmount { get; set; } = 0;
    public string? SpecialInstructions { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public CafeTable? Table { get; set; }
    public Customer? Customer { get; set; }
    public Staff? PlacedByStaff { get; set; }
    public ICollection<OrderItem> Items { get; set; } = [];
    public ICollection<KOT> KOTs { get; set; } = [];
    public Invoice? Invoice { get; set; }
    public ICollection<Payment> Payments { get; set; } = [];
}

public static class OrderStatus
{
    public const string Pending = "pending";
    public const string Confirmed = "confirmed";
    public const string Preparing = "preparing";
    public const string Ready = "ready";
    public const string Served = "served";
    public const string Cancelled = "cancelled";

    public static bool CanTransitionTo(string current, string next) => (current, next) switch
    {
        _ when current == next => true,
        (Pending, Confirmed) => true,
        (Pending, Preparing) => true,
        (Pending, Cancelled) => true,
        (Confirmed, Preparing) => true,
        (Confirmed, Ready) => true,
        (Confirmed, Served) => true,
        (Confirmed, Cancelled) => true,
        (Preparing, Ready) => true,
        (Preparing, Served) => true,
        (Preparing, Cancelled) => true,
        (Ready, Served) => true,
        (Ready, Cancelled) => true,
        _ => false
    };
}
