using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Core.ValueObjects;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services.Security;

public class AuditLoggingService : IAuditLoggingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditLoggingService> _logger;

    public AuditLoggingService(ApplicationDbContext context, ILogger<AuditLoggingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<bool>> LogUserActivityAsync(int userId, string activity, string details, string ipAddress, string userAgent)
    {
        try
        {
            _logger.LogInformation("Logging user activity: UserId={UserId}, Activity={Activity}", userId, activity);

            // Verify that the user exists in the Users table
            var user = await _context.Users
                .Where(u => u.Id == userId)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found. Skipping user activity log.", userId);
                return ServiceResult<bool>.Success(false); // Don't fail the operation, just skip logging
            }

            var auditLog = new SystemAuditLog
            {
                EventCategory = "user_activity",
                EventType = activity,
                EntityType = "User",
                EntityId = userId.ToString(),
                Action = activity,
                Description = details,
                UserId = userId,
                UserRole = user.IsAdminRole ? "Admin" : "User", // Use actual user role
                IPAddress = ipAddress,
                UserAgent = userAgent,
                RiskLevel = "low",
                RequiresReview = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.SystemAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User activity logged successfully: UserId={UserId}, Activity={Activity}", userId, activity);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log user activity");
            return ServiceResult<bool>.Failure("Failed to log user activity");
        }
    }

    public async Task<ServiceResult<bool>> LogAdminActivityAsync(int adminId, string activity, string details, string ipAddress, string userAgent, string? targetResource = null)
    {
        try
        {
            _logger.LogInformation("Logging admin activity: AdminId={AdminId}, Activity={Activity}", adminId, activity);

            // Verify that the admin user exists in the Users table
            var adminUser = await _context.Users
                .Where(u => u.Id == adminId && u.IsAdminRole == true)
                .FirstOrDefaultAsync();

            if (adminUser == null)
            {
                _logger.LogWarning("Admin user with ID {AdminId} not found or not an admin. Skipping audit log.", adminId);
                return ServiceResult<bool>.Success(false); // Don't fail the operation, just skip logging
            }

            var auditLog = new SystemAuditLog
            {
                EventCategory = "admin_activity",
                EventType = activity,
                EntityType = targetResource ?? "Admin",
                EntityId = adminId.ToString(),
                Action = activity,
                Description = details,
                UserId = adminId, // This references the admin user in the unified Users table
                AdminUserId = adminId, // This also references the same admin user
                UserRole = "Admin",
                IPAddress = ipAddress,
                UserAgent = userAgent,
                RiskLevel = "medium", // Admin activities have higher risk level
                RequiresReview = activity.Contains("delete", StringComparison.OrdinalIgnoreCase) ||
                                activity.Contains("disable", StringComparison.OrdinalIgnoreCase),
                CreatedAt = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(targetResource))
            {
                auditLog.AdditionalMetadata = $"{{\"targetResource\":\"{targetResource}\"}}";
            }

            _context.SystemAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin activity logged successfully: AdminId={AdminId}, Activity={Activity}", adminId, activity);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log admin activity");
            return ServiceResult<bool>.Failure("Failed to log admin activity");
        }
    }

    public async Task<ServiceResult<bool>> LogSystemEventAsync(string eventType, string description, string? correlationId = null, Dictionary<string, object>? metadata = null)
    {
        try
        {
            _logger.LogInformation("Logging system event: EventType={EventType}", eventType);

            var auditLog = new SystemAuditLog
            {
                EventCategory = "system",
                EventType = eventType,
                EntityType = "System",
                EntityId = correlationId,
                Action = eventType,
                Description = description,
                RiskLevel = "low", // System events are typically low risk
                RequiresReview = false,
                AdditionalMetadata = metadata != null ? System.Text.Json.JsonSerializer.Serialize(metadata) : null,
                CreatedAt = DateTime.UtcNow
            };

            _context.SystemAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("System event logged successfully: EventType={EventType}", eventType);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log system event");
            return ServiceResult<bool>.Failure("Failed to log system event");
        }
    }

    public async Task<ServiceResult<bool>> LogSecurityEventAsync(string eventType, string description, string ipAddress, int? userId = null, int? adminId = null, string? correlationId = null)
    {
        try
        {
            _logger.LogWarning("Logging security event: EventType={EventType}, IP={IpAddress}", eventType, ipAddress);

            // Determine risk level based on event type
            var riskLevel = GetSecurityEventRiskLevel(eventType);
            var requiresReview = riskLevel == "high" || riskLevel == "critical";

            // Verify admin user exists if adminId is provided
            if (adminId.HasValue)
            {
                var adminUser = await _context.Users
                    .Where(u => u.Id == adminId.Value && u.IsAdminRole == true)
                    .FirstOrDefaultAsync();

                if (adminUser == null)
                {
                    _logger.LogWarning("Admin user with ID {AdminId} not found or not an admin. Skipping security event log.", adminId);
                    return ServiceResult<bool>.Success(false); // Don't fail the operation, just skip logging
                }
            }

            // Verify regular user exists if userId is provided
            if (userId.HasValue)
            {
                var user = await _context.Users
                    .Where(u => u.Id == userId.Value)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found. Skipping security event log.", userId);
                    return ServiceResult<bool>.Success(false); // Don't fail the operation, just skip logging
                }
            }

            // Log to SecurityEvent table for security-specific events
            var securityEvent = new SecurityEvent
            {
                EventType = eventType,
                Description = description,
                IPAddress = ipAddress,
                UserId = userId ?? adminId,
                Severity = GetSecurityEventSeverity(eventType),
                Status = "logged",
                CorrelationId = correlationId ?? Guid.NewGuid().ToString(),
                RiskScore = GetRiskScore(eventType).ToString(),
                CreatedAt = DateTime.UtcNow
            };

            _context.SecurityEvents.Add(securityEvent);

            // Also log to SystemAuditLog for comprehensive audit trail
            var auditLog = new SystemAuditLog
            {
                EventCategory = "security",
                EventType = eventType,
                EntityType = "Security",
                EntityId = securityEvent.CorrelationId,
                Action = eventType,
                Description = description,
                UserId = userId,
                AdminUserId = adminId,
                UserRole = adminId.HasValue ? "Admin" : "User",
                IPAddress = ipAddress,
                RiskLevel = riskLevel,
                RequiresReview = requiresReview,
                AdditionalMetadata = correlationId != null ? $"{{\"correlationId\":\"{correlationId}\"}}" : null,
                CreatedAt = DateTime.UtcNow
            };

            _context.SystemAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Security event logged successfully: EventType={EventType}, RiskLevel={RiskLevel}", eventType, riskLevel);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event");
            return ServiceResult<bool>.Failure("Failed to log security event");
        }
    }

    private static string GetSecurityEventRiskLevel(string eventType)
    {
        return eventType.ToLower() switch
        {
            var e when e.Contains("breach") || e.Contains("attack") || e.Contains("intrusion") => "critical",
            var e when e.Contains("unauthorized") || e.Contains("suspicious") || e.Contains("blocked") => "high",
            var e when e.Contains("failed_login") || e.Contains("rate_limit") => "medium",
            _ => "low"
        };
    }

    private static string GetSecurityEventSeverity(string eventType)
    {
        return eventType.ToLower() switch
        {
            var e when e.Contains("breach") || e.Contains("attack") => "critical",
            var e when e.Contains("unauthorized") || e.Contains("suspicious") => "high",
            var e when e.Contains("blocked") || e.Contains("failed") => "medium",
            _ => "low"
        };
    }

    private static int GetRiskScore(string eventType)
    {
        return eventType.ToLower() switch
        {
            var e when e.Contains("breach") || e.Contains("attack") => 90,
            var e when e.Contains("unauthorized") || e.Contains("intrusion") => 80,
            var e when e.Contains("suspicious") || e.Contains("blocked") => 60,
            var e when e.Contains("failed_login") => 40,
            _ => 20
        };
    }

    public async Task<ServiceResult<List<SystemAuditLog>>> GetUserAuditLogsAsync(int userId, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50)
    {
        try
        {
            _logger.LogInformation("Getting audit logs for user {UserId}", userId);

            var query = _context.SystemAuditLogs
                .AsNoTracking()
                .Where(log => log.UserId == userId);

            // Apply date filters
            if (from.HasValue)
            {
                query = query.Where(log => log.CreatedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(log => log.CreatedAt <= to.Value);
            }

            var logs = await query
                .OrderByDescending(log => log.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(log => log.User)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} audit logs for user {UserId}", logs.Count, userId);
            return ServiceResult<List<SystemAuditLog>>.Success(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user audit logs");
            return ServiceResult<List<SystemAuditLog>>.Failure("Failed to get user audit logs");
        }
    }

    public async Task<ServiceResult<List<SystemAuditLog>>> GetAdminAuditLogsAsync(int? adminId = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50)
    {
        try
        {
            _logger.LogInformation("Getting admin audit logs for adminId: {AdminId}", adminId);

            var query = _context.SystemAuditLogs
                .AsNoTracking()
                .Where(log => log.UserRole == "Admin" || log.AdminUserId.HasValue);

            // Filter by specific admin if provided
            if (adminId.HasValue)
            {
                query = query.Where(log => log.AdminUserId == adminId || log.UserId == adminId);
            }

            // Apply date filters
            if (from.HasValue)
            {
                query = query.Where(log => log.CreatedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(log => log.CreatedAt <= to.Value);
            }

            var logs = await query
                .OrderByDescending(log => log.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(log => log.AdminUser)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} admin audit logs", logs.Count);
            return ServiceResult<List<SystemAuditLog>>.Success(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get admin audit logs");
            return ServiceResult<List<SystemAuditLog>>.Failure("Failed to get admin audit logs");
        }
    }

    public async Task<ServiceResult<List<SystemAuditLog>>> SearchAuditLogsAsync(string? activity = null, string? ipAddress = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50)
    {
        try
        {
            _logger.LogInformation("Searching audit logs with filters - Activity: {Activity}, IP: {IpAddress}, From: {From}, To: {To}, Page: {Page}",
                activity, ipAddress, from, to, page);

            var query = _context.SystemAuditLogs
                .AsNoTracking() // Read-only optimization
                .AsQueryable();

            // Apply filters first for early reduction
            if (!string.IsNullOrEmpty(activity))
            {
                query = query.Where(log =>
                    EF.Functions.Like(log.Action, $"%{activity}%") ||
                    EF.Functions.Like(log.Description, $"%{activity}%") ||
                    EF.Functions.Like(log.EventType, $"%{activity}%"));
            }

            if (!string.IsNullOrEmpty(ipAddress))
            {
                query = query.Where(log => log.IPAddress == ipAddress);
            }

            if (from.HasValue)
            {
                query = query.Where(log => log.CreatedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(log => log.CreatedAt <= to.Value);
            }

            // Apply pagination and ordering
            var logs = await query
                .OrderByDescending(log => log.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(log => log.User) // Only include if needed; consider projecting to avoid full User load
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} audit logs", logs.Count);
            return ServiceResult<List<SystemAuditLog>>.Success(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search audit logs");
            return ServiceResult<List<SystemAuditLog>>.Failure("Failed to search audit logs");
        }
    }

    public async Task<ServiceResult<Dictionary<string, int>>> GetActivitySummaryAsync(DateTime from, DateTime to, string? userType = null)
    {
        try
        {
            _logger.LogInformation("Getting activity summary from {From} to {To} for userType: {UserType}", from, to, userType);

            var query = _context.SystemAuditLogs
                .Where(log => log.CreatedAt >= from && log.CreatedAt <= to);

            // Filter by user type if specified
            if (!string.IsNullOrEmpty(userType))
            {
                query = query.Where(log => log.UserRole == userType);
            }

            // Group by event type and count
            var summary = await query
                .GroupBy(log => log.EventType)
                .Select(g => new { EventType = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EventType, x => x.Count);

            _logger.LogInformation("Retrieved activity summary with {Count} event types", summary.Count);
            return ServiceResult<Dictionary<string, int>>.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get activity summary");
            return ServiceResult<Dictionary<string, int>>.Failure("Failed to get activity summary");
        }
    }

    public async Task<ServiceResult<bool>> DeleteOldLogsAsync(DateTime olderThan)
    {
        try
        {
            _logger.LogInformation("Deleting audit logs older than {OlderThan}", olderThan);

            // Delete old SystemAuditLogs
            var oldAuditLogs = _context.SystemAuditLogs
                .Where(log => log.CreatedAt < olderThan);

            var auditLogsCount = await oldAuditLogs.CountAsync();
            _context.SystemAuditLogs.RemoveRange(oldAuditLogs);

            // Delete old SecurityEvents
            var oldSecurityEvents = _context.SecurityEvents
                .Where(se => se.CreatedAt < olderThan);

            var securityEventsCount = await oldSecurityEvents.CountAsync();
            _context.SecurityEvents.RemoveRange(oldSecurityEvents);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully deleted {AuditLogsCount} audit logs and {SecurityEventsCount} security events older than {OlderThan}",
                auditLogsCount, securityEventsCount, olderThan);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete old logs");
            return ServiceResult<bool>.Failure("Failed to delete old logs");
        }
    }

    public async Task LogEventAsync(string entityType, string entityId, string eventType, string eventCategory, object? oldValues, object? newValues, string? userId, string? adminUserId, string? ipAddress, string? userAgent, object? metadata)
    {
        try
        {
            _logger.LogInformation("Event logged: EntityType={EntityType}, EventType={EventType}", entityType, eventType);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log event: EntityType={EntityType}, EventType={EventType}", entityType, eventType);
        }
    }
}
