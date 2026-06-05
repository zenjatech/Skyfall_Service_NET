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

public sealed class PaymentsFunction
{
    private readonly IPaymentRepository _payments;
    private readonly IOrderRepository _orders;
    private readonly ITableRepository _tables;
    private readonly JwtHelper _jwt;
    private readonly ILogger<PaymentsFunction> _logger;

    public PaymentsFunction(IPaymentRepository payments, IOrderRepository orders, ITableRepository tables, JwtHelper jwt, ILogger<PaymentsFunction> logger)
    {
        _payments = payments;
        _orders = orders;
        _tables = tables;
        _jwt = jwt;
        _logger = logger;
    }

    [Function("GetPayments")]
    [OpenApiOperation(operationId: "GetPayments", tags: new[] { "Payments" }, Summary = "Get payments for an order")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<List<PaymentResponse>>))]
    public async Task<HttpResponseData> GetByOrder(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "payments/orders/{orderId:guid}")] HttpRequestData req,
        Guid orderId, CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt);
        if (error is not null) return await error;

        var list = await _payments.GetByOrderAsync(orderId, tenantId, ct);
        return await ResponseFactory.OkAsync(req, list.Select(MapToResponse).ToList());
    }

    [Function("CreatePayment")]
    [OpenApiOperation(operationId: "CreatePayment", tags: new[] { "Payments" }, Summary = "Record a payment")]
    [OpenApiRequestBody("application/json", typeof(PaymentCreateRequest), Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.Created, "application/json", typeof(ApiResponse<PaymentResponse>))]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "payments")] HttpRequestData req,
        CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt);
        if (error is not null) return await error;

        var body = await req.ReadFromJsonAsync<PaymentCreateRequest>(cancellationToken: ct);
        if (body is null) return await ResponseFactory.BadRequestAsync(req, "Invalid request body.");

        var order = await _orders.GetByIdAsync(body.OrderId, tenantId, ct);
        if (order is null) return await ResponseFactory.NotFoundAsync(req, "Order not found.");

        var payment = new Payment
        {
            TenantId = tenantId,
            OrderId = body.OrderId,
            Mode = body.Mode,
            Amount = body.Amount,
            Tip = body.Tip,
            Status = PaymentStatus.Success,
            RazorpayOrderId = body.RazorpayOrderId,
            RazorpayPaymentId = body.RazorpayPaymentId
        };
        await _payments.AddAsync(payment, ct);

        // Auto-serve + free table when fully paid
        var allPayments = await _payments.GetByOrderAsync(body.OrderId, tenantId, ct);
        var totalPaid = allPayments.Where(p => p.Status == PaymentStatus.Success).Sum(p => p.Amount);
        if (totalPaid >= order.TotalAmount)
        {
            if (order.Status is not OrderStatus.Served and not OrderStatus.Cancelled)
            {
                order.Status = OrderStatus.Served;
                order.UpdatedAt = DateTime.UtcNow;
                await _orders.UpdateAsync(order, ct);
            }

            var table = await _tables.GetByIdAsync(order.TableId, tenantId, ct);
            if (table is not null && table.Status != TableStatus.Free)
            {
                var otherActive = (await _orders.GetByTableAsync(order.TableId, tenantId, ct))
                    .Any(o => o.Id != order.Id && o.Status is not OrderStatus.Served and not OrderStatus.Cancelled);
                if (!otherActive)
                {
                    table.Status = TableStatus.Free;
                    table.UpdatedAt = DateTime.UtcNow;
                    await _tables.UpdateAsync(table, ct);
                }
            }
        }

        return await ResponseFactory.CreatedAsync(req, MapToResponse(payment));
    }

    private static PaymentResponse MapToResponse(Payment p) => new()
    {
        Id = p.Id,
        OrderId = p.OrderId,
        Mode = p.Mode,
        Amount = p.Amount,
        Tip = p.Tip,
        Status = p.Status,
        RazorpayOrderId = p.RazorpayOrderId,
        RazorpayPaymentId = p.RazorpayPaymentId,
        CreatedAt = p.CreatedAt
    };
}
