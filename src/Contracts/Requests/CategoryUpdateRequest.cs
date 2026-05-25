namespace Skyfall.Contracts.Requests;

public sealed class CategoryUpdateRequest
{
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsActive { get; set; }
}
