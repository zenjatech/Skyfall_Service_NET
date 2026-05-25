namespace Skyfall.Contracts.Responses;

public sealed class TableResponse
{
    public Guid Id { get; set; }
    public int TableNumber { get; set; }
    public string? QrCodeUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public DateTime CreatedAt { get; set; }
}
