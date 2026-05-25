namespace Skyfall.Contracts.Requests;

public sealed class TableCreateRequest
{
    public int TableNumber { get; set; }
    public int Capacity { get; set; } = 4;
}
