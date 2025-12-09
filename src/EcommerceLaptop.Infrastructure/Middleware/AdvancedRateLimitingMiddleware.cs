using EcommerceLaptop.Infrastructure.Services.Security;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceLaptop.Infrastructure.Middleware;

/// <summary>
/// Advanced rate limiting middleware with support for per-endpoint, per-IP, and per-user limits
/// Works in conjunction with RateLimitingService for flexible rate limiting rules
/// </summary>
public class AdvancedRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AdvancedRateLimitingMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AdvancedRateLimitingMiddleware(RequestDelegate next, ILogger<AdvancedRateLimitingMiddleware> logger, IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for certain paths
        var path = context.Request.Path.Value?.ToLowerInvariant();
        if (IsExcludedPath(path))
        {
            await _next(context);
            return;
        }

        var clientIdentifier = GetClientIdentifier(context);
        var endpoint = context.Request.Path.Value ?? "";
        var httpMethod = context.Request.Method;
        var userRole = GetUserRole(context);
        var apiKey = GetApiKey(context);

        using var scope = _serviceProvider.CreateScope();
        var rateLimitingService = scope.ServiceProvider.GetRequiredService<IRateLimitingService>();

        try
        {
            var result = await rateLimitingService.CheckRateLimitAsync(clientIdentifier, endpoint, httpMethod, userRole, apiKey);

            if (result.IsSuccess && !result.Data.IsAllowed)
            {
                await HandleRateLimitExceeded(context, result.Data, clientIdentifier, endpoint);
                return;
            }

            // Add rate limit headers to response (idempotent and safe)
            if (result.IsSuccess && result.Data.RemainingRequests >= 0 && !context.Response.HasStarted)
            {
                context.Response.Headers["X-RateLimit-Remaining"] = result.Data.RemainingRequests.ToString();
                if (!string.IsNullOrEmpty(result.Data.RuleName))
                {
                    context.Response.Headers["X-RateLimit-Rule"] = result.Data.RuleName;
                }
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in rate limiting middleware for {Client} on {Endpoint}", clientIdentifier, endpoint);

            // On error, allow the request to continue
            await _next(context);
        }
    }

    private async Task HandleRateLimitExceeded(HttpContext context, RateLimitResult rateLimitResult, string clientIdentifier, string endpoint)
    {
        _logger.LogWarning("Rate limit exceeded for {Client} on {Endpoint}: {Message}",
            clientIdentifier, endpoint, rateLimitResult.Message);

        context.Response.StatusCode = 429; // Too Many Requests

        // Set standard rate limiting headers (use assignment to avoid duplicates)
        if (!context.Response.HasStarted)
        {
            if (rateLimitResult.RetryAfter.HasValue)
            {
                var retryAfterSeconds = (int)(rateLimitResult.RetryAfter.Value - DateTime.UtcNow).TotalSeconds;
                context.Response.Headers["Retry-After"] = Math.Max(1, retryAfterSeconds).ToString();
            }
            else if (rateLimitResult.CooldownSeconds.HasValue)
            {
                context.Response.Headers["Retry-After"] = rateLimitResult.CooldownSeconds.Value.ToString();
            }

            context.Response.Headers["X-RateLimit-Remaining"] = "0";
            context.Response.Headers["X-RateLimit-Exceeded"] = "true";

            if (!string.IsNullOrEmpty(rateLimitResult.RuleName))
            {
                context.Response.Headers["X-RateLimit-Rule"] = rateLimitResult.RuleName;
            }
        }

        var response = new
        {
            error = "Rate limit exceeded",
            message = rateLimitResult.Message,
            remainingRequests = rateLimitResult.RemainingRequests,
            retryAfter = rateLimitResult.RetryAfter,
            rule = rateLimitResult.RuleName,
            timestamp = DateTime.UtcNow,
            requestId = context.TraceIdentifier
        };

        await context.Response.WriteAsJsonAsync(response);
    }

    private static string GetClientIdentifier(HttpContext context)
    {
        // Priority order: User ID > API Key > IP Address

        // 1. Try to get authenticated user ID
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                    context.User?.FindFirst("user_id")?.Value ??
                    context.User?.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            return $"user_{userId}";
        }

        // 2. Try to get API key
        var apiKey = GetApiKey(context);
        if (!string.IsNullOrEmpty(apiKey))
        {
            return $"apikey_{apiKey[..Math.Min(8, apiKey.Length)]}"; // First 8 chars for identification
        }

        // 3. Fall back to IP address
        return $"ip_{GetClientIPAddress(context)}";
    }

    private static string? GetUserRole(HttpContext context)
    {
        return context.User?.FindFirst(ClaimTypes.Role)?.Value ??
               context.User?.FindFirst("role")?.Value;
    }

    private static string? GetApiKey(HttpContext context)
    {
        // Check various header formats for API key
        var apiKeyHeaders = new[]
        {
            "X-API-Key",
            "X-Api-Key",
            "ApiKey",
            "Api-Key",
            "Authorization" // For "ApiKey xxxx" format
        };

        foreach (var header in apiKeyHeaders)
        {
            var value = context.Request.Headers[header].FirstOrDefault();
            if (!string.IsNullOrEmpty(value))
            {
                // Handle "ApiKey xxxx" or "Bearer xxxx" formats
                if (value.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
                {
                    return value[7..]; // Remove "ApiKey " prefix
                }
                else if (header == "Authorization" && value.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
                {
                    return value[7..]; // Remove "ApiKey " prefix
                }
                else if (header != "Authorization") // Don't return Bearer tokens as API keys
                {
                    return value;
                }
            }
        }

        // Check query parameter
        return context.Request.Query["apikey"].FirstOrDefault() ??
               context.Request.Query["api_key"].FirstOrDefault();
    }

    private static string GetClientIPAddress(HttpContext context)
    {
        // Same logic as IPBlockingMiddleware
        var ipHeaders = new[]
        {
            "CF-Connecting-IP",     // Cloudflare
            "X-Forwarded-For",      // Standard proxy header
            "X-Real-IP",           // Nginx proxy
            "X-Client-IP",         // Apache proxy
            "X-Original-Forwarded-For"
        };

        foreach (var header in ipHeaders)
        {
            var value = context.Request.Headers[header].FirstOrDefault();
            if (!string.IsNullOrEmpty(value))
            {
                var ip = value.Split(',').FirstOrDefault()?.Trim();
                if (IsValidIPAddress(ip))
                {
                    return ip;
                }
            }
        }

        var remoteIP = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(remoteIP))
        {
            if (remoteIP == "::1")
                return "127.0.0.1";

            return remoteIP;
        }

        return "unknown";
    }

    private static bool IsValidIPAddress(string? ip)
    {
        return !string.IsNullOrEmpty(ip) && System.Net.IPAddress.TryParse(ip, out _);
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
            "/robots.txt",
            "/swagger",
            "/api/auth/refresh" // Allow refresh tokens to bypass rate limits
        };

        return excludedPaths.Any(excluded => path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Extension method to add advanced rate limiting middleware to the pipeline
/// </summary>
public static class AdvancedRateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseAdvancedRateLimit(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AdvancedRateLimitingMiddleware>();
    }
}