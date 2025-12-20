using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Core.ValueObjects;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace EcommerceLaptop.Infrastructure.Services.Security;

/// <summary>
/// Comprehensive security event service for logging and monitoring security-related activities
/// Handles both automated security events and manual investigations
/// </summary>

public class SecurityEventService : ISecurityEventService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SecurityEventService> _logger;

    public SecurityEventService(ApplicationDbContext context, ILogger<SecurityEventService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<SecurityEvent>> LogEventAsync(SecurityEvent securityEvent)
    {
        try
        {
            securityEvent.CreatedAt = DateTime.UtcNow;

            // Auto-generate correlation ID if not provided
            if (string.IsNullOrEmpty(securityEvent.CorrelationId))
            {
                securityEvent.CorrelationId = Guid.NewGuid().ToString("N")[..16];
            }

            _context.SecurityEvents.Add(securityEvent);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Security event logged: {EventType} from {IPAddress} - {Description}",
                securityEvent.EventType, securityEvent.IPAddress, securityEvent.Description);

            return ServiceResult<SecurityEvent>.Success(securityEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging security event: {EventType}", securityEvent.EventType);
            return ServiceResult<SecurityEvent>.Failure("Failed to log security event");
        }
    }

    public async Task<ServiceResult<List<SecurityEvent>>> GetEventsAsync(
        int skip = 0,
        int take = 50,
        string? eventType = null,
        string? severity = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        try
        {
            var query = _context.SecurityEvents
                .Include(e => e.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(eventType))
            {
                query = query.Where(e => e.EventType == eventType);
            }

            if (!string.IsNullOrEmpty(severity))
            {
                query = query.Where(e => e.Severity == severity);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(e => e.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(e => e.CreatedAt <= toDate.Value);
            }

            var events = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return ServiceResult<List<SecurityEvent>>.Success(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security events");
            return ServiceResult<List<SecurityEvent>>.Failure("Failed to retrieve security events");
        }
    }

    public async Task<ServiceResult<SecurityEvent>> UpdateEventAsync(int eventId, string status, string? investigatedBy = null, string? notes = null)
    {
        try
        {
            var securityEvent = await _context.SecurityEvents.FindAsync(eventId);
            if (securityEvent == null)
            {
                return ServiceResult<SecurityEvent>.Failure("Security event not found");
            }

            securityEvent.Status = status;
            securityEvent.InvestigatedBy = investigatedBy;
            securityEvent.InvestigationNotes = notes;
            securityEvent.InvestigatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ServiceResult<SecurityEvent>.Success(securityEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating security event {EventId}", eventId);
            return ServiceResult<SecurityEvent>.Failure("Failed to update security event");
        }
    }

    public async Task<ServiceResult<Dictionary<string, int>>> GetEventStatisticsAsync(int days = 7)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            var stats = await _context.SecurityEvents
                .Where(e => e.CreatedAt >= cutoffDate)
                .GroupBy(e => e.EventType)
                .Select(g => new { EventType = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EventType, x => x.Count);

            return ServiceResult<Dictionary<string, int>>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security event statistics");
            return ServiceResult<Dictionary<string, int>>.Failure("Failed to get statistics");
        }
    }

    public async Task<ServiceResult<List<SecurityEvent>>> GetEventsByIPAsync(string ipAddress, int hours = 24)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-hours);

            var events = await _context.SecurityEvents
                .Where(e => e.IPAddress == ipAddress && e.CreatedAt >= cutoffTime)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return ServiceResult<List<SecurityEvent>>.Success(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security events for IP {IPAddress}", ipAddress);
            return ServiceResult<List<SecurityEvent>>.Failure("Failed to get events for IP");
        }
    }

    public async Task<ServiceResult<List<SecurityEvent>>> GetRelatedEventsAsync(int eventId)
    {
        try
        {
            var originalEvent = await _context.SecurityEvents.FindAsync(eventId);
            if (originalEvent == null)
            {
                return ServiceResult<List<SecurityEvent>>.Failure("Security event not found");
            }

            var relatedEvents = await _context.SecurityEvents
                .Where(e => e.Id != eventId &&
                           (e.CorrelationId == originalEvent.CorrelationId ||
                            e.IPAddress == originalEvent.IPAddress ||
                            e.UserId == originalEvent.UserId ||
                            e.SessionId == originalEvent.SessionId))
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return ServiceResult<List<SecurityEvent>>.Success(relatedEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting related events for {EventId}", eventId);
            return ServiceResult<List<SecurityEvent>>.Failure("Failed to get related events");
        }
    }

    public async Task<ServiceResult<bool>> MarkEventAsResolvedAsync(int eventId, string resolvedBy, string resolution)
    {
        try
        {
            var securityEvent = await _context.SecurityEvents.FindAsync(eventId);
            if (securityEvent == null)
            {
                return ServiceResult<bool>.Failure("Security event not found");
            }

            securityEvent.Status = "resolved";
            securityEvent.InvestigatedBy = resolvedBy;
            securityEvent.InvestigationNotes = resolution;
            securityEvent.InvestigatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking security event {EventId} as resolved", eventId);
            return ServiceResult<bool>.Failure("Failed to mark event as resolved");
        }
    }

    // Additional interface methods implementation

    /// <summary>
    /// Log security event with interface signature
    /// </summary>
    public async Task<ServiceResult<bool>> LogEventAsync(string eventType, string description, int? userId = null, int? adminUserId = null, string? ipAddress = null, string? userAgent = null, string? correlationId = null, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var securityEvent = new SecurityEvent
            {
                EventType = eventType,
                Description = description,
                UserId = userId,
                AdminUserId = adminUserId,
                IPAddress = ipAddress ?? "Unknown",
                UserAgent = userAgent,
                CorrelationId = correlationId ?? Guid.NewGuid().ToString(),
                Details = metadata != null ? JsonSerializer.Serialize(metadata) : "{}",
                CreatedAt = DateTime.UtcNow,
                Status = "new",
                Severity = DetermineSeverity(eventType)
            };

            _context.SecurityEvents.Add(securityEvent);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging security event");
            return ServiceResult<bool>.Failure("Failed to log security event");
        }
    }

    /// <summary>
    /// Get security events with enhanced filtering
    /// </summary>
    public async Task<ServiceResult<List<SecurityEvent>>> GetEventsAsync(string? eventType = null, DateTime? from = null, DateTime? to = null, int? userId = null, int? adminUserId = null, int page = 1, int pageSize = 50)
    {
        try
        {
            var query = _context.SecurityEvents.AsQueryable();

            if (!string.IsNullOrEmpty(eventType))
                query = query.Where(e => e.EventType == eventType);

            if (from.HasValue)
                query = query.Where(e => e.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(e => e.CreatedAt <= to.Value);

            if (userId.HasValue)
                query = query.Where(e => e.UserId == userId.Value);

            if (adminUserId.HasValue)
                query = query.Where(e => e.AdminUserId == adminUserId.Value);

            var skip = (page - 1) * pageSize;
            var events = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return ServiceResult<List<SecurityEvent>>.Success(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security events");
            return ServiceResult<List<SecurityEvent>>.Failure("Failed to retrieve security events");
        }
    }

    /// <summary>
    /// Get events by correlation ID
    /// </summary>
    public async Task<ServiceResult<List<SecurityEvent>>> GetEventsByCorrelationAsync(string correlationId)
    {
        try
        {
            var events = await _context.SecurityEvents
                .Where(e => e.CorrelationId == correlationId)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync();

            return ServiceResult<List<SecurityEvent>>.Success(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events by correlation ID {CorrelationId}", correlationId);
            return ServiceResult<List<SecurityEvent>>.Failure("Failed to retrieve correlated events");
        }
    }

    /// <summary>
    /// Get security event statistics
    /// </summary>
    public async Task<ServiceResult<Dictionary<string, int>>> GetEventStatisticsAsync(DateTime from, DateTime to)
    {
        try
        {
            var stats = new Dictionary<string, int>
            {
                ["TotalEvents"] = await _context.SecurityEvents
                    .Where(e => e.CreatedAt >= from && e.CreatedAt <= to)
                    .CountAsync(),
                ["CriticalEvents"] = await _context.SecurityEvents
                    .Where(e => e.CreatedAt >= from && e.CreatedAt <= to && e.Severity == "Critical")
                    .CountAsync(),
                ["HighSeverityEvents"] = await _context.SecurityEvents
                    .Where(e => e.CreatedAt >= from && e.CreatedAt <= to && e.Severity == "High")
                    .CountAsync(),
                ["UnresolvedEvents"] = await _context.SecurityEvents
                    .Where(e => e.CreatedAt >= from && e.CreatedAt <= to && e.Status != "resolved")
                    .CountAsync(),
                ["UniqueIPs"] = await _context.SecurityEvents
                    .Where(e => e.CreatedAt >= from && e.CreatedAt <= to)
                    .Select(e => e.IPAddress)
                    .Distinct()
                    .CountAsync()
            };

            return ServiceResult<Dictionary<string, int>>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security event statistics");
            return ServiceResult<Dictionary<string, int>>.Failure("Failed to retrieve statistics");
        }
    }

    /// <summary>
    /// Mark security event as investigated
    /// </summary>
    public async Task<ServiceResult<bool>> MarkEventInvestigatedAsync(int eventId, int adminUserId, string notes)
    {
        try
        {
            var securityEvent = await _context.SecurityEvents.FindAsync(eventId);
            if (securityEvent == null)
            {
                return ServiceResult<bool>.Failure("Security event not found");
            }

            securityEvent.Status = "resolved";
            securityEvent.InvestigatedAt = DateTime.UtcNow;
            securityEvent.InvestigatedBy = adminUserId.ToString();
            securityEvent.InvestigationNotes = notes;

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking security event {EventId} as investigated", eventId);
            return ServiceResult<bool>.Failure("Failed to mark event as investigated");
        }
    }

    /// <summary>
    /// Get suspicious activity patterns
    /// </summary>
    public async Task<ServiceResult<List<SecurityEvent>>> GetSuspiciousActivityAsync(DateTime from, DateTime to, int threshold = 10)
    {
        try
        {
            var suspiciousEvents = await _context.SecurityEvents
                .Where(e => e.CreatedAt >= from && e.CreatedAt <= to)
                .GroupBy(e => e.IPAddress)
                .Where(g => g.Count() >= threshold)
                .SelectMany(g => g)
                .OrderByDescending(e => e.CreatedAt)
                .Take(100)
                .ToListAsync();

            return ServiceResult<List<SecurityEvent>>.Success(suspiciousEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving suspicious activity");
            return ServiceResult<List<SecurityEvent>>.Failure("Failed to retrieve suspicious activity");
        }
    }

    /// <summary>
    /// Create alert rule (placeholder implementation)
    /// </summary>
    public async Task<ServiceResult<bool>> CreateAlertRuleAsync(string name, string eventType, int threshold, TimeSpan window, string? description = null, int? adminUserId = null)
    {
        try
        {
            // This would typically create an alert rule in a monitoring system
            // For now, just log the creation
            _logger.LogInformation("Alert rule created: {Name} for event type {EventType} with threshold {Threshold} over {Window}",
                name, eventType, threshold, window);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating alert rule");
            return ServiceResult<bool>.Failure("Failed to create alert rule");
        }
    }

    /// <summary>
    /// Delete old security events
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteOldEventsAsync(DateTime olderThan)
    {
        try
        {
            var oldEvents = await _context.SecurityEvents
                .Where(e => e.CreatedAt < olderThan)
                .ToListAsync();

            _context.SecurityEvents.RemoveRange(oldEvents);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted {Count} old security events", oldEvents.Count);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting old security events");
            return ServiceResult<bool>.Failure("Failed to delete old events");
        }
    }

    /// <summary>
    /// Get aggregated security metrics for the admin dashboard
    /// </summary>
    public async Task<ServiceResult<EcommerceLaptop.Core.DTOs.Admin.SecurityMetricsDto>> GetSecurityMetricsAsync(int days = 7)
    {
        try
        {
            var from = DateTime.UtcNow.AddDays(-days);
            var to = DateTime.UtcNow;

            // Totals
            var totalBlocked = await _context.SecurityEvents
                .Where(e => e.EventType == "ip_blocked" && e.CreatedAt >= from && e.CreatedAt <= to)
                .CountAsync();

            var rateLimitViolations = await _context.SecurityEvents
                .Where(e => e.EventType == "rate_limit_exceeded" && e.CreatedAt >= from && e.CreatedAt <= to)
                .CountAsync();

            var activeRules = await _context.RateLimitRules
                .Where(r => r.IsActive)
                .CountAsync();

            var suspiciousIPsCount = await _context.SecurityEvents
                .Where(e => e.CreatedAt >= from && (e.EventType == "rate_limit_exceeded" || e.EventType == "failed_login_attempt" || e.EventType == "suspicious_activity"))
                .Select(e => e.IPAddress)
                .Distinct()
                .CountAsync();

            var todayStart = DateTime.UtcNow.Date;
            var todayBlocked = await _context.SecurityEvents
                .Where(e => e.EventType == "ip_blocked" && e.CreatedAt >= todayStart)
                .CountAsync();

            // Top blocked IPs
            var topBlockedIPsRaw = await _context.SecurityEvents
                .Where(e => e.EventType == "ip_blocked" && e.CreatedAt >= from && e.CreatedAt <= to)
                .GroupBy(e => e.IPAddress)
                .Select(g => new { ip = g.Key, count = g.Count(), lastSeen = g.Max(x => x.CreatedAt) })
                .OrderByDescending(x => x.count)
                .Take(10)
                .ToListAsync();

            // Rate limit stats per endpoint (Legacy table removed, return empty or derive from SecurityEvents if needed)
            // For now returning empty list as precise endpoint aggregation from generic SecurityEvents description is complex
            var rateLimitStatsRaw = new List<dynamic>();

            var metrics = new EcommerceLaptop.Core.DTOs.Admin.SecurityMetricsDto
            {
                TotalBlocked = totalBlocked,
                RateLimitViolations = rateLimitViolations,
                ActiveRules = activeRules,
                SuspiciousIPs = suspiciousIPsCount,
                TodayBlocked = todayBlocked,
                TopBlockedIPs = topBlockedIPsRaw.Select(x => new EcommerceLaptop.Core.DTOs.Admin.TopBlockedIpDto
                {
                    Ip = x.ip ?? string.Empty,
                    Count = x.count,
                    LastSeen = x.lastSeen
                }).ToList(),
                RateLimitStats = new List<EcommerceLaptop.Core.DTOs.Admin.EndpointRateLimitStatDto>()
            };

            return ServiceResult<EcommerceLaptop.Core.DTOs.Admin.SecurityMetricsDto>.Success(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating security metrics");
            return ServiceResult<EcommerceLaptop.Core.DTOs.Admin.SecurityMetricsDto>.Failure("Failed to retrieve security metrics");
        }
    }

    /// <summary>
    /// Determine event severity based on event type
    /// </summary>
    private static string DetermineSeverity(string eventType)
    {
        return eventType.ToLower() switch
        {
            "ip_blocked" => "High",
            "rate_limit_exceeded" => "Medium",
            "suspicious_activity" => "High",
            "login_attempt" => "Low",
            "admin_action" => "Medium",
            _ => "Medium"
        };
    }
}

/// <summary>
/// Comprehensive audit logging service for tracking all system activities
/// Supports both admin and user actions with detailed metadata
/// </summary>