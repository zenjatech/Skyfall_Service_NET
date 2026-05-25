namespace Skyfall.Domain.Entities;

public sealed class KOT
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public int KotNumber { get; set; }
    public string ItemsJson { get; set; } = "[]";
    public string Status { get; set; } = KotStatus.New;
    public DateTime? PrintedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Order? Order { get; set; }
}

public static class KotStatus
{
    public const string New = "new";
    public const string Acknowledged = "acknowledged";
    public const string Completed = "completed";
}
