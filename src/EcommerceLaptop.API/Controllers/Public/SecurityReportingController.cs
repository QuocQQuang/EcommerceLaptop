using EcommerceLaptop.Infrastructure.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EcommerceLaptop.API.Controllers.Public;

[ApiController]
[Route("api/security")]
[AllowAnonymous]
public class SecurityReportingController : ControllerBase
{
    private readonly ILogger<SecurityReportingController> _logger;
    private readonly IIPBlockingService _ipBlockingService;
    private readonly ISecurityEventService _securityEventService;

    public SecurityReportingController(
        ILogger<SecurityReportingController> logger, 
        IIPBlockingService ipBlockingService,
        ISecurityEventService securityEventService)
    {
        _logger = logger;
        _ipBlockingService = ipBlockingService;
        _securityEventService = securityEventService;
    }

    [HttpPost("csp-report")]
    public async Task<IActionResult> ReportCspViolation([FromBody] JsonElement report)
    {
        var content = report.ToString();
        var clientIp = GetClientIp();
        _logger.LogWarning("CSP Violation Reported: {Content} | IP: {IP}", content, clientIp);

        // Persist to SecurityEvents table
        await _securityEventService.LogEventAsync(
            eventType: "csp_violation",
            description: $"CSP violation: {content.Substring(0, Math.Min(500, content.Length))}",
            userId: null,
            adminUserId: null,
            ipAddress: clientIp,
            userAgent: Request.Headers["User-Agent"].ToString(),
            correlationId: null,
            metadata: new Dictionary<string, object> { ["report"] = content }
        );

        return Ok();
    }

    [HttpPost("incident-report")]
    public async Task<IActionResult> ReportIncident([FromBody] SecurityIncidentReport request)
    {
        if (request == null) return BadRequest();

        var clientIp = GetClientIp();
        _logger.LogWarning("Client Security Incident: {Type} | Details: {Details} | IP: {IP}", 
            request.Type, request.Details, clientIp);

        // Persist to SecurityEvents table
        await _securityEventService.LogEventAsync(
            eventType: $"client_incident_{request.Type}",
            description: $"Client reported incident: {request.Type}",
            userId: null,
            adminUserId: null,
            ipAddress: clientIp,
            userAgent: request.UserAgent,
            correlationId: null,
            metadata: new Dictionary<string, object> 
            { 
                ["type"] = request.Type,
                ["details"] = request.Details ?? new object(),
                ["url"] = request.Url,
                ["timestamp"] = request.Timestamp
            }
        );

        return Ok();
    }

    [HttpPost("alert")]
    public async Task<IActionResult> ReportAlert([FromBody] SecurityAlertRequest request)
    {
        var clientIp = GetClientIp();
        _logger.LogWarning("Security Alert: {Type} | Pattern: {Pattern} | URL: {Url} | IP: {IP}", 
            request.Type, request.Pattern, request.Url, clientIp);

        // Persist to SecurityEvents table
        await _securityEventService.LogEventAsync(
            eventType: $"client_alert_{request.Type}",
            description: $"Security alert: {request.Type} matched pattern {request.Pattern}",
            userId: null,
            adminUserId: null,
            ipAddress: clientIp,
            userAgent: Request.Headers["User-Agent"].ToString(),
            correlationId: null,
            metadata: new Dictionary<string, object> 
            { 
                ["type"] = request.Type,
                ["pattern"] = request.Pattern,
                ["content"] = request.Content,
                ["url"] = request.Url,
                ["timestamp"] = request.Timestamp
            }
        );

        return Ok();
    }

    private string GetClientIp()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}

public class SecurityIncidentReport
{
    public string Type { get; set; } = string.Empty;
    public object? Details { get; set; }
    public string UserAgent { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
}

public class SecurityAlertRequest
{
    public string Type { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
}
