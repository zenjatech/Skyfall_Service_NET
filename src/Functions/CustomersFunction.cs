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

public sealed class CustomersFunction
{
    private readonly ICustomerRepository _customers;
    private readonly JwtHelper _jwt;
    private readonly ILogger<CustomersFunction> _logger;

    public CustomersFunction(ICustomerRepository customers, JwtHelper jwt, ILogger<CustomersFunction> logger)
    {
        _customers = customers;
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
        return await ResponseFactory.OkAsync(req, list.Select(MapToResponse).ToList());
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
        return await ResponseFactory.OkAsync(req, MapToResponse(entity));
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
            SpecialEventDate = body.SpecialEventDate
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

    private static CustomerResponse MapToResponse(Customer c) => new()
    {
        Id = c.Id, Phone = c.Phone, Name = c.Name, Email = c.Email, Birthday = c.Birthday,
        Anniversary = c.Anniversary, SpecialEventDate = c.SpecialEventDate, VisitCount = c.VisitCount,
        TotalSpent = c.TotalSpent, LastVisit = c.LastVisit, CreatedAt = c.CreatedAt
    };
}
