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

    public SecurityReportingController(ILogger<SecurityReportingController> logger, IIPBlockingService ipBlockingService)
    {
        _logger = logger;
        _ipBlockingService = ipBlockingService;
    }

    [HttpPost("csp-report")]
    public IActionResult ReportCspViolation([FromBody] JsonElement report)
    {
        // CSP reports are often noisy, so we log them with a specific event ID or category
        // The browser typically sends 'csp-report' property or the violation directly
        var content = report.ToString();
        _logger.LogWarning("CSP Violation Reported: {Content} | IP: {IP}", content, GetClientIp());

        // In a real scenario, you might parse this and store it in SecurityEvents if meaningful
        return Ok();
    }

    [HttpPost("incident-report")]
    public IActionResult ReportIncident([FromBody] SecurityIncidentReport request)
    {
        if (request == null) return BadRequest();

        _logger.LogWarning("Client Security Incident: {Type} | Details: {Details} | IP: {IP}", 
            request.Type, request.Details, GetClientIp());

        if (request.Type == "xss_attempt" || request.Type == "dev_tools_detected")
        {
             // Potential adaptive response: block IP if repeated
        }

        return Ok();
    }

    [HttpPost("alert")]
    public IActionResult ReportAlert([FromBody] SecurityAlertRequest request)
    {
        // Similar to incident-report but matching the frontend hook 'useSecurityMonitoring'
        // which sends { type, pattern, content, timestamp, url }
        
        _logger.LogWarning("Security Alert: {Type} | Pattern: {Pattern} | URL: {Url} | IP: {IP}", 
            request.Type, request.Pattern, request.Url, GetClientIp());

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
