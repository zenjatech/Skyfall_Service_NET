using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Skyfall.Functions;

public sealed class WarmupFunction
{
    private readonly ILogger<WarmupFunction> _logger;

    public WarmupFunction(ILogger<WarmupFunction> logger) => _logger = logger;

    [Function("Warmup")]
    public Task Run([WarmupTrigger] object warmupContext)
    {
        _logger.LogInformation("Warmup triggered");
        return Task.CompletedTask;
    }
}
