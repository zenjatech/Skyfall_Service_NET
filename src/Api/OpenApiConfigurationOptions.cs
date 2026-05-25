using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace Skyfall.Api;

public class OpenApiConfigurationOptions : IOpenApiConfigurationOptions
{
    public OpenApiInfo Info { get; set; } = new()
    {
        Version = "1.0.0",
        Title = "Skyfall Lounge API",
        Description = "Restaurant POS API — orders, tables, menu, KOT, billing, analytics."
    };

    public List<OpenApiServer> Servers { get; set; } = [];
    public OpenApiVersionType OpenApiVersion { get; set; } = OpenApiVersionType.V3;
    public bool IncludeRequestingHostName { get; set; } = true;
    public bool ForceHttps { get; set; } = false;
    public bool ForceHttp { get; set; } = false;
    public List<IDocumentFilter> DocumentFilters { get; set; } = [];
}
