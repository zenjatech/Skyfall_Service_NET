namespace Skyfall.Domain.Entities;

public sealed class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public string Mode { get; set; } = "cash";
    public decimal Amount { get; set; }
    public decimal Tip { get; set; } = 0;
    public string Status { get; set; } = PaymentStatus.Pending;
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Order? Order { get; set; }
}

public static class PaymentStatus
{
    public const string Pending = "pending";
    public const string Success = "success";
    public const string Failed = "failed";
    public const string Refunded = "refunded";
}

public static class PaymentMode
{
    public const string Cash = "cash";
    public const string Upi = "upi";
    public const string DebitCard = "debit_card";
    public const string CreditCard = "credit_card";
}
