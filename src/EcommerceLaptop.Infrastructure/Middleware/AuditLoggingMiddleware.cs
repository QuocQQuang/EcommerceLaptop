using EcommerceLaptop.Infrastructure.Services.Security;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceLaptop.Infrastructure.Middleware;

/// <summary>
/// Comprehensive audit logging middleware that tracks all HTTP requests and responses
/// Captures detailed information about user actions, API calls, and system interactions
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger, IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip audit logging for certain paths to reduce noise
        var path = context.Request.Path.Value?.ToLowerInvariant();
        if (IsExcludedPath(path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;
        
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Capture request details
        var requestDetails = await CaptureRequestDetailsAsync(context);
        
        Exception? exception = null;
        
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            exception = ex;
            throw; // Re-throw to maintain normal exception handling
        }
        finally
        {
            stopwatch.Stop();
            
            // Restore original stream
            await responseBodyStream.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
            
            // Log the audit event
            await LogAuditEventAsync(context, requestDetails, stopwatch.ElapsedMilliseconds, exception);
        }
    }

    private async Task<RequestDetails> CaptureRequestDetailsAsync(HttpContext context)
    {
        var details = new RequestDetails
        {
            Method = context.Request.Method,
            Path = context.Request.Path.Value ?? "",
            QueryString = context.Request.QueryString.Value ?? "",
            UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault(),
            IPAddress = GetClientIPAddress(context),
            Timestamp = DateTime.UtcNow
        };

        // Capture user information
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            details.UserId = context.User.FindFirst("user_id")?.Value ??
                            context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            details.UserEmail = context.User.FindFirst(ClaimTypes.Email)?.Value;
            details.UserRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
            details.IsAdmin = context.User.IsInRole("Admin");
        }

        // Capture request body for POST/PUT/PATCH requests (with size limit and sensitive data filtering)
        if (ShouldCaptureRequestBody(context))
        {
            details.RequestBody = await CaptureRequestBodyAsync(context);
        }

        return details;
    }

    private async Task LogAuditEventAsync(HttpContext context, RequestDetails requestDetails, long executionTimeMs, Exception? exception)
    {
        using var scope = _serviceProvider.CreateScope();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditLoggingService>();

        try
        {
            // Determine event category and type
            var (eventCategory, eventType) = DetermineEventCategoryAndType(requestDetails.Path, requestDetails.Method, context.Response.StatusCode);
            
            // Determine entity type and ID from the path
            var (entityType, entityId) = ExtractEntityFromPath(requestDetails.Path);
            
            // Create description
            var description = CreateDescription(requestDetails, context.Response.StatusCode, exception);
            
            // Create metadata
            var metadata = new Dictionary<string, object>
            {
                { "executionTimeMs", executionTimeMs },
                { "statusCode", context.Response.StatusCode },
                { "userAgent", requestDetails.UserAgent ?? "" },
                { "queryString", requestDetails.QueryString },
                { "requestId", context.TraceIdentifier }
            };

            if (exception != null)
            {
                metadata["exception"] = new
                {
                    type = exception.GetType().Name,
                    message = exception.Message,
                    stackTrace = exception.StackTrace
                };
            }

            // Add request body if captured
            if (!string.IsNullOrEmpty(requestDetails.RequestBody))
            {
                metadata["requestBody"] = requestDetails.RequestBody;
            }

            await auditService.LogEventAsync(
                entityType: entityType,
                entityId: entityId,
                eventType: eventType,
                eventCategory: eventCategory,
                oldValues: null, // No old values for HTTP requests
                newValues: metadata, // Pass metadata as new values
                userId: requestDetails.UserId?.ToString(),
                adminUserId: requestDetails.IsAdmin ? requestDetails.UserId?.ToString() : null,
                ipAddress: requestDetails.IPAddress,
                userAgent: requestDetails.UserAgent,
                metadata: metadata
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in audit logging middleware");
        }
    }

    private static async Task<string?> CaptureRequestBodyAsync(HttpContext context)
    {
        try
        {
            context.Request.EnableBuffering();
            
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0; // Reset for next middleware
            
            // Filter sensitive data
            return FilterSensitiveData(body, context.Request.Path.Value ?? "");
        }
        catch
        {
            return null;
        }
    }

    private static string? FilterSensitiveData(string body, string path)
    {
        if (string.IsNullOrEmpty(body) || body.Length > 5000) // Limit body size
            return null;

        // Don't log request bodies for sensitive endpoints
        var sensitiveEndpoints = new[]
        {
            "/api/auth/login",
            "/api/auth/register", 
            "/api/auth/change-password",
            "/api/auth/reset-password",
            "/api/payment"
        };

        if (sensitiveEndpoints.Any(endpoint => path.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase)))
        {
            return "[FILTERED - SENSITIVE ENDPOINT]";
        }

        try
        {
            // Try to parse as JSON and filter sensitive fields
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            
            if (root.ValueKind == JsonValueKind.Object)
            {
                var sensitiveFields = new[] { "password", "currentPassword", "newPassword", "creditCard", "cvv", "ssn" };
                var filteredObj = new Dictionary<string, object>();
                
                foreach (var property in root.EnumerateObject())
                {
                    if (sensitiveFields.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        filteredObj[property.Name] = "[FILTERED]";
                    }
                    else
                    {
                        filteredObj[property.Name] = property.Value.ToString();
                    }
                }
                
                return JsonSerializer.Serialize(filteredObj);
            }
        }
        catch
        {
            // If JSON parsing fails, return truncated body
            return body.Length > 500 ? body[..500] + "..." : body;
        }

        return body;
    }

    private static bool ShouldCaptureRequestBody(HttpContext context)
    {
        var method = context.Request.Method;
        var contentType = context.Request.ContentType?.ToLowerInvariant();
        
        // Only capture for modification operations with JSON content
        return (method == "POST" || method == "PUT" || method == "PATCH") &&
               contentType != null && 
               (contentType.Contains("application/json") || contentType.Contains("application/x-www-form-urlencoded")) &&
               context.Request.ContentLength > 0 &&
               context.Request.ContentLength < 10000; // Limit size
    }

    private static (string category, string type) DetermineEventCategoryAndType(string path, string method, int statusCode)
    {
        if (path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase))
        {
            return ("authentication", $"auth_{method.ToLowerInvariant()}");
        }
        
        if (path.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase))
        {
            return ("administration", $"admin_{method.ToLowerInvariant()}");
        }
        
        if (path.Contains("/security", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/audit", StringComparison.OrdinalIgnoreCase))
        {
            return ("security", $"security_{method.ToLowerInvariant()}");
        }

        if (method == "GET")
        {
            return ("data_access", "read");
        }
        
        if (method == "POST")
        {
            return ("data_modification", "create");
        }
        
        if (method == "PUT" || method == "PATCH")
        {
            return ("data_modification", "update");
        }
        
        if (method == "DELETE")
        {
            return ("data_modification", "delete");
        }

        return ("api_access", "unknown");
    }

    private static (string entityType, string? entityId) ExtractEntityFromPath(string path)
    {
        try
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            if (segments.Length >= 3 && segments[0] == "api")
            {
                var entityType = segments[1];
                
                // Try to extract ID from path
                if (segments.Length >= 3 && int.TryParse(segments[2], out _))
                {
                    return (entityType, segments[2]);
                }
                
                return (entityType, null);
            }
            
            return ("unknown", null);
        }
        catch
        {
            return ("unknown", null);
        }
    }

    private static string CreateDescription(RequestDetails details, int statusCode, Exception? exception)
    {
        var action = $"{details.Method} {details.Path}";
        var user = !string.IsNullOrEmpty(details.UserEmail) ? $" by {details.UserEmail}" : 
                  !string.IsNullOrEmpty(details.UserId) ? $" by user {details.UserId}" : "";
        
        if (exception != null)
        {
            return $"{action}{user} failed with exception: {exception.GetType().Name}";
        }
        
        var statusDescription = statusCode switch
        {
            >= 200 and < 300 => "succeeded",
            >= 400 and < 500 => "failed (client error)",
            >= 500 => "failed (server error)",
            _ => $"completed with status {statusCode}"
        };
        
        return $"{action}{user} {statusDescription}";
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
            "/api/auth/refresh", // Don't log refresh token calls
            "/static",
            "/_next"
        };

        return excludedPaths.Any(excluded => path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
    }

    private class RequestDetails
    {
        public string Method { get; set; } = "";
        public string Path { get; set; } = "";
        public string QueryString { get; set; } = "";
        public string? UserAgent { get; set; }
        public string IPAddress { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? UserRole { get; set; }
        public bool IsAdmin { get; set; }
        public string? RequestBody { get; set; }
    }
}

/// <summary>
/// Extension method to add audit logging middleware to the pipeline
/// </summary>
public static class AuditLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuditLoggingMiddleware>();
    }
}