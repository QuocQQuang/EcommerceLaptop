using EcommerceLaptop.Infrastructure.Services.Security;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceLaptop.Infrastructure.Middleware;

/// <summary>
/// Middleware for IP blocking functionality
/// Checks incoming requests against IP block/whitelist rules
/// Must be placed early in the middleware pipeline for security
/// </summary>
public class IPBlockingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IPBlockingMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    public IPBlockingMiddleware(RequestDelegate next, ILogger<IPBlockingMiddleware> logger, IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip IP blocking for health checks and internal endpoints
        var path = context.Request.Path.Value?.ToLowerInvariant();
        if (IsExcludedPath(path))
        {
            await _next(context);
            return;
        }

        var clientIP = GetClientIPAddress(context);
        if (string.IsNullOrEmpty(clientIP))
        {
            _logger.LogWarning("Could not determine client IP address");
            await _next(context);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var ipBlockingService = scope.ServiceProvider.GetRequiredService<IIPBlockingService>();

        try
        {
            // Check if IP is whitelisted first (highest priority)
            var whitelistResult = await ipBlockingService.IsIPWhitelistedAsync(clientIP);
            if (whitelistResult.IsSuccess && whitelistResult.Data)
            {
                await ipBlockingService.LogIPAccessAttemptAsync(clientIP, path ?? "", false, "IP whitelisted");
                await _next(context);
                return;
            }

            // Check if IP is blacklisted
            var blacklistResult = await ipBlockingService.IsIPBlockedAsync(clientIP);
            if (blacklistResult.IsSuccess && blacklistResult.Data)
            {
                await HandleBlockedIP(context, clientIP, path);
                await ipBlockingService.LogIPAccessAttemptAsync(clientIP, path ?? "", true, "IP blacklisted");
                return;
            }

            // IP is not blocked, continue with request
            await ipBlockingService.LogIPAccessAttemptAsync(clientIP, path ?? "", false, "IP not in block list");
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in IP blocking middleware for IP {ClientIP}", clientIP);
            
            // On error, allow the request to continue to avoid breaking the app
            // but log the issue for investigation
            await _next(context);
        }
    }

    private async Task HandleBlockedIP(HttpContext context, string clientIP, string? path)
    {
        _logger.LogWarning("Blocked access attempt from IP {ClientIP} to {Path}", clientIP, path);

        context.Response.StatusCode = 403; // Forbidden
        context.Response.Headers["X-Blocked-Reason"] = "IP address is blocked";
        
        var response = new
        {
            error = "Access denied",
            message = "Your IP address has been blocked due to security policies",
            timestamp = DateTime.UtcNow,
            requestId = context.TraceIdentifier
        };

        await context.Response.WriteAsJsonAsync(response);
    }

    private static string GetClientIPAddress(HttpContext context)
    {
        return context.GetClientIpAddress("unknown");
    }

    private static bool IsExcludedPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var excludedPaths = new[]
        {
            "/health",
            "/ready",
            "/alive",
            "/metrics",
            "/favicon.ico",
            "/.well-known",
            "/robots.txt"
        };

        return excludedPaths.Any(excluded => path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Extension method to add IP blocking middleware to the pipeline
/// </summary>
public static class IPBlockingMiddlewareExtensions
{
    public static IApplicationBuilder UseIPBlocking(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<IPBlockingMiddleware>();
    }
}
