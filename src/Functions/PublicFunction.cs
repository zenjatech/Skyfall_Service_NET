using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Skyfall.Common;
using Skyfall.Contracts.Requests;
using Skyfall.Contracts.Responses;
using Skyfall.Domain.Entities;
using Skyfall.Infrastructure.Repositories;

namespace Skyfall.Functions;

public sealed class PublicFunction
{
    private const decimal TaxRate = 0.05m;
    private const string DefaultTenantSlug = "skyfall";

    private readonly ITenantRepository _tenants;
    private readonly IMenuRepository _menu;
    private readonly ITableRepository _tables;
    private readonly ICustomerRepository _customers;
    private readonly IOrderRepository _orders;
    private readonly IPaymentRepository _payments;
    private readonly IInvoiceRepository _invoices;

    public PublicFunction(
        ITenantRepository tenants,
        IMenuRepository menu,
        ITableRepository tables,
        ICustomerRepository customers,
        IOrderRepository orders,
        IPaymentRepository payments,
        IInvoiceRepository invoices)
    {
        _tenants = tenants;
        _menu = menu;
        _tables = tables;
        _customers = customers;
        _orders = orders;
        _payments = payments;
        _invoices = invoices;
    }

    [Function("GetPublicMenu")]
    [OpenApiOperation(operationId: "GetPublicMenu", tags: new[] { "Public" }, Summary = "Get public QR menu")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<PublicMenuResponse>))]
    public async Task<HttpResponseData> GetMenu(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/menu")] HttpRequestData req,
        CancellationToken ct)
    {
        var tenant = await ResolveTenantAsync(req, ct);
        if (tenant is null) return await ResponseFactory.NotFoundAsync(req, "Cafe tenant not found.");

        var categories = await _menu.GetCategoriesAsync(tenant.Id, ct);
        var items = await _menu.GetMenuItemsAsync(tenant.Id, null, ct);

        var response = new PublicMenuResponse
        {
            Categories = categories.Where(c => c.IsActive).Select(MapCategory).ToList(),
            Items = items.Where(i => i.IsAvailable).Select(MapMenuItem).ToList()
        };
        return await ResponseFactory.OkAsync(req, response);
    }

    [Function("GetPublicTable")]
    [OpenApiOperation(operationId: "GetPublicTable", tags: new[] { "Public" }, Summary = "Get public table")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<TableResponse>))]
    public async Task<HttpResponseData> GetTable(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/tables/{id:guid}")] HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var tenant = await ResolveTenantAsync(req, ct);
        if (tenant is null) return await ResponseFactory.NotFoundAsync(req, "Cafe tenant not found.");

        var table = await _tables.GetByIdAsync(id, tenant.Id, ct);
        if (table is null) return await ResponseFactory.NotFoundAsync(req, "Table not found.");
        return await ResponseFactory.OkAsync(req, MapTable(table));
    }

    [Function("GetPublicTableByNumber")]
    [OpenApiOperation(operationId: "GetPublicTableByNumber", tags: new[] { "Public" }, Summary = "Get public table by number")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<TableResponse>))]
    public async Task<HttpResponseData> GetTableByNumber(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/tables/by-number/{tableNumber:int}")] HttpRequestData req,
        int tableNumber,
        CancellationToken ct)
    {
        var tenant = await ResolveTenantAsync(req, ct);
        if (tenant is null) return await ResponseFactory.NotFoundAsync(req, "Cafe tenant not found.");

        var table = await _tables.GetByNumberAsync(tableNumber, tenant.Id, ct);
        if (table is null) return await ResponseFactory.NotFoundAsync(req, "Table not found.");
        return await ResponseFactory.OkAsync(req, MapTable(table));
    }

    [Function("IdentifyPublicCustomer")]
    [OpenApiOperation(operationId: "IdentifyPublicCustomer", tags: new[] { "Public" }, Summary = "Create or update public customer")]
    [OpenApiRequestBody("application/json", typeof(CustomerUpsertRequest), Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<CustomerIdentifyResponse>))]
    public async Task<HttpResponseData> IdentifyCustomer(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "public/customers/identify")] HttpRequestData req,
        CancellationToken ct)
    {
        var tenant = await ResolveTenantAsync(req, ct);
        if (tenant is null) return await ResponseFactory.NotFoundAsync(req, "Cafe tenant not found.");

        var body = await req.ReadFromJsonAsync<CustomerUpsertRequest>(cancellationToken: ct);
        if (body is null || string.IsNullOrWhiteSpace(body.Phone))
            return await ResponseFactory.BadRequestAsync(req, "Phone is required.");

        var phone = body.Phone.Trim();
        var existing = await _customers.GetByPhoneAsync(phone, tenant.Id, ct);
        var isNew = existing is null;
        var customer = existing ?? new Customer { TenantId = tenant.Id, Phone = phone };

        if (!string.IsNullOrWhiteSpace(body.Name)) customer.Name = body.Name.Trim();
        if (!string.IsNullOrWhiteSpace(body.Email)) customer.Email = body.Email.Trim();
        if (body.Birthday.HasValue) customer.Birthday = body.Birthday;
        if (body.Anniversary.HasValue) customer.Anniversary = body.Anniversary;
        if (body.SpecialEventDate.HasValue) customer.SpecialEventDate = body.SpecialEventDate;
        if (!string.IsNullOrWhiteSpace(body.SpecialEventName)) customer.SpecialEventName = body.SpecialEventName.Trim();
        customer.UpdatedAt = DateTime.UtcNow;

        if (isNew) await _customers.AddAsync(customer, ct);
        else await _customers.UpdateAsync(customer, ct);

        return await ResponseFactory.OkAsync(req, new CustomerIdentifyResponse
        {
            Customer = MapCustomer(customer),
            IsNew = isNew
        });
    }

    [Function("CreatePublicOrder")]
    [OpenApiOperation(operationId: "CreatePublicOrder", tags: new[] { "Public" }, Summary = "Create public QR order")]
    [OpenApiRequestBody("application/json", typeof(OrderCreateRequest), Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.Created, "application/json", typeof(ApiResponse<OrderResponse>))]
    public async Task<HttpResponseData> CreateOrder(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "public/orders")] HttpRequestData req,
        CancellationToken ct)
    {
        var tenant = await ResolveTenantAsync(req, ct);
        if (tenant is null) return await ResponseFactory.NotFoundAsync(req, "Cafe tenant not found.");

        var body = await req.ReadFromJsonAsync<OrderCreateRequest>(cancellationToken: ct);
        if (body is null || body.Items.Count == 0)
            return await ResponseFactory.BadRequestAsync(req, "At least one item is required.");

        var table = await _tables.GetByIdAsync(body.TableId, tenant.Id, ct);
        if (table is null) return await ResponseFactory.BadRequestAsync(req, "Table not found.");

        var order = new Order
        {
            TenantId = tenant.Id,
            TableId = body.TableId,
            CustomerId = body.CustomerId,
            Status = OrderStatus.Pending,
            OrderType = string.IsNullOrWhiteSpace(body.OrderType) ? "dine_in" : body.OrderType,
            SpecialInstructions = body.SpecialInstructions
        };

        decimal subtotal = 0;
        var kotItems = new List<object>();

        foreach (var itemReq in body.Items)
        {
            var menuItem = await _menu.GetMenuItemByIdAsync(itemReq.MenuItemId, tenant.Id, ct);
            if (menuItem is null || !menuItem.IsAvailable)
                return await ResponseFactory.BadRequestAsync(req, $"Menu item {itemReq.MenuItemId} is unavailable.");

            decimal unitPrice = menuItem.BasePrice;
            string? variantName = null;

            if (itemReq.VariantId.HasValue)
            {
                var variant = menuItem.Variants.FirstOrDefault(v => v.Id == itemReq.VariantId.Value && v.IsAvailable);
                if (variant is null) return await ResponseFactory.BadRequestAsync(req, "Variant not found.");
                unitPrice += variant.PriceModifier;
                variantName = variant.Name;
            }

            var addonNames = new List<string>();
            foreach (var addonId in itemReq.AddonIds)
            {
                var addon = menuItem.Addons.FirstOrDefault(a => a.Id == addonId && a.IsAvailable);
                if (addon is null) return await ResponseFactory.BadRequestAsync(req, "Addon not found.");
                unitPrice += addon.ExtraPrice;
                addonNames.Add(addon.Name);
            }

            subtotal += unitPrice * itemReq.Quantity;
            order.Items.Add(new OrderItem
            {
                TenantId = tenant.Id,
                MenuItemId = itemReq.MenuItemId,
                VariantId = itemReq.VariantId,
                Quantity = itemReq.Quantity,
                UnitPrice = unitPrice,
                AddonsJson = addonNames.Count > 0 ? JsonSerializer.Serialize(addonNames) : null,
                SpecialInstructions = itemReq.SpecialInstructions
            });

            kotItems.Add(new
            {
                menuItem.Name,
                Variant = variantName,
                Addons = addonNames,
                itemReq.Quantity,
                itemReq.SpecialInstructions
            });
        }

        var taxableAmount = Math.Max(subtotal - order.DiscountAmount, 0);
        order.Subtotal = subtotal;
        order.TaxAmount = Math.Round(taxableAmount * TaxRate, 2);
        order.TotalAmount = taxableAmount + order.TaxAmount;

        var kotNumber = await _orders.GetNextKotNumberAsync(tenant.Id, ct);
        order.KOTs.Add(new KOT
        {
            TenantId = tenant.Id,
            KotNumber = kotNumber,
            ItemsJson = JsonSerializer.Serialize(kotItems),
            Status = KotStatus.New
        });

        table.Status = TableStatus.Occupied;
        table.UpdatedAt = DateTime.UtcNow;
        await _tables.UpdateAsync(table, ct);

        await _orders.AddAsync(order, ct);
        var created = await _orders.GetByIdAsync(order.Id, tenant.Id, ct);
        return await ResponseFactory.CreatedAsync(req, MapOrder(created!));
    }

    [Function("GetPublicOrder")]
    [OpenApiOperation(operationId: "GetPublicOrder", tags: new[] { "Public" }, Summary = "Get public order")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<OrderResponse>))]
    public async Task<HttpResponseData> GetOrder(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/orders/{id:guid}")] HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var tenant = await ResolveTenantAsync(req, ct);
        if (tenant is null) return await ResponseFactory.NotFoundAsync(req, "Cafe tenant not found.");

        var order = await _orders.GetByIdAsync(id, tenant.Id, ct);
        if (order is null) return await ResponseFactory.NotFoundAsync(req, "Order not found.");
        return await ResponseFactory.OkAsync(req, MapOrder(order));
    }

    [Function("GetPublicBilling")]
    [OpenApiOperation(operationId: "GetPublicBilling", tags: new[] { "Public" }, Summary = "Get public billing summary")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<PublicBillingResponse>))]
    public async Task<HttpResponseData> GetBilling(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/billing/{orderId:guid}")] HttpRequestData req,
        Guid orderId,
        CancellationToken ct)
    {
        var tenant = await ResolveTenantAsync(req, ct);
        if (tenant is null) return await ResponseFactory.NotFoundAsync(req, "Cafe tenant not found.");

        var order = await _orders.GetByIdAsync(orderId, tenant.Id, ct);
        if (order is null) return await ResponseFactory.NotFoundAsync(req, "Order not found.");

        var payments = await _payments.GetByOrderAsync(orderId, tenant.Id, ct);
        var invoice = await _invoices.GetByOrderIdAsync(orderId, tenant.Id, ct);
        var paidAmount = payments.Where(p => p.Status == PaymentStatus.Success).Sum(p => p.Amount);
        var dueAmount = Math.Max(order.TotalAmount - paidAmount, 0);

        return await ResponseFactory.OkAsync(req, new PublicBillingResponse
        {
            OrderId = order.Id,
            InvoiceNumber = invoice?.InvoiceNumber,
            Items = MapOrder(order).Items,
            Subtotal = order.Subtotal,
            TaxAmount = order.TaxAmount,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            PaidAmount = paidAmount,
            DueAmount = dueAmount,
            PaymentStatus = dueAmount <= 0 ? "paid" : paidAmount > 0 ? "partial" : "unpaid",
            Payments = payments.Select(MapPayment).ToList()
        });
    }

    private async Task<Tenant?> ResolveTenantAsync(HttpRequestData req, CancellationToken ct)
    {
        var tenantKey = req.Headers.FirstOrDefault(h =>
            string.Equals(h.Key, "X-Tenant-Id", StringComparison.OrdinalIgnoreCase)).Value?.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(tenantKey))
        {
            var qs = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            tenantKey = qs["tenant"];
        }

        tenantKey = string.IsNullOrWhiteSpace(tenantKey) ? DefaultTenantSlug : tenantKey.Trim();

        var tenant = Guid.TryParse(tenantKey, out var id)
            ? await _tenants.GetByIdAsync(id, ct)
            : await _tenants.GetBySlugAsync(tenantKey.ToLowerInvariant(), ct);

        return tenant?.IsActive == true ? tenant : null;
    }

    private static CategoryResponse MapCategory(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Icon = c.Icon,
        DisplayOrder = c.DisplayOrder,
        IsActive = c.IsActive
    };

    private static MenuItemResponse MapMenuItem(MenuItem m) => new()
    {
        Id = m.Id,
        CategoryId = m.CategoryId,
        CategoryName = m.Category?.Name ?? string.Empty,
        Name = m.Name,
        Description = m.Description,
        BasePrice = m.BasePrice,
        ImageUrl = m.ImageUrl,
        IsAvailable = m.IsAvailable,
        IsVeg = m.IsVeg,
        PrepTimeMinutes = m.PrepTimeMinutes,
        Variants = m.Variants.Select(v => new VariantResponse
        {
            Id = v.Id,
            Name = v.Name,
            PriceModifier = v.PriceModifier,
            IsAvailable = v.IsAvailable
        }).ToList(),
        Addons = m.Addons.Select(a => new AddonResponse
        {
            Id = a.Id,
            Name = a.Name,
            ExtraPrice = a.ExtraPrice,
            IsAvailable = a.IsAvailable
        }).ToList()
    };

    private static TableResponse MapTable(CafeTable t) => new()
    {
        Id = t.Id,
        TableNumber = t.TableNumber,
        QrCodeUrl = t.QrCodeUrl,
        Status = t.Status,
        Capacity = t.Capacity,
        CreatedAt = t.CreatedAt
    };

    private static CustomerResponse MapCustomer(Customer c) => new()
    {
        Id = c.Id,
        Phone = c.Phone,
        Name = c.Name,
        Email = c.Email,
        Birthday = c.Birthday,
        Anniversary = c.Anniversary,
        SpecialEventDate = c.SpecialEventDate,
        SpecialEventName = c.SpecialEventName,
        VisitCount = c.VisitCount,
        TotalSpent = c.TotalSpent,
        LastVisit = c.LastVisit,
        CreatedAt = c.CreatedAt
    };

    private static OrderResponse MapOrder(Order o) => new()
    {
        Id = o.Id,
        TableId = o.TableId,
        TableNumber = o.Table?.TableNumber ?? 0,
        CustomerId = o.CustomerId,
        CustomerName = o.Customer?.Name,
        CustomerPhone = o.Customer?.Phone,
        PlacedByStaffId = o.PlacedByStaffId,
        PlacedByStaffName = o.PlacedByStaff?.Name,
        Status = o.Status,
        OrderType = o.OrderType,
        Subtotal = o.Subtotal,
        TaxAmount = o.TaxAmount,
        DiscountAmount = o.DiscountAmount,
        TotalAmount = o.TotalAmount,
        SpecialInstructions = o.SpecialInstructions,
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt,
        Items = o.Items.Select(i => new OrderItemResponse
        {
            Id = i.Id,
            MenuItemId = i.MenuItemId,
            MenuItemName = i.MenuItem?.Name ?? string.Empty,
            VariantId = i.VariantId,
            VariantName = i.Variant?.Name,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            AddonsJson = i.AddonsJson,
            SpecialInstructions = i.SpecialInstructions,
            ItemStatus = i.ItemStatus
        }).ToList()
    };

    private static PaymentResponse MapPayment(Payment p) => new()
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
