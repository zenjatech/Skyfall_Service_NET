using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Skyfall.Common;
using Skyfall.Contracts.Requests;
using Skyfall.Contracts.Responses;
using Skyfall.Domain.Entities;
using Skyfall.Infrastructure.Data;
using Skyfall.Infrastructure.Repositories;

namespace Skyfall.Functions;

public sealed class CustomersFunction
{
    private readonly ICustomerRepository _customers;
    private readonly AppDbContext _db;
    private readonly JwtHelper _jwt;
    private readonly ILogger<CustomersFunction> _logger;

    public CustomersFunction(ICustomerRepository customers, AppDbContext db, JwtHelper jwt, ILogger<CustomersFunction> logger)
    {
        _customers = customers;
        _db = db;
        _jwt = jwt;
        _logger = logger;
    }

    [Function("GetCustomers")]
    [OpenApiOperation(operationId: "GetCustomers", tags: new[] { "Customers" }, Summary = "List all customers")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<List<CustomerResponse>>))]
    public async Task<HttpResponseData> GetAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers")] HttpRequestData req,
        CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt);
        if (error is not null) return await error;

        var list = await _customers.GetAllAsync(tenantId, ct);

        var visitCounts = await _db.Orders.AsNoTracking()
            .Where(o => o.TenantId == tenantId && o.CustomerId.HasValue && o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.CustomerId!.Value)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.CustomerId, g => g.Count, ct);

        var totalSpentMap = await _db.Orders.AsNoTracking()
            .Where(o => o.TenantId == tenantId && o.CustomerId.HasValue && o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.CustomerId!.Value)
            .Select(g => new { CustomerId = g.Key, Total = g.Sum(o => o.TotalAmount) })
            .ToDictionaryAsync(g => g.CustomerId, g => g.Total, ct);

        return await ResponseFactory.OkAsync(req, list.Select(c => MapToResponse(c, visitCounts, totalSpentMap)).ToList());
    }

    [Function("GetCustomerById")]
    [OpenApiOperation(operationId: "GetCustomerById", tags: new[] { "Customers" }, Summary = "Get customer by id")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<CustomerResponse>))]
    public async Task<HttpResponseData> GetById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers/{id:guid}")] HttpRequestData req,
        Guid id, CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt);
        if (error is not null) return await error;

        var entity = await _customers.GetByIdAsync(id, tenantId, ct);
        if (entity is null) return await ResponseFactory.NotFoundAsync(req, "Customer not found.");

        var visitCount = await _db.Orders.AsNoTracking()
            .CountAsync(o => o.TenantId == tenantId && o.CustomerId == id && o.Status != OrderStatus.Cancelled, ct);
        var totalSpent = await _db.Orders.AsNoTracking()
            .Where(o => o.TenantId == tenantId && o.CustomerId == id && o.Status != OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.TotalAmount ?? 0, ct);

        return await ResponseFactory.OkAsync(req, MapToResponse(entity, visitCount, totalSpent));
    }

    [Function("UpsertCustomer")]
    [OpenApiOperation(operationId: "UpsertCustomer", tags: new[] { "Customers" }, Summary = "Create or update customer by phone")]
    [OpenApiRequestBody("application/json", typeof(CustomerUpsertRequest), Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<CustomerResponse>))]
    public async Task<HttpResponseData> Upsert(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers/upsert")] HttpRequestData req,
        CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt);
        if (error is not null) return await error;

        var body = await req.ReadFromJsonAsync<CustomerUpsertRequest>(cancellationToken: ct);
        if (body is null || string.IsNullOrWhiteSpace(body.Phone))
            return await ResponseFactory.BadRequestAsync(req, "Phone is required.");

        var existing = await _customers.GetByPhoneAsync(body.Phone.Trim(), tenantId, ct);
        if (existing is not null)
        {
            if (body.Name is not null) existing.Name = body.Name.Trim();
            if (body.Email is not null) existing.Email = body.Email.Trim();
            if (body.Birthday.HasValue) existing.Birthday = body.Birthday;
            if (body.Anniversary.HasValue) existing.Anniversary = body.Anniversary;
            if (body.SpecialEventDate.HasValue) existing.SpecialEventDate = body.SpecialEventDate;
            if (!string.IsNullOrWhiteSpace(body.SpecialEventName)) existing.SpecialEventName = body.SpecialEventName.Trim();
            existing.UpdatedAt = DateTime.UtcNow;
            await _customers.UpdateAsync(existing, ct);
            return await ResponseFactory.OkAsync(req, MapToResponse(existing));
        }

        var entity = new Customer
        {
            TenantId = tenantId,
            Phone = body.Phone.Trim(),
            Name = body.Name?.Trim(),
            Email = body.Email?.Trim(),
            Birthday = body.Birthday,
            Anniversary = body.Anniversary,
            SpecialEventDate = body.SpecialEventDate,
            SpecialEventName = string.IsNullOrWhiteSpace(body.SpecialEventName) ? null : body.SpecialEventName.Trim()
        };
        await _customers.AddAsync(entity, ct);
        return await ResponseFactory.CreatedAsync(req, MapToResponse(entity));
    }

    [Function("DeleteCustomer")]
    [OpenApiOperation(operationId: "DeleteCustomer", tags: new[] { "Customers" }, Summary = "Delete customer")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NoContent)]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "customers/{id:guid}")] HttpRequestData req,
        Guid id, CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt, StaffRoles.Admin);
        if (error is not null) return await error;

        var deleted = await _customers.DeleteAsync(id, tenantId, ct);
        if (!deleted) return await ResponseFactory.NotFoundAsync(req, "Customer not found.");
        return await ResponseFactory.NoContentAsync(req);
    }

    private static CustomerResponse MapToResponse(Customer c, Dictionary<Guid, int> visitCounts, Dictionary<Guid, decimal> totalSpentMap) =>
        MapToResponse(c, visitCounts.GetValueOrDefault(c.Id, 0), totalSpentMap.GetValueOrDefault(c.Id, 0));

    private static CustomerResponse MapToResponse(Customer c, int visitCount = -1, decimal totalSpent = -1) => new()
    {
        Id = c.Id, Phone = c.Phone, Name = c.Name, Email = c.Email, Birthday = c.Birthday,
        Anniversary = c.Anniversary, SpecialEventDate = c.SpecialEventDate, SpecialEventName = c.SpecialEventName,
        VisitCount = visitCount >= 0 ? visitCount : c.VisitCount,
        TotalSpent = totalSpent >= 0 ? totalSpent : c.TotalSpent,
        LastVisit = c.LastVisit, CreatedAt = c.CreatedAt
    };
}
