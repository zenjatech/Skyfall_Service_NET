namespace Skyfall.Common;

public sealed class JwtOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 1440;
}
