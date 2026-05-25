namespace Skyfall.Contracts.Requests;

public sealed class PaymentCreateRequest
{
    public Guid OrderId { get; set; }
    public string Mode { get; set; } = "cash";
    public decimal Amount { get; set; }
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
}
