namespace Skyfall.Contracts.Requests;

public sealed class StaffUpdateRequest
{
    public string? Name { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}
