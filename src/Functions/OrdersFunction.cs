using System.Net;
using System.Text.Json;
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

public sealed class OrdersFunction
{
    private const decimal TaxRate = 0.05m;

    private readonly IOrderRepository _orders;
    private readonly IMenuRepository _menu;
    private readonly ITableRepository _tables;
    private readonly JwtHelper _jwt;
    private readonly ILogger<OrdersFunction> _logger;

    public OrdersFunction(IOrderRepository orders, IMenuRepository menu, ITableRepository tables, JwtHelper jwt, ILogger<OrdersFunction> logger)
    {
        _orders = orders;
        _menu = menu;
        _tables = tables;
        _jwt = jwt;
        _logger = logger;
    }

    [Function("GetOrders")]
    [OpenApiOperation(operationId: "GetOrders", tags: new[] { "Orders" }, Summary = "List orders")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<List<OrderResponse>>))]
    public async Task<HttpResponseData> GetAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders")] HttpRequestData req,
        CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt);
        if (error is not null) return await error;

        var qs = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var status = qs["status"];
        var list = await _orders.GetAllAsync(tenantId, status, ct);
        return await ResponseFactory.OkAsync(req, list.Select(MapToResponse).ToList());
    }

    [Function("GetOrderById")]
    [OpenApiOperation(operationId: "GetOrderById", tags: new[] { "Orders" }, Summary = "Get order by id")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<OrderResponse>))]
    public async Task<HttpResponseData> GetById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{id:guid}")] HttpRequestData req,
        Guid id, CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt);
        if (error is not null) return await error;

        var entity = await _orders.GetByIdAsync(id, tenantId, ct);
        if (entity is null) return await ResponseFactory.NotFoundAsync(req, "Order not found.");
        return await ResponseFactory.OkAsync(req, MapToResponse(entity));
    }

    [Function("CreateOrder")]
    [OpenApiOperation(operationId: "CreateOrder", tags: new[] { "Orders" }, Summary = "Create order")]
    [OpenApiRequestBody("application/json", typeof(OrderCreateRequest), Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.Created, "application/json", typeof(ApiResponse<OrderResponse>))]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")] HttpRequestData req,
        CancellationToken ct)
    {
        var (principal, tenantId, error) = AuthHelper.Authorize(req, _jwt);
        if (error is not null) return await error;

        var body = await req.ReadFromJsonAsync<OrderCreateRequest>(cancellationToken: ct);
        if (body is null || body.Items.Count == 0)
            return await ResponseFactory.BadRequestAsync(req, "At least one item is required.");

        var table = await _tables.GetByIdAsync(body.TableId, tenantId, ct);
        if (table is null) return await ResponseFactory.BadRequestAsync(req, "Table not found.");

        var staffId = JwtHelper.GetStaffId(principal!);
        var order = new Order
        {
            TenantId = tenantId,
            TableId = body.TableId,
            CustomerId = body.CustomerId,
            PlacedByStaffId = staffId,
            OrderType = body.OrderType,
            SpecialInstructions = body.SpecialInstructions
        };

        decimal subtotal = 0;
        var kotItems = new List<object>();

        foreach (var itemReq in body.Items)
        {
            var menuItem = await _menu.GetMenuItemByIdAsync(itemReq.MenuItemId, tenantId, ct);
            if (menuItem is null) return await ResponseFactory.BadRequestAsync(req, $"Menu item {itemReq.MenuItemId} not found.");

            decimal unitPrice = menuItem.BasePrice;
            string? variantName = null;

            if (itemReq.VariantId.HasValue)
            {
                var variant = menuItem.Variants.FirstOrDefault(v => v.Id == itemReq.VariantId.Value);
                if (variant is null) return await ResponseFactory.BadRequestAsync(req, "Variant not found.");
                unitPrice += variant.PriceModifier;
                variantName = variant.Name;
            }

            var addonNames = new List<string>();
            foreach (var addonId in itemReq.AddonIds)
            {
                var addon = menuItem.Addons.FirstOrDefault(a => a.Id == addonId);
                if (addon is null) return await ResponseFactory.BadRequestAsync(req, "Addon not found.");
                unitPrice += addon.ExtraPrice;
                addonNames.Add(addon.Name);
            }

            subtotal += unitPrice * itemReq.Quantity;
            var addonsJson = itemReq.AddonIds.Count > 0 ? JsonSerializer.Serialize(addonNames) : null;

            order.Items.Add(new OrderItem
            {
                TenantId = tenantId,
                MenuItemId = itemReq.MenuItemId,
                VariantId = itemReq.VariantId,
                Quantity = itemReq.Quantity,
                UnitPrice = unitPrice,
                AddonsJson = addonsJson,
                SpecialInstructions = itemReq.SpecialInstructions
            });

            kotItems.Add(new { menuItem.Name, Variant = variantName, Addons = addonNames, itemReq.Quantity, itemReq.SpecialInstructions });
        }

        var taxableAmount = Math.Max(subtotal - order.DiscountAmount, 0);
        order.Subtotal = subtotal;
        order.TaxAmount = Math.Round(taxableAmount * TaxRate, 2);
        order.TotalAmount = taxableAmount + order.TaxAmount;
        order.Status = OrderStatus.Confirmed;

        var kotNumber = await _orders.GetNextKotNumberAsync(tenantId, ct);
        order.KOTs.Add(new KOT
        {
            TenantId = tenantId,
            KotNumber = kotNumber,
            ItemsJson = JsonSerializer.Serialize(kotItems),
            Status = KotStatus.New
        });

        table.Status = TableStatus.Occupied;
        table.UpdatedAt = DateTime.UtcNow;
        await _tables.UpdateAsync(table, ct);

        await _orders.AddAsync(order, ct);
        var created = await _orders.GetByIdAsync(order.Id, tenantId, ct);
        return await ResponseFactory.CreatedAsync(req, MapToResponse(created!));
    }

    [Function("UpdateOrderStatus")]
    [OpenApiOperation(operationId: "UpdateOrderStatus", tags: new[] { "Orders" }, Summary = "Update order status")]
    [OpenApiRequestBody("application/json", typeof(OrderStatusUpdateRequest), Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<OrderResponse>))]
    public async Task<HttpResponseData> UpdateStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "orders/{id:guid}/status")] HttpRequestData req,
        Guid id, CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt);
        if (error is not null) return await error;

        var entity = await _orders.GetByIdAsync(id, tenantId, ct);
        if (entity is null) return await ResponseFactory.NotFoundAsync(req, "Order not found.");

        var body = await req.ReadFromJsonAsync<OrderStatusUpdateRequest>(cancellationToken: ct);
        if (body is null) return await ResponseFactory.BadRequestAsync(req, "Invalid request body.");

        if (!OrderStatus.CanTransitionTo(entity.Status, body.Status))
            return await ResponseFactory.UnprocessableAsync(req, $"Cannot transition from '{entity.Status}' to '{body.Status}'.");

        entity.Status = body.Status;
        entity.UpdatedAt = DateTime.UtcNow;

        if (body.Status is OrderStatus.Served or OrderStatus.Cancelled)
        {
            var activeOrders = await _orders.GetByTableAsync(entity.TableId, tenantId, ct);
            var stillActive = activeOrders.Any(o => o.Id != entity.Id &&
                o.Status is not OrderStatus.Served and not OrderStatus.Cancelled);
            if (!stillActive)
            {
                var table = await _tables.GetByIdAsync(entity.TableId, tenantId, ct);
                if (table is not null)
                {
                    table.Status = TableStatus.Free;
                    table.UpdatedAt = DateTime.UtcNow;
                    await _tables.UpdateAsync(table, ct);
                }
            }
        }

        await _orders.UpdateAsync(entity, ct);
        return await ResponseFactory.OkAsync(req, MapToResponse(entity));
    }

    [Function("AddOrderItems")]
    [OpenApiOperation(operationId: "AddOrderItems", tags: new[] { "Orders" }, Summary = "Add items to existing order")]
    [OpenApiRequestBody("application/json", typeof(OrderAddItemsRequest), Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<OrderResponse>))]
    public async Task<HttpResponseData> AddItems(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders/{id:guid}/items")] HttpRequestData req,
        Guid id, CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt);
        if (error is not null) return await error;

        var entity = await _orders.GetByIdAsync(id, tenantId, ct);
        if (entity is null) return await ResponseFactory.NotFoundAsync(req, "Order not found.");
        if (entity.Status is OrderStatus.Served or OrderStatus.Cancelled)
            return await ResponseFactory.UnprocessableAsync(req, "Cannot add items to a closed order.");

        var body = await req.ReadFromJsonAsync<OrderAddItemsRequest>(cancellationToken: ct);
        if (body is null || body.Items.Count == 0)
            return await ResponseFactory.BadRequestAsync(req, "At least one item is required.");

        var kotItems = new List<object>();
        decimal addedAmount = 0;

        foreach (var itemReq in body.Items)
        {
            var menuItem = await _menu.GetMenuItemByIdAsync(itemReq.MenuItemId, tenantId, ct);
            if (menuItem is null) return await ResponseFactory.BadRequestAsync(req, $"Menu item {itemReq.MenuItemId} not found.");

            decimal unitPrice = menuItem.BasePrice;
            string? variantName = null;
            var addonNames = new List<string>();

            if (itemReq.VariantId.HasValue)
            {
                var variant = menuItem.Variants.FirstOrDefault(v => v.Id == itemReq.VariantId.Value);
                if (variant is not null) { unitPrice += variant.PriceModifier; variantName = variant.Name; }
            }

            foreach (var addonId in itemReq.AddonIds)
            {
                var addon = menuItem.Addons.FirstOrDefault(a => a.Id == addonId);
                if (addon is not null) { unitPrice += addon.ExtraPrice; addonNames.Add(addon.Name); }
            }

            addedAmount += unitPrice * itemReq.Quantity;
            entity.Items.Add(new OrderItem
            {
                TenantId = tenantId,
                OrderId = entity.Id,
                MenuItemId = itemReq.MenuItemId,
                VariantId = itemReq.VariantId,
                Quantity = itemReq.Quantity,
                UnitPrice = unitPrice,
                AddonsJson = addonNames.Count > 0 ? JsonSerializer.Serialize(addonNames) : null,
                SpecialInstructions = itemReq.SpecialInstructions
            });
            kotItems.Add(new { menuItem.Name, Variant = variantName, Addons = addonNames, itemReq.Quantity, itemReq.SpecialInstructions });
        }

        entity.Subtotal += addedAmount;
        var taxable = Math.Max(entity.Subtotal - entity.DiscountAmount, 0);
        entity.TaxAmount = Math.Round(taxable * TaxRate, 2);
        entity.TotalAmount = taxable + entity.TaxAmount;
        entity.UpdatedAt = DateTime.UtcNow;

        var kotNumber = await _orders.GetNextKotNumberAsync(tenantId, ct);
        entity.KOTs.Add(new KOT
        {
            TenantId = tenantId,
            OrderId = entity.Id,
            KotNumber = kotNumber,
            ItemsJson = JsonSerializer.Serialize(kotItems),
            Status = KotStatus.New
        });

        await _orders.UpdateAsync(entity, ct);
        return await ResponseFactory.OkAsync(req, MapToResponse(entity));
    }

    private static OrderResponse MapToResponse(Order o) => new()
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
}
