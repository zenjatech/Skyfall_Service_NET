namespace Skyfall.Domain.Entities;

public sealed class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public Guid? BilledByStaffId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? PdfUrl { get; set; }
    public bool WhatsappSent { get; set; } = false;
    public bool SmsSent { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Order? Order { get; set; }
    public Staff? BilledByStaff { get; set; }
}
