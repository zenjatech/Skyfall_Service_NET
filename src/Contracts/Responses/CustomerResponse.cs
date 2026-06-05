namespace Skyfall.Contracts.Responses;

public sealed class CustomerResponse
{
    public Guid Id { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public DateOnly? Birthday { get; set; }
    public DateOnly? Anniversary { get; set; }
    public DateOnly? SpecialEventDate { get; set; }
    public string? SpecialEventName { get; set; }
    public int VisitCount { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastVisit { get; set; }
    public DateTime CreatedAt { get; set; }
}
