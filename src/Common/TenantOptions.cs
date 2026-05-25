namespace Skyfall.Common;

public sealed class TenantOptions
{
    public Guid TenantId { get; set; }
    public string Slug { get; set; } = string.Empty;
}
