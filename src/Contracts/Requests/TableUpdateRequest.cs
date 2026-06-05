namespace Skyfall.Contracts.Requests;

public sealed class TableUpdateRequest
{
    public int? TableNumber { get; set; }
    public int? Capacity { get; set; }
    public string? Status { get; set; }
}
