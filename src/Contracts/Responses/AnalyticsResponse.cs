namespace Skyfall.Contracts.Responses;

public sealed class DashboardAnalyticsResponse
{
    public decimal TodayRevenue { get; set; }
    public int TodayOrders { get; set; }
    public int ActiveTables { get; set; }
    public int TotalCustomers { get; set; }
    public List<DailyRevenueResponse> WeeklyRevenue { get; set; } = [];
    public List<TopMenuItemResponse> TopItems { get; set; } = [];
    public List<PaymentBreakdownItem> PaymentBreakdown { get; set; } = [];
}

public sealed class PaymentBreakdownItem
{
    public string Mode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Percent { get; set; }
}

public sealed class DailyRevenueResponse
{
    public DateOnly Date { get; set; }
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public sealed class TopMenuItemResponse
{
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}
