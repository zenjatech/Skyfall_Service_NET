namespace Skyfall.Contracts.Responses;

public sealed class KotResponse
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public int KotNumber { get; set; }
    public string ItemsJson { get; set; } = "[]";
    public string Status { get; set; } = string.Empty;
    public DateTime? PrintedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int TableNumber { get; set; }
}
