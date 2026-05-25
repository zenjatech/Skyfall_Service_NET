namespace Skyfall.Contracts.Responses;

public sealed class InvoiceResponse
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? PdfUrl { get; set; }
    public bool WhatsappSent { get; set; }
    public bool SmsSent { get; set; }
    public Guid? BilledByStaffId { get; set; }
    public string? BilledByStaffName { get; set; }
    public DateTime CreatedAt { get; set; }
}
