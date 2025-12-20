using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.ValueObjects;

namespace EcommerceLaptop.Infrastructure.Services.Security;

/// <summary>
/// Service for comprehensive audit logging of user and admin activities
/// Uses existing AdminAuditLog entity for consistency with existing system
/// </summary>
public interface IAuditLoggingService
{
    Task<ServiceResult<bool>> LogUserActivityAsync(int userId, string activity, string details, string ipAddress, string userAgent);
    Task<ServiceResult<bool>> LogAdminActivityAsync(int adminId, string activity, string details, string ipAddress, string userAgent, string? targetResource = null);
    Task<ServiceResult<bool>> LogSystemEventAsync(string eventType, string description, string? correlationId = null, Dictionary<string, object>? metadata = null);
    Task<ServiceResult<bool>> LogSecurityEventAsync(string eventType, string description, string ipAddress, int? userId = null, int? adminId = null, string? correlationId = null);

    Task<ServiceResult<Dictionary<string, int>>> GetActivitySummaryAsync(DateTime from, DateTime to, string? userType = null);
    Task<ServiceResult<bool>> DeleteOldLogsAsync(DateTime olderThan);
    Task LogEventAsync(string entityType, string entityId, string eventType, string eventCategory, object? oldValues, object? newValues, string? userId, string? adminUserId, string? ipAddress, string? userAgent, object? metadata);
}