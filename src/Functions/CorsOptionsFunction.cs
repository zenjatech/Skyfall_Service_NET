using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Skyfall.Functions;

public sealed class CorsOptionsFunction
{
    [Function("CorsPreflightHandler")]
    public HttpResponseData HandleOptions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "options", Route = "{*any}")] HttpRequestData req)
    {
        // CorsMiddleware adds the actual CORS headers; this just ensures OPTIONS
        // requests are routed to the worker instead of rejected 404 by the host.
        return req.CreateResponse(HttpStatusCode.NoContent);
    }
}
