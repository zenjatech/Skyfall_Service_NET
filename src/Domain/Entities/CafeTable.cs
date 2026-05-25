namespace Skyfall.Domain.Entities;

public sealed class CafeTable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public int TableNumber { get; set; }
    public string? QrCodeUrl { get; set; }
    public string Status { get; set; } = TableStatus.Free;
    public int Capacity { get; set; } = 4;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public static class TableStatus
{
    public const string Free = "free";
    public const string Occupied = "occupied";
    public const string Reserved = "reserved";
    public const string BillRequested = "bill_requested";
}
