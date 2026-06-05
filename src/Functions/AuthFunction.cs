using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Skyfall.Common;
using Skyfall.Contracts.Requests;
using Skyfall.Contracts.Responses;
using Skyfall.Infrastructure.Repositories;

namespace Skyfall.Functions;

public sealed class AuthFunction
{
    private readonly IStaffRepository _staff;
    private readonly ITenantRepository _tenants;
    private readonly JwtHelper _jwt;
    private readonly IOptions<JwtOptions> _jwtOptions;
    private readonly ILogger<AuthFunction> _logger;

    public AuthFunction(IStaffRepository staff, ITenantRepository tenants, JwtHelper jwt, IOptions<JwtOptions> jwtOptions, ILogger<AuthFunction> logger)
    {
        _staff = staff;
        _tenants = tenants;
        _jwt = jwt;
        _jwtOptions = jwtOptions;
        _logger = logger;
    }

    [Function("Login")]
    [OpenApiOperation(operationId: "Login", tags: new[] { "Auth" }, Summary = "Authenticate staff member")]
    [OpenApiRequestBody("application/json", typeof(LoginRequest), Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(ApiResponse<AuthResponse>))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(ApiResponse))]
    public async Task<HttpResponseData> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequestData req,
        CancellationToken ct)
    {
        var correlationId = Guid.NewGuid();
        _logger.LogInformation("CorrelationId {CorrelationId} - Login started", correlationId);

        try
        {
            var body = await req.ReadFromJsonAsync<LoginRequest>(cancellationToken: ct);
            if (body is null || string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
                return await ResponseFactory.BadRequestAsync(req, "Email and password are required.");

            var tenantIdHeader = req.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "X-Tenant-Id", StringComparison.OrdinalIgnoreCase)).Value?.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(tenantIdHeader))
                return await ResponseFactory.BadRequestAsync(req, "X-Tenant-Id header is required.");

            Guid tenantId;
            if (Guid.TryParse(tenantIdHeader, out var parsedGuid))
            {
                tenantId = parsedGuid;
            }
            else
            {
                var tenant = await _tenants.GetBySlugAsync(tenantIdHeader.Trim().ToLowerInvariant(), ct);
                if (tenant is null)
                    return await ResponseFactory.NotFoundAsync(req, $"Tenant '{tenantIdHeader}' not found.");
                tenantId = tenant.Id;
            }

            var staff = await _staff.GetByEmailAsync(body.Email.Trim().ToLowerInvariant(), tenantId, ct);
            if (staff is null || !staff.IsActive || !BCrypt.Net.BCrypt.Verify(body.Password, staff.PasswordHash))
            {
                _logger.LogWarning("CorrelationId {CorrelationId} - Invalid credentials for {Email}", correlationId, body.Email);
                return await ResponseFactory.UnauthorizedAsync(req, "Invalid credentials.");
            }

            staff.LastLoginAt = DateTime.UtcNow;
            await _staff.UpdateAsync(staff, ct);

            var expiryMinutes = _jwtOptions.Value.ExpiryMinutes;
            var token = _jwt.GenerateToken(staff.Id, staff.Role, tenantId);
            var response = new AuthResponse
            {
                Token = token,
                StaffId = staff.Id,
                Name = staff.Name,
                Role = staff.Role,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
            };

            _logger.LogInformation("CorrelationId {CorrelationId} - Login successful for {Email}", correlationId, body.Email);
            return await ResponseFactory.OkAsync(req, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CorrelationId {CorrelationId} - Login failed", correlationId);
            throw;
        }
    }
}
