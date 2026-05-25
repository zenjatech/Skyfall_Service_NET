using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Skyfall.Common;
using Skyfall.Contracts.Responses;
using Skyfall.Domain.Entities;
using Skyfall.Infrastructure.Repositories;

namespace Skyfall.Functions;

public sealed class BillingFunction
{
    private readonly IInvoiceRepository _invoices;
    private readonly IOrderRepository _orders;
    private readonly ITableRepository _tables;
    private readonly ICustomerRepository _customers;
    private readonly JwtHelper _jwt;
    private readonly ILogger<BillingFunction> _logger;

    public BillingFunction(IInvoiceRepository invoices, IOrderRepository orders, ITableRepository tables,
        ICustomerRepository customers, JwtHelper jwt, ILogger<BillingFunction> logger)
    {
        _invoices = invoices;
        _orders = orders;
        _tables = tables;
        _customers = customers;
        _jwt = jwt;
        _logger = logger;
    }

    [Function("GenerateInvoice")]
    [OpenApiOperation(operationId: "GenerateInvoice", tags: new[] { "Billing" }, Summary = "Generate invoice for an order")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<InvoiceResponse>))]
    public async Task<HttpResponseData> Generate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "billing/orders/{orderId:guid}/invoice")] HttpRequestData req,
        Guid orderId, CancellationToken ct)
    {
        var (principal, tenantId, error) = AuthHelper.Authorize(req, _jwt);
        if (error is not null) return await error;

        var existing = await _invoices.GetByOrderIdAsync(orderId, tenantId, ct);
        if (existing is not null) return await ResponseFactory.OkAsync(req, MapToResponse(existing));

        var order = await _orders.GetByIdAsync(orderId, tenantId, ct);
        if (order is null) return await ResponseFactory.NotFoundAsync(req, "Order not found.");
        if (order.Status is OrderStatus.Cancelled)
            return await ResponseFactory.UnprocessableAsync(req, "Cannot generate invoice for cancelled order.");

        var staffId = JwtHelper.GetStaffId(principal!);
        var invoiceNumber = await _invoices.GenerateInvoiceNumberAsync(tenantId, ct);

        var invoice = new Invoice
        {
            TenantId = tenantId,
            OrderId = orderId,
            BilledByStaffId = staffId,
            InvoiceNumber = invoiceNumber
        };
        await _invoices.AddAsync(invoice, ct);

        if (order.CustomerId.HasValue)
        {
            var customer = await _customers.GetByIdAsync(order.CustomerId.Value, tenantId, ct);
            if (customer is not null)
            {
                customer.VisitCount++;
                customer.TotalSpent += order.TotalAmount;
                customer.LastVisit = DateTime.UtcNow;
                customer.UpdatedAt = DateTime.UtcNow;
                await _customers.UpdateAsync(customer, ct);
            }
        }

        var created = await _invoices.GetByIdAsync(invoice.Id, tenantId, ct);
        return await ResponseFactory.OkAsync(req, MapToResponse(created!));
    }

    [Function("GetInvoice")]
    [OpenApiOperation(operationId: "GetInvoice", tags: new[] { "Billing" }, Summary = "Get invoice for an order")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<InvoiceResponse>))]
    public async Task<HttpResponseData> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "billing/orders/{orderId:guid}/invoice")] HttpRequestData req,
        Guid orderId, CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt);
        if (error is not null) return await error;

        var invoice = await _invoices.GetByOrderIdAsync(orderId, tenantId, ct);
        if (invoice is null) return await ResponseFactory.NotFoundAsync(req, "Invoice not found.");
        return await ResponseFactory.OkAsync(req, MapToResponse(invoice));
    }

    private static InvoiceResponse MapToResponse(Invoice i) => new()
    {
        Id = i.Id,
        OrderId = i.OrderId,
        InvoiceNumber = i.InvoiceNumber,
        PdfUrl = i.PdfUrl,
        WhatsappSent = i.WhatsappSent,
        SmsSent = i.SmsSent,
        BilledByStaffId = i.BilledByStaffId,
        BilledByStaffName = i.BilledByStaff?.Name,
        CreatedAt = i.CreatedAt
    };
}
