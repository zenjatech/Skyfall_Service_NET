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

public sealed class StaffFunction
{
    private readonly IStaffRepository _staff;
    private readonly JwtHelper _jwt;
    private readonly ILogger<StaffFunction> _logger;

    public StaffFunction(IStaffRepository staff, JwtHelper jwt, ILogger<StaffFunction> logger)
    {
        _staff = staff;
        _jwt = jwt;
        _logger = logger;
    }

    [Function("GetStaff")]
    [OpenApiOperation(operationId: "GetStaff", tags: new[] { "Staff" }, Summary = "List all staff")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<List<StaffResponse>>))]
    public async Task<HttpResponseData> GetAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "staff")] HttpRequestData req,
        CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt, StaffRoles.Admin);
        if (error is not null) return await error;

        var list = await _staff.GetAllAsync(tenantId, ct);
        return await ResponseFactory.OkAsync(req, list.Select(MapToResponse).ToList());
    }

    [Function("GetStaffById")]
    [OpenApiOperation(operationId: "GetStaffById", tags: new[] { "Staff" }, Summary = "Get staff by id")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<StaffResponse>))]
    public async Task<HttpResponseData> GetById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "staff/{id:guid}")] HttpRequestData req,
        Guid id, CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt, StaffRoles.Admin);
        if (error is not null) return await error;

        var entity = await _staff.GetByIdAsync(id, tenantId, ct);
        if (entity is null) return await ResponseFactory.NotFoundAsync(req, "Staff not found.");
        return await ResponseFactory.OkAsync(req, MapToResponse(entity));
    }

    [Function("CreateStaff")]
    [OpenApiOperation(operationId: "CreateStaff", tags: new[] { "Staff" }, Summary = "Create staff member")]
    [OpenApiRequestBody("application/json", typeof(StaffCreateRequest), Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.Created, "application/json", typeof(ApiResponse<StaffResponse>))]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "staff")] HttpRequestData req,
        CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt, StaffRoles.Admin);
        if (error is not null) return await error;

        var body = await req.ReadFromJsonAsync<StaffCreateRequest>(cancellationToken: ct);
        if (body is null) return await ResponseFactory.BadRequestAsync(req, "Invalid request body.");
        if (string.IsNullOrWhiteSpace(body.Name) || string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
            return await ResponseFactory.BadRequestAsync(req, "Name, email and password are required.");

        var existing = await _staff.GetByEmailAsync(body.Email.Trim().ToLowerInvariant(), tenantId, ct);
        if (existing is not null) return await ResponseFactory.ConflictAsync(req, "Email already registered.");

        var entity = new Staff
        {
            TenantId = tenantId,
            Name = body.Name.Trim(),
            Email = body.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.Password),
            Role = body.Role
        };
        await _staff.AddAsync(entity, ct);
        return await ResponseFactory.CreatedAsync(req, MapToResponse(entity));
    }

    [Function("UpdateStaff")]
    [OpenApiOperation(operationId: "UpdateStaff", tags: new[] { "Staff" }, Summary = "Update staff member")]
    [OpenApiRequestBody("application/json", typeof(StaffUpdateRequest), Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<StaffResponse>))]
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "staff/{id:guid}")] HttpRequestData req,
        Guid id, CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt, StaffRoles.Admin);
        if (error is not null) return await error;

        var entity = await _staff.GetByIdAsync(id, tenantId, ct);
        if (entity is null) return await ResponseFactory.NotFoundAsync(req, "Staff not found.");

        var body = await req.ReadFromJsonAsync<StaffUpdateRequest>(cancellationToken: ct);
        if (body is null) return await ResponseFactory.BadRequestAsync(req, "Invalid request body.");

        if (body.Name is not null) entity.Name = body.Name.Trim();
        if (body.Role is not null) entity.Role = body.Role;
        if (body.IsActive.HasValue) entity.IsActive = body.IsActive.Value;
        entity.UpdatedAt = DateTime.UtcNow;

        await _staff.UpdateAsync(entity, ct);
        return await ResponseFactory.OkAsync(req, MapToResponse(entity));
    }

    [Function("DeleteStaff")]
    [OpenApiOperation(operationId: "DeleteStaff", tags: new[] { "Staff" }, Summary = "Delete staff member")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NoContent)]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "staff/{id:guid}")] HttpRequestData req,
        Guid id, CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt, StaffRoles.Admin);
        if (error is not null) return await error;

        var deleted = await _staff.DeleteAsync(id, tenantId, ct);
        if (!deleted) return await ResponseFactory.NotFoundAsync(req, "Staff not found.");
        return await ResponseFactory.NoContentAsync(req);
    }

    private static StaffResponse MapToResponse(Staff s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Email = s.Email,
        Role = s.Role,
        IsActive = s.IsActive,
        LastLoginAt = s.LastLoginAt,
        CreatedAt = s.CreatedAt
    };
}
