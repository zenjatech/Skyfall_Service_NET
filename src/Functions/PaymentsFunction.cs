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
    private readonly JwtHelper _jwt;
    private readonly ILogger<PaymentsFunction> _logger;

    public PaymentsFunction(IPaymentRepository payments, IOrderRepository orders, JwtHelper jwt, ILogger<PaymentsFunction> logger)
    {
        _payments = payments;
        _orders = orders;
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
            Status = PaymentStatus.Success,
            RazorpayOrderId = body.RazorpayOrderId,
            RazorpayPaymentId = body.RazorpayPaymentId
        };
        await _payments.AddAsync(payment, ct);
        return await ResponseFactory.CreatedAsync(req, MapToResponse(payment));
    }

    private static PaymentResponse MapToResponse(Payment p) => new()
    {
        Id = p.Id,
        OrderId = p.OrderId,
        Mode = p.Mode,
        Amount = p.Amount,
        Status = p.Status,
        RazorpayOrderId = p.RazorpayOrderId,
        RazorpayPaymentId = p.RazorpayPaymentId,
        CreatedAt = p.CreatedAt
    };
}
