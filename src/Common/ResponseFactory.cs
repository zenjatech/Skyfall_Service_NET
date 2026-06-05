using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker.Http;

namespace Skyfall.Common;

public static class ResponseFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task<HttpResponseData> OkAsync<T>(HttpRequestData req, T data, string message = "OK")
        => await WriteJson(req, HttpStatusCode.OK, ApiResponse<T>.Ok(data, message));

    public static async Task<HttpResponseData> CreatedAsync<T>(HttpRequestData req, T data, string message = "Created")
        => await WriteJson(req, HttpStatusCode.Created, ApiResponse<T>.Ok(data, message));

    public static Task<HttpResponseData> NoContentAsync(HttpRequestData req)
        => Task.FromResult(req.CreateResponse(HttpStatusCode.NoContent));

    public static async Task<HttpResponseData> BadRequestAsync(HttpRequestData req, string message, string[]? errors = null)
        => await WriteJson(req, HttpStatusCode.BadRequest, ApiResponse.Fail(message, errors));

    public static async Task<HttpResponseData> UnauthorizedAsync(HttpRequestData req, string message = "Unauthorized.")
        => await WriteJson(req, HttpStatusCode.Unauthorized, ApiResponse.Fail(message));

    public static async Task<HttpResponseData> ForbiddenAsync(HttpRequestData req, string message = "Forbidden.")
        => await WriteJson(req, HttpStatusCode.Forbidden, ApiResponse.Fail(message));

    public static async Task<HttpResponseData> NotFoundAsync(HttpRequestData req, string message)
        => await WriteJson(req, HttpStatusCode.NotFound, ApiResponse.Fail(message));

    public static async Task<HttpResponseData> ConflictAsync(HttpRequestData req, string message)
        => await WriteJson(req, HttpStatusCode.Conflict, ApiResponse.Fail(message));

    public static async Task<HttpResponseData> UnprocessableAsync(HttpRequestData req, string message, string[]? errors = null)
        => await WriteJson(req, HttpStatusCode.UnprocessableEntity, ApiResponse.Fail(message, errors));

    private static async Task<HttpResponseData> WriteJson<T>(HttpRequestData req, HttpStatusCode status, T body)
    {
        var res = req.CreateResponse(status);
        res.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await res.WriteStringAsync(JsonSerializer.Serialize(body, JsonOptions));
        return res;
    }
}
