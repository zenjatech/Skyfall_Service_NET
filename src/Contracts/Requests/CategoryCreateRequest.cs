namespace Skyfall.Contracts.Requests;

public sealed class CategoryCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; } = 0;
}
