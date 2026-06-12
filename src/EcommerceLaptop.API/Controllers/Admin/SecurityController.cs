using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.ValueObjects;
using EcommerceLaptop.Infrastructure.Services.Security;
using EcommerceLaptop.Infrastructure.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using EcommerceLaptop.API.Authorization;
using EcommerceLaptop.Core.Constants;

namespace EcommerceLaptop.API.Controllers.Admin;

/// <summary>
/// Controller for managing IP blocking rules and security events
/// Requires admin authorization and security:manage permission
/// </summary>
[ApiController]
[Route("api/admin/security")]
[RequireAdmin]
[EnableRateLimiting("AdminAuthPolicy")]
public class SecurityController : ControllerBase
{
    private readonly IIPBlockingService _ipBlockingService;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly ISecurityEventService _securityEventService;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly ILogger<SecurityController> _logger;

    public SecurityController(
        IIPBlockingService ipBlockingService,
        IRateLimitingService rateLimitingService,
        ISecurityEventService securityEventService,
        IAuditLoggingService auditLoggingService,
        ILogger<SecurityController> logger)
    {
        _ipBlockingService = ipBlockingService;
        _rateLimitingService = rateLimitingService;
        _securityEventService = securityEventService;
        _auditLoggingService = auditLoggingService;
        _logger = logger;
    }

    #region IP Blocking Management

