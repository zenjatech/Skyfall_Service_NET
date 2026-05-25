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

public sealed class MenuFunction
{
    private readonly IMenuRepository _menu;
    private readonly JwtHelper _jwt;
    private readonly ILogger<MenuFunction> _logger;

    public MenuFunction(IMenuRepository menu, JwtHelper jwt, ILogger<MenuFunction> logger)
    {
        _menu = menu;
        _jwt = jwt;
        _logger = logger;
    }

    // ── Categories ──────────────────────────────────────────────────────────

    [Function("GetCategories")]
    [OpenApiOperation(operationId: "GetCategories", tags: new[] { "Menu" }, Summary = "List categories")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<List<CategoryResponse>>))]
    public async Task<HttpResponseData> GetCategories(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu/categories")] HttpRequestData req,
        CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt);
        if (error is not null) return await error;

        var list = await _menu.GetCategoriesAsync(tenantId, ct);
        return await ResponseFactory.OkAsync(req, list.Select(MapCategory).ToList());
    }

    [Function("CreateCategory")]
    [OpenApiOperation(operationId: "CreateCategory", tags: new[] { "Menu" }, Summary = "Create category")]
    [OpenApiRequestBody("application/json", typeof(CategoryCreateRequest), Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.Created, "application/json", typeof(ApiResponse<CategoryResponse>))]
    public async Task<HttpResponseData> CreateCategory(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu/categories")] HttpRequestData req,
        CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt, StaffRoles.Admin);
        if (error is not null) return await error;

        var body = await req.ReadFromJsonAsync<CategoryCreateRequest>(cancellationToken: ct);
        if (body is null || string.IsNullOrWhiteSpace(body.Name))
            return await ResponseFactory.BadRequestAsync(req, "Name is required.");

        var entity = new Category { TenantId = tenantId, Name = body.Name.Trim(), Icon = body.Icon, DisplayOrder = body.DisplayOrder };
        await _menu.AddCategoryAsync(entity, ct);
        return await ResponseFactory.CreatedAsync(req, MapCategory(entity));
    }

    [Function("UpdateCategory")]
    [OpenApiOperation(operationId: "UpdateCategory", tags: new[] { "Menu" }, Summary = "Update category")]
    [OpenApiRequestBody("application/json", typeof(CategoryUpdateRequest), Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<CategoryResponse>))]
    public async Task<HttpResponseData> UpdateCategory(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "menu/categories/{id:guid}")] HttpRequestData req,
        Guid id, CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt, StaffRoles.Admin);
        if (error is not null) return await error;

        var entity = await _menu.GetCategoryByIdAsync(id, tenantId, ct);
        if (entity is null) return await ResponseFactory.NotFoundAsync(req, "Category not found.");

        var body = await req.ReadFromJsonAsync<CategoryUpdateRequest>(cancellationToken: ct);
        if (body is null) return await ResponseFactory.BadRequestAsync(req, "Invalid request body.");

        if (body.Name is not null) entity.Name = body.Name.Trim();
        if (body.Icon is not null) entity.Icon = body.Icon;
        if (body.DisplayOrder.HasValue) entity.DisplayOrder = body.DisplayOrder.Value;
        if (body.IsActive.HasValue) entity.IsActive = body.IsActive.Value;
        entity.UpdatedAt = DateTime.UtcNow;

        await _menu.UpdateCategoryAsync(entity, ct);
        return await ResponseFactory.OkAsync(req, MapCategory(entity));
    }

    [Function("DeleteCategory")]
    [OpenApiOperation(operationId: "DeleteCategory", tags: new[] { "Menu" }, Summary = "Delete category")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NoContent)]
    public async Task<HttpResponseData> DeleteCategory(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "menu/categories/{id:guid}")] HttpRequestData req,
        Guid id, CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt, StaffRoles.Admin);
        if (error is not null) return await error;

        var deleted = await _menu.DeleteCategoryAsync(id, tenantId, ct);
        if (!deleted) return await ResponseFactory.NotFoundAsync(req, "Category not found.");
        return await ResponseFactory.NoContentAsync(req);
    }

    // ── Menu Items ───────────────────────────────────────────────────────────

    [Function("GetMenuItems")]
    [OpenApiOperation(operationId: "GetMenuItems", tags: new[] { "Menu" }, Summary = "List menu items")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<List<MenuItemResponse>>))]
    public async Task<HttpResponseData> GetMenuItems(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu/items")] HttpRequestData req,
        CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt);
        if (error is not null) return await error;

        var qs = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        Guid? categoryId = Guid.TryParse(qs["categoryId"], out var cid) ? cid : null;

        var list = await _menu.GetMenuItemsAsync(tenantId, categoryId, ct);
        return await ResponseFactory.OkAsync(req, list.Select(MapMenuItem).ToList());
    }

    [Function("GetMenuItemById")]
    [OpenApiOperation(operationId: "GetMenuItemById", tags: new[] { "Menu" }, Summary = "Get menu item by id")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<MenuItemResponse>))]
    public async Task<HttpResponseData> GetMenuItemById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu/items/{id:guid}")] HttpRequestData req,
        Guid id, CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt);
        if (error is not null) return await error;

        var entity = await _menu.GetMenuItemByIdAsync(id, tenantId, ct);
        if (entity is null) return await ResponseFactory.NotFoundAsync(req, "Menu item not found.");
        return await ResponseFactory.OkAsync(req, MapMenuItem(entity));
    }

    [Function("CreateMenuItem")]
    [OpenApiOperation(operationId: "CreateMenuItem", tags: new[] { "Menu" }, Summary = "Create menu item")]
    [OpenApiRequestBody("application/json", typeof(MenuItemCreateRequest), Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.Created, "application/json", typeof(ApiResponse<MenuItemResponse>))]
    public async Task<HttpResponseData> CreateMenuItem(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu/items")] HttpRequestData req,
        CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt, StaffRoles.Admin);
        if (error is not null) return await error;

        var body = await req.ReadFromJsonAsync<MenuItemCreateRequest>(cancellationToken: ct);
        if (body is null || string.IsNullOrWhiteSpace(body.Name))
            return await ResponseFactory.BadRequestAsync(req, "Name is required.");

        var category = await _menu.GetCategoryByIdAsync(body.CategoryId, tenantId, ct);
        if (category is null) return await ResponseFactory.BadRequestAsync(req, "Category not found.");

        var entity = new MenuItem
        {
            TenantId = tenantId,
            CategoryId = body.CategoryId,
            Name = body.Name.Trim(),
            Description = body.Description,
            BasePrice = body.BasePrice,
            ImageUrl = body.ImageUrl,
            IsVeg = body.IsVeg,
            PrepTimeMinutes = body.PrepTimeMinutes
        };
        await _menu.AddMenuItemAsync(entity, ct);
        return await ResponseFactory.CreatedAsync(req, MapMenuItem(entity));
    }

    [Function("UpdateMenuItem")]
    [OpenApiOperation(operationId: "UpdateMenuItem", tags: new[] { "Menu" }, Summary = "Update menu item")]
    [OpenApiRequestBody("application/json", typeof(MenuItemUpdateRequest), Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<MenuItemResponse>))]
    public async Task<HttpResponseData> UpdateMenuItem(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "menu/items/{id:guid}")] HttpRequestData req,
        Guid id, CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt, StaffRoles.Admin);
        if (error is not null) return await error;

        var entity = await _menu.GetMenuItemByIdAsync(id, tenantId, ct);
        if (entity is null) return await ResponseFactory.NotFoundAsync(req, "Menu item not found.");

        var body = await req.ReadFromJsonAsync<MenuItemUpdateRequest>(cancellationToken: ct);
        if (body is null) return await ResponseFactory.BadRequestAsync(req, "Invalid request body.");

        if (body.Name is not null) entity.Name = body.Name.Trim();
        if (body.Description is not null) entity.Description = body.Description;
        if (body.BasePrice.HasValue) entity.BasePrice = body.BasePrice.Value;
        if (body.ImageUrl is not null) entity.ImageUrl = body.ImageUrl;
        if (body.IsAvailable.HasValue) entity.IsAvailable = body.IsAvailable.Value;
        if (body.IsVeg.HasValue) entity.IsVeg = body.IsVeg.Value;
        if (body.PrepTimeMinutes.HasValue) entity.PrepTimeMinutes = body.PrepTimeMinutes.Value;
        if (body.CategoryId.HasValue) entity.CategoryId = body.CategoryId.Value;
        entity.UpdatedAt = DateTime.UtcNow;

        await _menu.UpdateMenuItemAsync(entity, ct);
        return await ResponseFactory.OkAsync(req, MapMenuItem(entity));
    }

    [Function("DeleteMenuItem")]
    [OpenApiOperation(operationId: "DeleteMenuItem", tags: new[] { "Menu" }, Summary = "Delete menu item")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NoContent)]
    public async Task<HttpResponseData> DeleteMenuItem(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "menu/items/{id:guid}")] HttpRequestData req,
        Guid id, CancellationToken ct)
    {
        var (_, tenantId, error) = AuthHelper.Authorize(req, _jwt, StaffRoles.Admin);
        if (error is not null) return await error;

        var deleted = await _menu.DeleteMenuItemAsync(id, tenantId, ct);
        if (!deleted) return await ResponseFactory.NotFoundAsync(req, "Menu item not found.");
        return await ResponseFactory.NoContentAsync(req);
    }

    private static CategoryResponse MapCategory(Category c) => new()
    {
        Id = c.Id, Name = c.Name, Icon = c.Icon, DisplayOrder = c.DisplayOrder, IsActive = c.IsActive
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
        Variants = m.Variants.Select(v => new VariantResponse { Id = v.Id, Name = v.Name, PriceModifier = v.PriceModifier, IsAvailable = v.IsAvailable }).ToList(),
        Addons = m.Addons.Select(a => new AddonResponse { Id = a.Id, Name = a.Name, ExtraPrice = a.ExtraPrice, IsAvailable = a.IsAvailable }).ToList()
    };
}
