using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Context;
using System.Security.Claims;

namespace EcommerceLaptop.Infrastructure.Middleware;

public class StructuredLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public StructuredLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = GetClientIPAddress(context);
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? context.User?.FindFirst("sub")?.Value;
        var userEmail = context.User?.FindFirst(ClaimTypes.Email)?.Value;

        // Push properties to the Serilog DiagnosticContext so they appear in the final Request Log
        // Note: usage of IDiagnosticContext requires these to be registered. 
        // Or we can use LogContext.PushProperty if we are logging inside this middleware or downstream.
        // For SerilogRequestLogging, IDiagnosticContext is the way.
        
        var diagnosticContext = context.RequestServices.GetService<IDiagnosticContext>();
        
        if (diagnosticContext != null)
        {
            diagnosticContext.Set("ClientIp", ipAddress);
            if (!string.IsNullOrEmpty(userId)) diagnosticContext.Set("UserId", userId);
            if (!string.IsNullOrEmpty(userEmail)) diagnosticContext.Set("UserEmail", userEmail);
            diagnosticContext.Set("UserAgent", context.Request.Headers["User-Agent"].FirstOrDefault());
        }

        // Also push to LogContext for any logs generated *during* the request (e.g. inside Controllers)
        using (LogContext.PushProperty("ClientIp", ipAddress))
        using (LogContext.PushProperty("UserId", userId))
        {
            await _next(context);
        }
    }

    private static string GetClientIPAddress(HttpContext context)
    {
        var ipHeaders = new[]
        {
            "CF-Connecting-IP",
            "X-Forwarded-For",
            "X-Real-IP",
            "X-Client-IP"
        };

        foreach (var header in ipHeaders)
        {
            var value = context.Request.Headers[header].FirstOrDefault();
            if (!string.IsNullOrEmpty(value))
            {
                return value.Split(',').FirstOrDefault()?.Trim() ?? "";
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
