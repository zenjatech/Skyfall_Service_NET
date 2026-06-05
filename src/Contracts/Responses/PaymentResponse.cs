namespace Skyfall.Contracts.Responses;

public sealed class PaymentResponse
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Mode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Tip { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public DateTime CreatedAt { get; set; }
}
