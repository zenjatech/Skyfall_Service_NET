using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Skyfall.Common;
using Skyfall.Contracts.Responses;
using Skyfall.Domain.Entities;
using Skyfall.Infrastructure.Data;

namespace Skyfall.Functions;

public sealed class AnalyticsFunction
{
    private readonly AppDbContext _db;
    private readonly JwtHelper _jwt;
    private readonly ILogger<AnalyticsFunction> _logger;

    public AnalyticsFunction(AppDbContext db, JwtHelper jwt, ILogger<AnalyticsFunction> logger)
    {
        _db = db;
        _jwt = jwt;
        _logger = logger;
    }

    [Function("GetDashboardAnalytics")]
    [OpenApiOperation(operationId: "GetDashboardAnalytics", tags: new[] { "Analytics" }, Summary = "Get dashboard analytics")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<DashboardAnalyticsResponse>))]
    public async Task<HttpResponseData> GetDashboard(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "analytics/dashboard")] HttpRequestData req,
        CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt, StaffRoles.Admin);
        if (error is not null) return await error;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayStart = today.ToDateTime(TimeOnly.MinValue);
        var todayEnd = today.ToDateTime(TimeOnly.MaxValue);

        var todayOrders = await _db.Orders.AsNoTracking()
            .Where(o => o.TenantId == tenantId && o.CreatedAt >= todayStart && o.CreatedAt <= todayEnd
                && o.Status != OrderStatus.Cancelled)
            .ToListAsync(ct);

        var activeTables = await _db.Tables.AsNoTracking()
            .CountAsync(t => t.TenantId == tenantId && t.Status == TableStatus.Occupied, ct);

        var totalCustomers = await _db.Customers.AsNoTracking()
            .CountAsync(c => c.TenantId == tenantId, ct);

        var weekStart = todayStart.AddDays(-6);
        var weeklyOrders = await _db.Orders.AsNoTracking()
            .Where(o => o.TenantId == tenantId && o.CreatedAt >= weekStart && o.Status != OrderStatus.Cancelled)
            .ToListAsync(ct);

        var weeklyRevenue = weeklyOrders
            .GroupBy(o => DateOnly.FromDateTime(o.CreatedAt))
            .Select(g => new DailyRevenueResponse { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount), OrderCount = g.Count() })
            .OrderBy(r => r.Date)
            .ToList();

        var topItems = await _db.OrderItems.AsNoTracking()
            .Include(i => i.MenuItem)
            .Include(i => i.Order)
            .Where(i => i.TenantId == tenantId && i.Order!.Status != OrderStatus.Cancelled
                && i.Order.CreatedAt >= weekStart)
            .GroupBy(i => new { i.MenuItemId, i.MenuItem!.Name })
            .Select(g => new TopMenuItemResponse
            {
                MenuItemId = g.Key.MenuItemId,
                Name = g.Key.Name,
                QuantitySold = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.UnitPrice * i.Quantity)
            })
            .OrderByDescending(t => t.QuantitySold)
            .Take(10)
            .ToListAsync(ct);

        var todayPayments = await _db.Payments.AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.CreatedAt >= todayStart && p.CreatedAt <= todayEnd
                && p.Status == PaymentStatus.Success)
            .ToListAsync(ct);

        var totalPaid = todayPayments.Sum(p => p.Amount);
        var paymentBreakdown = todayPayments
            .GroupBy(p => p.Mode)
            .Select(g => new PaymentBreakdownItem
            {
                Mode = g.Key,
                Amount = g.Sum(p => p.Amount),
                Percent = totalPaid > 0 ? (int)Math.Round(g.Sum(p => p.Amount) / totalPaid * 100) : 0
            })
            .OrderByDescending(p => p.Amount)
            .ToList();

        var result = new DashboardAnalyticsResponse
        {
            TodayRevenue = todayOrders.Sum(o => o.TotalAmount),
            TodayOrders = todayOrders.Count,
            ActiveTables = activeTables,
            TotalCustomers = totalCustomers,
            WeeklyRevenue = weeklyRevenue,
            TopItems = topItems,
            PaymentBreakdown = paymentBreakdown
        };

        return await ResponseFactory.OkAsync(req, result);
    }
}
