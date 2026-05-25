using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Skyfall.Common;

namespace Skyfall.Functions;

public sealed class HealthFunction
{
    private readonly ILogger<HealthFunction> _logger;

    public HealthFunction(ILogger<HealthFunction> logger) => _logger = logger;

    [Function("Health")]
    [OpenApiOperation(operationId: "Health", tags: new[] { "Health" }, Summary = "Health check")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<object>))]
    public async Task<HttpResponseData> Health(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req,
        CancellationToken ct)
    {
        _logger.LogInformation("Health check called");
        return await ResponseFactory.OkAsync(req, new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
