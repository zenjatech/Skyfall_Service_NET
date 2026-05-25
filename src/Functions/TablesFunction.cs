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

public sealed class TablesFunction
{
    private readonly ITableRepository _tables;
    private readonly JwtHelper _jwt;
    private readonly ILogger<TablesFunction> _logger;

    public TablesFunction(ITableRepository tables, JwtHelper jwt, ILogger<TablesFunction> logger)
    {
        _tables = tables;
        _jwt = jwt;
        _logger = logger;
    }

    [Function("GetTables")]
    [OpenApiOperation(operationId: "GetTables", tags: new[] { "Tables" }, Summary = "List all tables")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<List<TableResponse>>))]
    public async Task<HttpResponseData> GetAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tables")] HttpRequestData req,
        CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt);
        if (error is not null) return await error;

        var list = await _tables.GetAllAsync(tenantId, ct);
        return await ResponseFactory.OkAsync(req, list.Select(MapToResponse).ToList());
    }

    [Function("GetTableById")]
    [OpenApiOperation(operationId: "GetTableById", tags: new[] { "Tables" }, Summary = "Get table by id")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<TableResponse>))]
    public async Task<HttpResponseData> GetById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tables/{id:guid}")] HttpRequestData req,
        Guid id, CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt);
        if (error is not null) return await error;

        var entity = await _tables.GetByIdAsync(id, tenantId, ct);
        if (entity is null) return await ResponseFactory.NotFoundAsync(req, "Table not found.");
        return await ResponseFactory.OkAsync(req, MapToResponse(entity));
    }

    [Function("CreateTable")]
    [OpenApiOperation(operationId: "CreateTable", tags: new[] { "Tables" }, Summary = "Create table")]
    [OpenApiRequestBody("application/json", typeof(TableCreateRequest), Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.Created, "application/json", typeof(ApiResponse<TableResponse>))]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tables")] HttpRequestData req,
        CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt, StaffRoles.Admin);
        if (error is not null) return await error;

        var body = await req.ReadFromJsonAsync<TableCreateRequest>(cancellationToken: ct);
        if (body is null) return await ResponseFactory.BadRequestAsync(req, "Invalid request body.");

        if (await _tables.TableNumberExistsAsync(body.TableNumber, tenantId, null, ct))
            return await ResponseFactory.ConflictAsync(req, $"Table number {body.TableNumber} already exists.");

        var entity = new CafeTable { TenantId = tenantId, TableNumber = body.TableNumber, Capacity = body.Capacity };
        await _tables.AddAsync(entity, ct);
        return await ResponseFactory.CreatedAsync(req, MapToResponse(entity));
    }

    [Function("UpdateTable")]
    [OpenApiOperation(operationId: "UpdateTable", tags: new[] { "Tables" }, Summary = "Update table")]
    [OpenApiRequestBody("application/json", typeof(TableUpdateRequest), Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<TableResponse>))]
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "tables/{id:guid}")] HttpRequestData req,
        Guid id, CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt, StaffRoles.Admin);
        if (error is not null) return await error;

        var entity = await _tables.GetByIdAsync(id, tenantId, ct);
        if (entity is null) return await ResponseFactory.NotFoundAsync(req, "Table not found.");

        var body = await req.ReadFromJsonAsync<TableUpdateRequest>(cancellationToken: ct);
        if (body is null) return await ResponseFactory.BadRequestAsync(req, "Invalid request body.");

        if (body.Capacity.HasValue) entity.Capacity = body.Capacity.Value;
        if (body.Status is not null) entity.Status = body.Status;
        entity.UpdatedAt = DateTime.UtcNow;

        await _tables.UpdateAsync(entity, ct);
        return await ResponseFactory.OkAsync(req, MapToResponse(entity));
    }

    [Function("DeleteTable")]
    [OpenApiOperation(operationId: "DeleteTable", tags: new[] { "Tables" }, Summary = "Delete table")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NoContent)]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "tables/{id:guid}")] HttpRequestData req,
        Guid id, CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt, StaffRoles.Admin);
        if (error is not null) return await error;

        var deleted = await _tables.DeleteAsync(id, tenantId, ct);
        if (!deleted) return await ResponseFactory.NotFoundAsync(req, "Table not found.");
        return await ResponseFactory.NoContentAsync(req);
    }


    private static TableResponse MapToResponse(CafeTable t) => new()
    {
        Id = t.Id,
        TableNumber = t.TableNumber,
        QrCodeUrl = t.QrCodeUrl,
        Status = t.Status,
        Capacity = t.Capacity,
        CreatedAt = t.CreatedAt
    };
}
