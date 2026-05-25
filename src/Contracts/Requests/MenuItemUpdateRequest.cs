namespace Skyfall.Contracts.Requests;

public sealed class MenuItemUpdateRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? BasePrice { get; set; }
    public string? ImageUrl { get; set; }
    public bool? IsAvailable { get; set; }
    public bool? IsVeg { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public Guid? CategoryId { get; set; }
}