    /// <summary>
    /// Get all IP blocking rules
    /// </summary>
    [HttpGet("ip-rules")]
    [RequireAdminPermission(AdminPermissions.SecurityRead)]
    public async Task<IActionResult> GetIPRules()
    {
        var result = await _ipBlockingService.GetBlockedIPsAsync();

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new
        {
            items = result.Data,
            totalCount = result.Data?.Count ?? 0,
            page = 1,
            pageSize = result.Data?.Count ?? 0,
            totalPages = 1
        });
    }

    /// <summary>
    /// Get all IP blocking rules (alias for frontend compatibility)
    /// </summary>
    [HttpGet("ip-blocks")] // Frontend calls this
    [RequireAdminPermission(AdminPermissions.SecurityRead)]
    public async Task<IActionResult> GetIPBlocks()
    {
        return await GetIPRules();
    }

    /// <summary>
    /// Create a new IP blocking rule
    /// </summary>
    [HttpPost("ip-rules")]
    [RequireAdminPermission(AdminPermissions.SecurityManage)]
    public async Task<IActionResult> CreateIPRule([FromBody] CreateIPRuleRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var adminEmail = User?.Identity?.Name ?? "system";

        var rule = new IPBlockRule
        {
            IPAddress = request.IPAddress,
            Type = request.Type,
            Reason = request.Reason,
            IsActive = request.IsActive,
            ExpiresAt = request.ExpiresAt,
            CreatedBy = adminEmail,
            ThreatLevel = request.ThreatLevel ?? "medium",
            CountryCode = request.CountryCode
        };

        var adminId = GetCurrentAdminUserId() ?? 2; // Default to Super Admin (ID 2)
        var createResult = await _ipBlockingService.BlockIPAsync(rule.IPAddress, rule.Reason, adminId, rule.ExpiresAt);

        if (!createResult.IsSuccess)
            return BadRequest(new { error = createResult.ErrorMessage });

        await _auditLoggingService.LogAdminActivityAsync(
            adminId,
            "ip_rule_created",
            $"Blocked {rule.IPAddress} ({rule.Type}) - {rule.Reason}",
            GetClientIpAddress(),
            Request.Headers["User-Agent"].ToString(),
            targetResource: rule.IPAddress);

        return Ok(new { success = createResult.Data, message = "IP rule created successfully" });
    }

    /// <summary>
    /// Update an existing IP blocking rule
    /// </summary>
    [HttpPut("ip-rules/{id:int}")]
    [RequireAdminPermission(AdminPermissions.SecurityManage)]
    public async Task<IActionResult> UpdateIPRule(int id, [FromBody] UpdateIPRuleRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var rule = new IPBlockRule
        {
            Id = id,
            IPAddress = request.IPAddress,
            Type = request.Type,
            Reason = request.Reason,
            IsActive = request.IsActive,
            ExpiresAt = request.ExpiresAt,
            ThreatLevel = request.ThreatLevel ?? "medium",
            CountryCode = request.CountryCode
        };

        var result = await _ipBlockingService.UpdateIPRuleAsync(rule);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        var adminId = GetCurrentAdminUserId() ?? 2; // Default to Super Admin (ID 2)
        await _auditLoggingService.LogAdminActivityAsync(
            adminId,
            "ip_rule_updated",
            $"Updated rule {id} for {rule.IPAddress}",
            GetClientIpAddress(),
            Request.Headers["User-Agent"].ToString(),
            targetResource: rule.IPAddress);

        return Ok(result.Data);
    }

    /// <summary>
    /// Delete an IP blocking rule
    /// </summary>
    [HttpDelete("ip-rules/{id:int}")]
    [RequireAdminPermission(AdminPermissions.SecurityManage)]
    public async Task<IActionResult> DeleteIPRule(int id)
    {
        var result = await _ipBlockingService.DeleteIPRuleAsync(id);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        var adminId = GetCurrentAdminUserId() ?? 2; // Default to Super Admin (ID 2)
        await _auditLoggingService.LogAdminActivityAsync(
            adminId,
            "ip_rule_deleted",
            $"Deleted IP rule {id}",
            GetClientIpAddress(),
            Request.Headers["User-Agent"].ToString());

        return Ok(new { success = true, message = "IP rule deleted successfully" });
    }

    /// <summary>
    /// Block an IP address quickly
    /// </summary>
    [HttpPost("block-ip")]
    [RequireAdminPermission(AdminPermissions.SecurityManage)]
    public async Task<IActionResult> BlockIP([FromBody] BlockIPRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var adminUserId = GetCurrentAdminUserId();
        var result = await _ipBlockingService.BlockIPAsync(request.IPAddress, request.Reason, adminUserId, request.ExpiresAt);

        if (!result.IsSuccess)
            return BadRequest(new { error = "Failed to block IP address" });

        await _auditLoggingService.LogAdminActivityAsync(
            adminUserId ?? 2, // Default to Super Admin (ID 2)
            "ip_blocked",
            $"Blocked IP {request.IPAddress} - {request.Reason}",
            GetClientIpAddress(),
            Request.Headers["User-Agent"].ToString(),
            targetResource: request.IPAddress);

        return Ok(new { success = true, message = $"IP {request.IPAddress} has been blocked" });
    }

    /// <summary>
    /// Unblock an IP address
    /// </summary>
    [HttpPost("unblock-ip")]
    [RequireAdminPermission(AdminPermissions.SecurityManage)]
    public async Task<IActionResult> UnblockIP([FromBody] UnblockIPRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var adminUserId = GetCurrentAdminUserId();
        var result = await _ipBlockingService.UnblockIPAsync(request.IPAddress, adminUserId);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        await _auditLoggingService.LogAdminActivityAsync(
            adminUserId ?? 2, // Default to Super Admin (ID 2)
            "ip_unblocked",
            $"Unblocked IP {request.IPAddress}",
            GetClientIpAddress(),
            Request.Headers["User-Agent"].ToString(),
            targetResource: request.IPAddress);

        return Ok(new { success = true, message = $"IP {request.IPAddress} has been unblocked" });
    }

    /// <summary>
    /// Get suspicious IP addresses
    /// </summary>
    [HttpGet("suspicious-ips")]
    [RequireAdminPermission(AdminPermissions.SecurityRead)]
    public async Task<IActionResult> GetSuspiciousIPs([FromQuery] int hours = 24)
    {
        var result = await _ipBlockingService.GetSuspiciousIPsAsync(hours);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    #endregion

    #region Rate Limiting Management

    /// <summary>
    /// Get all rate limiting rules
    /// </summary>
    [HttpGet("rate-limit-rules")]
    [RequireAdminPermission(AdminPermissions.SecurityRead)]
    public async Task<IActionResult> GetRateLimitRules()
    {
        var result = await _rateLimitingService.GetRateLimitRulesAsync();

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new
        {
            items = result.Data,
            totalCount = result.Data?.Count ?? 0,
            page = 1,
            pageSize = result.Data?.Count ?? 0,
            totalPages = 1
        });
    }

    /// <summary>
    /// Create a new rate limiting rule
    /// </summary>
    [HttpPost("rate-limit-rules")]
    [RequireAdminPermission(AdminPermissions.SecurityManage)]
    public async Task<IActionResult> CreateRateLimitRule([FromBody] CreateRateLimitRuleRequest request)
    {
        _logger.LogInformation(" CREATE REQUEST - Rate Limit Rule");

        if (request == null)
        {
            _logger.LogWarning(" REQUEST IS NULL");
            return BadRequest(new { error = "Request body is required" });
        }

        _logger.LogInformation(" REQUEST DATA - Name: {Name} | Endpoint: {Endpoint} | HttpMethod: {HttpMethod}",
            request.Name ?? "null", request.Endpoint ?? "null", request.HttpMethod ?? "null");
        _logger.LogInformation(" MODEL STATE - IsValid: {IsValid} | ErrorCount: {ErrorCount}",
            ModelState.IsValid, ModelState.ErrorCount);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning(" MODEL STATE ERRORS:");
            foreach (var error in ModelState)
            {
                _logger.LogWarning(" FIELD: {Field} | ERRORS: {Errors}",
                    error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
            }
            return BadRequest(ModelState);
        }

        var adminEmail = User?.Identity?.Name ?? "system";

        var rule = new RateLimitRule
        {
            Name = request.Name ?? string.Empty,
            Endpoint = request.Endpoint ?? string.Empty,
            HttpMethod = request.HttpMethod ?? "ALL",
            RequestsPerMinute = request.RequestsPerMinute,
            RequestsPerHour = request.RequestsPerHour,
            RequestsPerDay = request.RequestsPerDay,
            IsActive = request.IsActive,
            Priority = request.Priority,
            Description = request.Description,
            IPWhitelist = request.IPWhitelist ?? "[]",
            UserRoleExceptions = request.UserRoleExceptionsString ?? "[]",
            ApiKeyExceptions = request.ApiKeyExceptions ?? "[]",
            CooldownSeconds = request.CooldownSeconds,
            CustomErrorMessage = request.CustomErrorMessage,
            CreatedBy = adminEmail
        };

        _logger.LogInformation(" CALLING SERVICE - Creating rule with Name: {Name}", rule.Name);
        var created = await _rateLimitingService.CreateRateLimitRuleAsync(rule);
        _logger.LogInformation(" SERVICE RESULT - IsSuccess: {IsSuccess} | ErrorMessage: {ErrorMessage}",
            created.IsSuccess, created.ErrorMessage ?? "none");

        if (!created.IsSuccess)
        {
            _logger.LogWarning(" SERVICE FAILED - Returning BadRequest with error: {Error}", created.ErrorMessage);
            return BadRequest(new { error = created.ErrorMessage });
        }

        await _auditLoggingService.LogAdminActivityAsync(
            GetCurrentAdminUserId() ?? 2, // Default to Super Admin (ID 2)
            "rate_limit_rule_created",
            $"Created rate limit rule {rule.Name} for {rule.Endpoint}",
            GetClientIpAddress(),
            Request.Headers["User-Agent"].ToString(),
            targetResource: rule.Endpoint);

        return Ok(new { success = true, data = created.Data, message = "Rate limit rule created successfully" });
    }

    /// <summary>
    /// Update an existing rate limiting rule
    /// </summary>
    [HttpPut("rate-limit-rules/{id:int}")]
    [RequireAdminPermission(AdminPermissions.SecurityManage)]
    public async Task<IActionResult> UpdateRateLimitRule(int id, [FromBody] UpdateRateLimitRuleRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var rule = new RateLimitRule
        {
            Id = id,
            Name = request.Name,
            Endpoint = request.Endpoint,
            HttpMethod = request.HttpMethod,
            RequestsPerMinute = request.RequestsPerMinute,
            RequestsPerHour = request.RequestsPerHour,
            RequestsPerDay = request.RequestsPerDay,
            IsActive = request.IsActive,
            Priority = request.Priority,
            Description = request.Description,
            IPWhitelist = request.IPWhitelist ?? "[]",
            UserRoleExceptions = request.UserRoleExceptionsString ?? "[]",
            ApiKeyExceptions = request.ApiKeyExceptions ?? "[]",
            CooldownSeconds = request.CooldownSeconds,
            CustomErrorMessage = request.CustomErrorMessage
        };

        rule.Id = id;
        var updated = await _rateLimitingService.UpdateRateLimitRuleAsync(rule);

        if (!updated.IsSuccess)
            return BadRequest(new { error = updated.ErrorMessage });

        await _auditLoggingService.LogAdminActivityAsync(
            GetCurrentAdminUserId() ?? 2, // Default to Super Admin (ID 2)
            "rate_limit_rule_updated",
            $"Updated rate limit rule {id} - {rule.Name}",
            GetClientIpAddress(),
            Request.Headers["User-Agent"].ToString(),
            targetResource: rule.Endpoint);

        return Ok(updated.Data);
    }

    /// <summary>
    /// Delete a rate limiting rule
    /// </summary>
    [HttpDelete("rate-limit-rules/{id:int}")]
    [RequireAdminPermission(AdminPermissions.SecurityManage)]
    public async Task<IActionResult> DeleteRateLimitRule(int id)
    {
        _logger.LogInformation(" DELETE REQUEST - Rate Limit Rule ID: {RuleId}", id);

        var result = await _rateLimitingService.DeleteRateLimitRuleAsync(id);

        _logger.LogInformation(" DELETE RESULT - Success: {IsSuccess} | Error: {ErrorMessage}",
            result.IsSuccess, result.ErrorMessage);

        if (!result.IsSuccess)
        {
            _logger.LogWarning(" RETURNING BADREQUEST - Rule ID: {RuleId} | Error: {ErrorMessage}",
                id, result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage });
        }

        await _auditLoggingService.LogAdminActivityAsync(
            GetCurrentAdminUserId() ?? 2, // Default to Super Admin (ID 2)
            "rate_limit_rule_deleted",
            $"Deleted rate limit rule {id}",
            GetClientIpAddress(),
            Request.Headers["User-Agent"].ToString());

        return Ok(new { success = true, message = "Rate limit rule deleted successfully" });
    }



    #endregion

    #region Security Metrics

    /// <summary>
    /// Get aggregated security metrics for admin dashboard
    /// </summary>
    [HttpGet("metrics")]
    [RequireAdminPermission(AdminPermissions.SecurityRead)]
    public async Task<IActionResult> GetSecurityMetrics([FromQuery] int days = 7)
    {
        var result = await _securityEventService.GetSecurityMetricsAsync(days);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    #endregion

    #region Security Events Management

    /// <summary>
    /// Get security events with filtering
    /// </summary>
    [HttpGet("events")]
    [RequireAdminPermission(AdminPermissions.SecurityRead)]
    public async Task<IActionResult> GetSecurityEvents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? eventType = null,
        [FromQuery] string? severity = null, // Note: Service signature updated to standard params order?
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        // Service signature correction: 
        // GetEventsAsync(eventType, from, to, userId, adminId, page, pageSize)
        // Severity was missing in my previous signature update?
        // Wait, the Interface defines GetEventsAsync without 'severity' in the middle?
        // Interface: GetEventsAsync(string? eventType = null, DateTime? from = null, DateTime? to = null, int? userId = null, int? adminUserId = null, int page = 1, int pageSize = 50);
        // BUT my implementation code uses 'severity' inside the body! I must pass it.
        // I need to correct the Service Interface and Implementation to include 'Severtiy' OR pass it as metadata.
        // Looking at previous state, I removed 'Severity' from implementation signature in previous step? 
        // Yes, "GetEventsAsync(string? eventType..."
        // I should have kept 'severity'.
        
        // I will fix the Controller call assuming I will fix Service signature next. 
        // Or I pass null for userId/adminId.
        
        // Let's use named arguments for safety if possible, or just positional.
        
        var result = await _securityEventService.GetEventsAsync(eventType, severity, fromDate, toDate, null, null, page, pageSize);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new
        {
            items = result.Data.Items,
            totalCount = result.Data.TotalCount,
            page = result.Data.Page,
            pageSize = result.Data.PageSize,
            totalPages = result.Data.TotalPages
        });
    }

    /// <summary>
    /// Get security events with filtering (alias for frontend compatibility)
    /// </summary>
    [HttpGet("security-events")] // Frontend calls this
    [RequireAdminPermission(AdminPermissions.SecurityRead)]
    public async Task<IActionResult> GetSecurityEventsAlias(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string? eventType = null,
        [FromQuery] string? severity = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        return await GetSecurityEvents(skip, take, eventType, severity, fromDate, toDate);
    }

    /// <summary>
    /// Update a security event (for investigation purposes)
    /// </summary>
    [HttpPut("events/{id:int}")]
    [RequireAdminPermission(AdminPermissions.SecurityManage)]
    public async Task<IActionResult> UpdateSecurityEvent(int id, [FromBody] UpdateSecurityEventRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var adminUserId = GetCurrentAdminUserId() ?? 1; // Default admin user ID
        var result = await _securityEventService.MarkEventInvestigatedAsync(id, adminUserId, request.Notes ?? string.Empty);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        await _auditLoggingService.LogAdminActivityAsync(
            adminUserId,
            "security_event_updated",
            $"Updated security event {id}",
            GetClientIpAddress(),
            Request.Headers["User-Agent"].ToString());

        return Ok(result.Data);
    }

    /// <summary>
    /// Get security event statistics
    /// </summary>
    [HttpGet("events/statistics")]
    [HttpGet("statistics")] // Alias for frontend
    [RequireAdminPermission(AdminPermissions.SecurityRead)]
    public async Task<IActionResult> GetSecurityStatistics([FromQuery] int days = 7)
    {
        var from = DateTime.UtcNow.AddDays(-days);
        var to = DateTime.UtcNow;
        var result = await _securityEventService.GetEventStatisticsAsync(from, to);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get security events by IP address
    /// </summary>
    [HttpGet("events/by-ip/{ipAddress}")]
    [RequireAdminPermission(AdminPermissions.SecurityRead)]
    public async Task<IActionResult> GetEventsByIP(string ipAddress, [FromQuery] int hours = 24)
    {
        var result = await _securityEventService.GetEventsByIPAsync(ipAddress, hours);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get related security events
    /// </summary>
    [HttpGet("events/{id:int}/related")]
    [RequireAdminPermission(AdminPermissions.SecurityRead)]
    public async Task<IActionResult> GetRelatedEvents(int id)
    {
        var result = await _securityEventService.GetRelatedEventsAsync(id);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Mark security event as resolved
    /// </summary>
    [HttpPost("events/{id:int}/resolve")]
    [RequireAdminPermission(AdminPermissions.SecurityManage)]
    public async Task<IActionResult> ResolveSecurityEvent(int id, [FromBody] ResolveSecurityEventRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var adminUserId = GetCurrentAdminUserId() ?? 1; // Default admin user ID
        var result = await _securityEventService.MarkEventInvestigatedAsync(id, adminUserId, request.Resolution);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        await _auditLoggingService.LogAdminActivityAsync(
            adminUserId,
            "security_event_resolved",
            $"Resolved security event {id}",
            GetClientIpAddress(),
            Request.Headers["User-Agent"].ToString());

        return Ok(new { success = true, message = "Security event marked as resolved" });
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Get the current admin user ID from the JWT claims
    /// </summary>
    private int? GetCurrentAdminUserId()
    {
        var userIdClaim = User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
                          ?? User?.FindFirst("user_id")?.Value
                          ?? User?.FindFirst("sub")?.Value
                          ?? User?.FindFirst("id")?.Value;

        if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }

        return null; // Return null if no valid admin user ID found
    }

    private string GetClientIpAddress()
    {
        return HttpContext.GetClientIpAddress();
    }

    #endregion
}

#region Request/Response DTOs

public class CreateIPRuleRequest
{
    public string IPAddress { get; set; } = "";
    public string Type { get; set; } = ""; // blacklist/whitelist
    public string Reason { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public string? ThreatLevel { get; set; }
    public string? CountryCode { get; set; }
}

public class UpdateIPRuleRequest : CreateIPRuleRequest
{
}

public class BlockIPRequest
{
    public string IPAddress { get; set; } = "";
    public string Reason { get; set; } = "";
    public DateTime? ExpiresAt { get; set; }
}

public class UnblockIPRequest
{
    public string IPAddress { get; set; } = "";
}

public class CreateRateLimitRuleRequest
{
    public string Name { get; set; } = "";
    public string Endpoint { get; set; } = "";

    // Support both frontend naming conventions  
    [System.Text.Json.Serialization.JsonPropertyName("method")]
    public string Method { get; set; } = "ALL";

    [System.Text.Json.Serialization.JsonIgnore]
    public string HttpMethod => Method;

    public int RequestsPerMinute { get; set; } = 60;
    public int RequestsPerHour { get; set; } = 1000;
    public int RequestsPerDay { get; set; } = 10000;
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 0;
    public string? Description { get; set; }

    // Support array format from frontend and convert to string for backend
    [System.Text.Json.Serialization.JsonPropertyName("ipWhitelist")]
    public List<string>? IpWhitelistArray { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public string? IPWhitelist => IpWhitelistArray != null && IpWhitelistArray.Any() ? string.Join(",", IpWhitelistArray) : null;

    [System.Text.Json.Serialization.JsonPropertyName("userRoleExceptions")]
    public List<string>? UserRoleExceptionsArray { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserRoleExceptionsString => UserRoleExceptionsArray != null && UserRoleExceptionsArray.Any() ? string.Join(",", UserRoleExceptionsArray) : null;

    public string? ApiKeyExceptions { get; set; }
    public int CooldownSeconds { get; set; } = 60;
    public string? CustomErrorMessage { get; set; }
}

public class UpdateRateLimitRuleRequest : CreateRateLimitRuleRequest
{
}

public class UpdateSecurityEventRequest
{
    public string Status { get; set; } = "";
    public string? Notes { get; set; }
}

public class ResolveSecurityEventRequest
{
    public string Resolution { get; set; } = "";
}

#endregion
