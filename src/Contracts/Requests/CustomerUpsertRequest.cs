namespace Skyfall.Contracts.Requests;

public sealed class CustomerUpsertRequest
{
    public string Phone { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public DateOnly? Birthday { get; set; }
    public DateOnly? Anniversary { get; set; }
    public DateOnly? SpecialEventDate { get; set; }
    public string? SpecialEventName { get; set; }
}
