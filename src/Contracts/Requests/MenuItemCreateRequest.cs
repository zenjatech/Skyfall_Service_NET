namespace Skyfall.Contracts.Requests;

public sealed class MenuItemCreateRequest
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsVeg { get; set; } = true;
    public int PrepTimeMinutes { get; set; } = 15;
}
