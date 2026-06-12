using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Infrastructure.Services;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.API.Authorization;
using EcommerceLaptop.Core.Services;
using System.Diagnostics;

using EcommerceLaptop.Infrastructure.Services.Security;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Controller for managing system settings
/// </summary>
[Route("api/[controller]")]
[ApiController]
// [Authorize(Roles = "Admin")] // Temporarily disabled for testing
public class SystemSettingsController(
    ISystemSettingsService settingsService,
    IEmailService emailService,
    IEmailSecurityService emailSecurityService,
    IAuditLoggingService auditLoggingService,
    ILogger<SystemSettingsController> logger)
    : BaseApiController(logger)
{
    private readonly ISystemSettingsService _settingsService = settingsService;
    private readonly IEmailService _emailService = emailService;
    private readonly IEmailSecurityService _emailSecurityService = emailSecurityService;
    private readonly IAuditLoggingService _auditLoggingService = auditLoggingService;

    /// <summary>
    /// Get all system settings grouped by category
    /// </summary>
    [HttpGet]
    [RequireAdminPermission("settings:read")]
    public async Task<IActionResult> GetAllSettings()
    {
        var result = await _settingsService.GetAllSettingsGroupedAsync();

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get settings by category
    /// </summary>
    [HttpGet("category/{category}")]
    [RequireAdminPermission("settings:read")]
    public async Task<IActionResult> GetSettingsByCategory(string category)
    {
        var result = await _settingsService.GetSettingsByCategoryAsync(category);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get all available categories
    /// </summary>
    [HttpGet("categories")]
    [RequireAdminPermission("settings:read")]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _settingsService.GetCategoriesAsync();

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get a single setting by key
    /// </summary>
    [HttpGet("{key}")]
    [RequireAdminPermission("settings:read")]
    public async Task<IActionResult> GetSetting(string key)
    {
        var result = await _settingsService.GetSettingByKeyAsync(key);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        if (result.Data == null)
            return NotFound(new { error = "Setting not found" });

        return Ok(result.Data);
    }

    /// <summary>
    /// Update or create a setting
    /// </summary>
    [HttpPut("{key}")]
    [RequireAdminPermission("settings:write")]
    public async Task<IActionResult> UpsertSetting(string key, [FromBody] UpsertSettingRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _settingsService.UpsertSettingAsync(key, request.Value, request.Category, request.Description);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Update multiple settings
    /// </summary>
    [HttpPut("batch")]
    [RequireAdminPermission("settings:write")]
    public async Task<IActionResult> UpdateSettings([FromBody] List<SystemSettingUpdateRequest> settings)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _settingsService.UpdateSettingsAsync(settings);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new { message = "Settings updated successfully" });
    }

    /// <summary>
    /// Send a test email using current SMTP configuration
    /// </summary>
    public record TestEmailRequest(string? To = null, string? Subject = null, string? HtmlContent = null, string? TextContent = null);

    [HttpPost("test-email")]
    [RequireAdminPermission("settings:read")]
    public async Task<IActionResult> SendTestEmail([FromBody] TestEmailRequest? request)
    {
        var stopwatch = Stopwatch.StartNew();
        var toEmail = string.IsNullOrWhiteSpace(request?.To) ? "quocquang30@gmail.com" : request!.To!;
        var subject = string.IsNullOrWhiteSpace(request?.Subject) ? "Test Email - System Settings" : request!.Subject!;
        var html = string.IsNullOrWhiteSpace(request?.HtmlContent)
            ? "<h2>Test Email</h2><p>This is a test email sent from EcommerceLaptop.</p>"
            : request!.HtmlContent!;

        // Pre-check rate limit to return proper status code instead of generic 500
        var rateOk = await _emailSecurityService.CheckRateLimitAsync(toEmail);
        if (!rateOk)
        {
            await _auditLoggingService.LogAdminActivityAsync(
                GetAdminIdAsInt() ?? 2, // Default to SuperAdmin (ID 2)
                "email_test_rate_limited",
                $"Email test rate limited for recipient {toEmail}",
                GetClientIpAddress(),
                Request.Headers["User-Agent"].ToString(),
                targetResource: "SystemSettings/test-email");
            return StatusCode(429, new { success = false, message = "Rate limit exceeded. Please try again later." });
        }

        var result = await _emailService.SendCustomEmailAsync(toEmail, toEmail, subject, html, request?.TextContent);
        if (!result)
        {
            stopwatch.Stop();
            _logger.LogError("Failed to send test email to {ToEmail} after {ElapsedMs} ms", toEmail, stopwatch.ElapsedMilliseconds);
            await _auditLoggingService.LogAdminActivityAsync(
                GetAdminIdAsInt() ?? 2, // Default to SuperAdmin (ID 2)
                "email_test_failed",
                $"Failed to send test email to {toEmail} in {stopwatch.ElapsedMilliseconds} ms",
                GetClientIpAddress(),
                Request.Headers["User-Agent"].ToString(),
                targetResource: "SystemSettings/test-email");
            return StatusCode(500, new { success = false, message = "Failed to send test email" });
        }

        stopwatch.Stop();
        _logger.LogInformation("Sent test email to {ToEmail} successfully in {ElapsedMs} ms", toEmail, stopwatch.ElapsedMilliseconds);
        await _auditLoggingService.LogAdminActivityAsync(
            GetAdminIdAsInt() ?? 2, // Default to SuperAdmin (ID 2)
            "email_test_success",
            $"Sent test email to {toEmail} in {stopwatch.ElapsedMilliseconds} ms",
            GetClientIpAddress(),
            Request.Headers["User-Agent"].ToString(),
            targetResource: "SystemSettings/test-email");

        return Ok(new { success = true, elapsedMs = stopwatch.ElapsedMilliseconds });
    }

    /// <summary>
    /// Send a test notification email for a specific event type with detailed audit logs
    /// </summary>
    public record TestNotificationRequest(string EventType, string? To = null);

    [HttpPost("test-notification")]
    [RequireAdminPermission("settings:read")]
    public async Task<IActionResult> SendTestNotification([FromBody] TestNotificationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            return BadRequest(new { success = false, message = "EventType is required" });
        }

        var stopwatch = Stopwatch.StartNew();
        var toEmail = string.IsNullOrWhiteSpace(request.To) ? "quocquang30@gmail.com" : request.To!;

        var rateOk = await _emailSecurityService.CheckRateLimitAsync(toEmail);
        if (!rateOk)
        {
            await _auditLoggingService.LogAdminActivityAsync(
                GetAdminIdAsInt() ?? 2, // Default to SuperAdmin (ID 2)
                "notification_test_rate_limited",
                $"Notification test rate limited for event {request.EventType} to {toEmail}",
                GetClientIpAddress(),
                Request.Headers["User-Agent"].ToString(),
                targetResource: "SystemSettings/test-notification");
            return StatusCode(429, new { success = false, message = "Rate limit exceeded. Please try again later." });
        }

        var subject = $"Test Notification - {request.EventType}";
        var html = $"<h2>Test Notification</h2><p>Event: {request.EventType}</p><p>This is a test notification email.</p>";

        var result = await _emailService.SendCustomEmailAsync(toEmail, toEmail, subject, html, null);
        stopwatch.Stop();

        if (!result)
        {
            _logger.LogError("Failed to send test notification for {EventType} to {ToEmail} after {ElapsedMs} ms", request.EventType, toEmail, stopwatch.ElapsedMilliseconds);
            await _auditLoggingService.LogAdminActivityAsync(
                GetAdminIdAsInt() ?? 2, // Default to SuperAdmin (ID 2)
                "notification_test_failed",
                $"Failed to send test notification {request.EventType} to {toEmail} in {stopwatch.ElapsedMilliseconds} ms",
                GetClientIpAddress(),
                Request.Headers["User-Agent"].ToString(),
                targetResource: "SystemSettings/test-notification");
            return StatusCode(500, new { success = false, message = "Failed to send test notification" });
        }

        _logger.LogInformation("Sent test notification for {EventType} to {ToEmail} in {ElapsedMs} ms", request.EventType, toEmail, stopwatch.ElapsedMilliseconds);
        await _auditLoggingService.LogAdminActivityAsync(
            GetAdminIdAsInt() ?? 2, // Default to SuperAdmin (ID 2)
            "notification_test_success",
            $"Sent test notification {request.EventType} to {toEmail} in {stopwatch.ElapsedMilliseconds} ms",
            GetClientIpAddress(),
            Request.Headers["User-Agent"].ToString(),
            targetResource: "SystemSettings/test-notification"); return Ok(new { success = true, elapsedMs = stopwatch.ElapsedMilliseconds });
    }

    /// <summary>
    /// Delete a setting
    /// </summary>
    [HttpDelete("{key}")]
    [RequireAdminPermission("settings:manage")]
    public async Task<IActionResult> DeleteSetting(string key)
    {
        var result = await _settingsService.DeleteSettingAsync(key);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new { message = "Setting deleted successfully" });
    }

    /// <summary>
    /// Reset setting to default value
    /// </summary>
    [HttpPost("{key}/reset")]
    [RequireAdminPermission("settings:write")]
    public async Task<IActionResult> ResetSetting(string key)
    {
        var result = await _settingsService.ResetSettingToDefaultAsync(key);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    private int? GetAdminIdAsInt()
    {
        // Try multiple claim types to find the user ID
        var adminIdStr = User.FindFirst("uid")?.Value
                        ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
                        ?? User.FindFirst("user_id")?.Value
                        ?? User.FindFirst("sub")?.Value
                        ?? User.FindFirst("id")?.Value;

        if (int.TryParse(adminIdStr, out int adminId))
        {
            return adminId;
        }
        return null;
    }
}

/// <summary>
/// DTO for upserting system settings
/// </summary>
public class UpsertSettingRequest
{
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string? Description { get; set; }
}
