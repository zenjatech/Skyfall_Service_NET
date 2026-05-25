using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Skyfall.Common;
using Skyfall.Contracts.Requests;
using Skyfall.Contracts.Responses;
using Skyfall.Domain.Entities;
using Skyfall.Infrastructure.Repositories;

namespace Skyfall.Functions;

public sealed class KOTFunction
{
    private readonly IKOTRepository _kots;
    private readonly JwtHelper _jwt;
    private readonly ILogger<KOTFunction> _logger;

    public KOTFunction(IKOTRepository kots, JwtHelper jwt, ILogger<KOTFunction> logger)
    {
        _kots = kots;
        _jwt = jwt;
        _logger = logger;
    }

    [Function("GetKOTs")]
    [OpenApiOperation(operationId: "GetKOTs", tags: new[] { "KOT" }, Summary = "List KOTs for kitchen display")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<List<KotResponse>>))]
    public async Task<HttpResponseData> GetAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "kots")] HttpRequestData req,
        CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt);
        if (error is not null) return await error;

        var qs = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var status = qs["status"];
        var list = await _kots.GetAllAsync(tenantId, status, ct);
        return await ResponseFactory.OkAsync(req, list.Select(MapToResponse).ToList());
    }

    [Function("UpdateKOTStatus")]
    [OpenApiOperation(operationId: "UpdateKOTStatus", tags: new[] { "KOT" }, Summary = "Update KOT status")]
    [OpenApiRequestBody("application/json", typeof(KotStatusUpdateRequest), Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<KotResponse>))]
    public async Task<HttpResponseData> UpdateStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "kots/{id:guid}/status")] HttpRequestData req,
        Guid id, CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt);
        if (error is not null) return await error;

        var entity = await _kots.GetByIdAsync(id, tenantId, ct);
        if (entity is null) return await ResponseFactory.NotFoundAsync(req, "KOT not found.");

        var body = await req.ReadFromJsonAsync<KotStatusUpdateRequest>(cancellationToken: ct);
        if (body is null) return await ResponseFactory.BadRequestAsync(req, "Invalid request body.");

        var validStatuses = new[] { KotStatus.New, KotStatus.Acknowledged, KotStatus.Completed };
        if (!validStatuses.Contains(body.Status))
            return await ResponseFactory.BadRequestAsync(req, "Invalid KOT status.");

        entity.Status = body.Status;
        entity.UpdatedAt = DateTime.UtcNow;
        await _kots.UpdateAsync(entity, ct);
        return await ResponseFactory.OkAsync(req, MapToResponse(entity));
    }

    private static KotResponse MapToResponse(KOT k) => new()
    {
        Id = k.Id,
        OrderId = k.OrderId,
        KotNumber = k.KotNumber,
        ItemsJson = k.ItemsJson,
        Status = k.Status,
        PrintedAt = k.PrintedAt,
        CreatedAt = k.CreatedAt,
        UpdatedAt = k.UpdatedAt,
        TableNumber = k.Order?.Table?.TableNumber ?? 0
    };
}
