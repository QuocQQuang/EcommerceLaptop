using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EcommerceLaptop.Infrastructure.Middleware;
using EcommerceLaptop.Infrastructure.Services.Security;
using System.Security.Claims;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Base controller implementing common functionality and patterns
/// Follows Clean Architecture and SOLID principles for maintainable API design
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public abstract class BaseApiController(ILogger logger) : ControllerBase
{
    protected readonly ILogger _logger = logger;

    /// <summary>
    /// Gets the current authenticated user ID from JWT claims
    /// </summary>
    protected int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Gets the current authenticated user email from JWT claims
    /// </summary>
    protected string? GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Gets the current user roles from JWT claims
    /// </summary>
    protected IEnumerable<string> GetCurrentUserRoles()
    {
        return User.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }

    /// <summary>
    /// Checks if current user has specific role
    /// </summary>
    protected bool HasRole(string role)
    {
        return User.IsInRole(role);
    }

    /// <summary>
    /// Checks if current user has any of the specified roles
    /// </summary>
    protected bool HasAnyRole(params string[] roles)
    {
        return roles.Any(role => User.IsInRole(role));
    }

    /// <summary>
    /// Gets client IP address from current HTTP request
    /// </summary>
    protected string GetClientIpAddress()
    {
        return HttpContext.GetClientIpAddress();
    }

    /// <summary>
    /// Gets User Agent from current HTTP request headers
    /// </summary>
    protected string GetUserAgent()
    {
        return HttpContext.Request.Headers["User-Agent"].ToString() ?? "Unknown";
    }

    /// <summary>
    /// Logs user activity with automatic IP and User Agent extraction
    /// </summary>
    protected async Task LogUserActivityAsync(IAuditLoggingService auditLoggingService, string activity, string details)
    {
        var userId = GetCurrentUserId();
        if (userId.HasValue)
        {
            await auditLoggingService.LogUserActivityAsync(
                userId.Value,
                activity,
                details,
                GetClientIpAddress(),
                GetUserAgent()
            );
        }
    }

    /// <summary>
    /// Logs admin activity with automatic IP and User Agent extraction
    /// </summary>
    protected async Task LogAdminActivityAsync(IAuditLoggingService auditLoggingService, string activity, string details, string? targetResource = null)
    {
        var adminUserId = GetCurrentUserId();
        if (adminUserId.HasValue && HasRole("Admin"))
        {
            await auditLoggingService.LogAdminActivityAsync(
                adminUserId.Value,
                activity,
                details,
                GetClientIpAddress(),
                GetUserAgent(),
                targetResource
            );
        }
    }

    /// <summary>
    /// Logs security event with automatic IP extraction
    /// </summary>
    protected async Task LogSecurityEventAsync(IAuditLoggingService auditLoggingService, string eventType, string description, string? correlationId = null)
    {
        var userId = GetCurrentUserId();
        await auditLoggingService.LogSecurityEventAsync(
            eventType,
            description,
            GetClientIpAddress(),
            userId: userId,
            correlationId: correlationId
        );
    }

    /// <summary>
    /// Creates a standardized success response
    /// </summary>
    protected IActionResult SuccessResponse<T>(T data, string? message = null)
    {
        return Ok(new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        });
    }

    /// <summary>
    /// Creates a standardized error response
    /// </summary>
    protected IActionResult ErrorResponse(string message, int statusCode = 400)
    {
        var response = new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Data = null
        };

        return statusCode switch
        {
            400 => BadRequest(response),
            401 => Unauthorized(response),
            403 => Forbid(),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => BadRequest(response)
        };
    }

    /// <summary>
    /// Handles exceptions centrally (Legacy - Use GlobalExceptionHandler instead)
    /// </summary>
    [Obsolete("Use GlobalExceptionHandler instead")]
    protected IActionResult HandleException(Exception ex, string? customMessage = null)
    {
        _logger.LogError(ex, "An error occurred while processing the request: {Message}", customMessage ?? "No details");
        return ErrorResponse(customMessage ?? ex.Message, 500);
    }

    /// <summary>
    /// Validates model state manually (Legacy - Use [ApiController] validation instead)
    /// </summary>
    [Obsolete("Use [ApiController] automatic validation instead")]
    protected IActionResult? ValidateModelState()
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            
            var message = string.Join("; ", errors);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Validation Failed: " + message,
                Data = null
            });
        }
        return null;
    }

    /// <summary>
    /// Creates a paginated response
    /// </summary>
    protected IActionResult PaginatedResponse<T>(IEnumerable<T> items, int totalCount, int page, int pageSize)
    {
        var response = new PaginatedApiResponse<T>
        {
            Success = true,
            Data = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            HasNextPage = page < (int)Math.Ceiling((double)totalCount / pageSize),
            HasPreviousPage = page > 1
        };

        return Ok(response);
    }

    /// <summary>
    /// Checks authorization for resource access
    /// </summary>
    protected bool CanAccessResource(int resourceUserId)
    {
        var currentUserId = GetCurrentUserId();
        
        // Admin can access all resources
        if (HasRole("Admin"))
            return true;
            
        // Users can only access their own resources
        return currentUserId == resourceUserId;
    }
}

#region Response DTOs

/// <summary>
/// Standard API response wrapper
/// </summary>
public record ApiResponse<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Paginated API response wrapper
/// </summary>
public record PaginatedApiResponse<T> : ApiResponse<IEnumerable<T>>
{
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
}

#endregion
