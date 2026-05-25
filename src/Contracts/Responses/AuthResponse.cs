namespace Skyfall.Contracts.Responses;

public sealed class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public Guid StaffId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
