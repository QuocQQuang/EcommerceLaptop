using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Core.ValueObjects;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using EcommerceLaptop.Infrastructure.Services.Logging;

namespace EcommerceLaptop.Infrastructure.Services.Security;

/// <summary>
/// Hybrid security event service: Writes to Loki (via Serilog), Reads from Loki (via LokiClient)
/// Keeps SQL dependency only for legacy compatibility if needed or Metrics aggregation
/// </summary>

public class SecurityEventService : ISecurityEventService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SecurityEventService> _logger;
    private readonly ILokiClient _lokiClient;

    public SecurityEventService(
        ApplicationDbContext context,
        ILogger<SecurityEventService> logger,
        ILokiClient lokiClient)
    {
        _context = context;
        _logger = logger;
        _lokiClient = lokiClient;
    }

    public async Task<ServiceResult<SecurityEvent>> LogEventAsync(SecurityEvent securityEvent)
    {
        try
        {
            securityEvent.CreatedAt = DateTime.UtcNow;

            if (string.IsNullOrEmpty(securityEvent.CorrelationId))
            {
                securityEvent.CorrelationId = Guid.NewGuid().ToString("N")[..16];
            }

            // Write to Serilog (which pushes to Loki)
            // LOW-cardinality data goes to labels (via propertiesAsLabels config)
            // HIGH-cardinality data (IP, UserId, CorrelationId) goes into scope (structured properties)
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["EventType"] = securityEvent.EventType,
                ["Severity"] = securityEvent.Severity ?? "Info",
                // These are logged as structured properties, parseable by LokiClient
                ["IPAddress"] = securityEvent.IPAddress ?? "Unknown",
                ["UserId"] = securityEvent.UserId?.ToString() ?? "Anonymous",
                ["AdminUserId"] = securityEvent.AdminUserId?.ToString() ?? "",
                ["CorrelationId"] = securityEvent.CorrelationId ?? "",
                ["Details"] = securityEvent.Details ?? "",
                ["UserAgent"] = securityEvent.UserAgent ?? ""
            }))
            {
                // Log with Description as the main message (@m in Loki)
                // The scope properties above will be included in the JSON log but not in @m
                _logger.LogInformation("{Description}", securityEvent.Description);
            }

            // SecurityEvents are now logged exclusively to Loki via Serilog

            return ServiceResult<SecurityEvent>.Success(securityEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging security event: {EventType}", securityEvent.EventType);
            return ServiceResult<SecurityEvent>.Failure("Failed to log security event");
        }
    }

    public async Task<ServiceResult<EcommerceLaptop.Core.DTOs.PagedResult<SecurityEvent>>> GetEventsAsync(
        string? eventType = null,
        string? severity = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? userId = null,
        int? adminUserId = null,
        int page = 1,
        int pageSize = 50)
    {
        int skip = (page - 1) * pageSize;
        int take = pageSize;

        try
        {
            // Query Loki using Time Range (native to Loki)
            var from = fromDate ?? DateTime.UtcNow.AddDays(-7);
            var to = toDate ?? DateTime.UtcNow;

            // Build label selector query (only low-cardinality labels)
            // IMPORTANT: Always require EventType to exist, so we only get security logs
            // not general application logs (EF warnings, HTTP requests, etc.)
            var labels = new List<string> { "app=\"ecommerce-api\"" };

            if (!string.IsNullOrEmpty(eventType))
            {
                // Specific event type filter
                labels.Add($"EventType=\"{eventType}\"");
            }
            else
            {
                // If no specific eventType, still require EventType to exist (regex match any value)
                // This filters out logs without EventType label (general app logs)
                labels.Add("EventType=~\".+\"");
            }

            if (!string.IsNullOrEmpty(severity))
            {
                labels.Add($"Severity=\"{severity}\"");
            }

            var query = "{" + string.Join(", ", labels) + "}";

            // Get Total Count for pagination info
            var totalCount = await _lokiClient.CountAsync(query, from, to);

            // Time Range pagination: fetch with limit (native Loki approach)
            // For page > 1, we still need in-memory skip due to Loki limitations
            var limit = Math.Min(skip + take + 100, 1000);
            var events = await _lokiClient.QueryAsync(query, from, to, limit);

            // In-memory pagination (acceptable for admin dashboards with reasonable data)
            var pagedItems = events.Skip(skip).Take(take).ToList();

            var result = new EcommerceLaptop.Core.DTOs.PagedResult<SecurityEvent>
            {
                Items = pagedItems,
                TotalCount = Math.Max(totalCount, events.Count),
                Page = page,
                PageSize = take
            };

            return ServiceResult<EcommerceLaptop.Core.DTOs.PagedResult<SecurityEvent>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security events from Loki");
            return ServiceResult<EcommerceLaptop.Core.DTOs.PagedResult<SecurityEvent>>.Failure("Failed to retrieve security events");
        }
    }

    public async Task<ServiceResult<SecurityEvent>> UpdateEventAsync(int eventId, string status, string? investigatedBy = null, string? notes = null)
    {
        // Loki is immutable. We cannot update an event.
        // Instead, we log a NEW event indicating the update/resolution.

        var updateEvent = new SecurityEvent
        {
            EventType = "security_event_updated",
            Description = $"Security Event {eventId} status changed to {status}. Notes: {notes}",
            Severity = "Info",
            IPAddress = "Internal", // Admin action
            Details = JsonSerializer.Serialize(new { OriginalEventId = eventId, Status = status, Notes = notes, Admin = investigatedBy })
        };

        await LogEventAsync(updateEvent);

        // Return dummy event as we can't fetch the original "updated" one easily from ID
        return ServiceResult<SecurityEvent>.Success(updateEvent);
    }

    // ... [Rest of file needs similar updates or can be left if unused/legacy]
    // Consolidating repetitive logic...

    public async Task<ServiceResult<Dictionary<string, int>>> GetEventStatisticsAsync(int days = 7)
    {
        var to = DateTime.UtcNow;
        var from = to.AddDays(-Math.Max(days, 1));
        return await GetEventStatisticsAsync(from, to);
    }

    public async Task<ServiceResult<List<SecurityEvent>>> GetEventsByIPAsync(string ipAddress, int hours = 24)
    {
        try
        {
            var from = DateTime.UtcNow.AddHours(-hours);
            var to = DateTime.UtcNow;

            var query = "{app=\"ecommerce-api\", EventType=~\".+\"}";
            var events = await _lokiClient.QueryAsync(query, from, to, 1000);
            events = events
                .Where(e => string.Equals(e.IPAddress, ipAddress, StringComparison.OrdinalIgnoreCase))
                .Take(100)
                .ToList();

            return ServiceResult<List<SecurityEvent>>.Success(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting events by IP from Loki");
            return ServiceResult<List<SecurityEvent>>.Failure("Failed to retrieve events by IP");
        }
    }

    public async Task<ServiceResult<List<SecurityEvent>>> GetRelatedEventsAsync(int eventId)
    {
        try
        {
            var to = DateTime.UtcNow;
            var from = to.AddDays(-30);
            var query = "{app=\"ecommerce-api\", EventType=~\".+\"}";
            var events = await _lokiClient.QueryAsync(query, from, to, 1000);
            var source = events.FirstOrDefault(e => e.Id == eventId);

            if (source == null)
            {
                return ServiceResult<List<SecurityEvent>>.Success(new List<SecurityEvent>());
            }

            var related = events
                .Where(e => e.Id != source.Id)
                .Where(e =>
                    (!string.IsNullOrWhiteSpace(source.CorrelationId)
                        && string.Equals(e.CorrelationId, source.CorrelationId, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(source.IPAddress)
                        && source.IPAddress != "Unknown"
                        && string.Equals(e.IPAddress, source.IPAddress, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(e => e.CreatedAt)
                .Take(50)
                .ToList();

            return ServiceResult<List<SecurityEvent>>.Success(related);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting related security events for {EventId}", eventId);
            return ServiceResult<List<SecurityEvent>>.Failure("Failed to retrieve related events");
        }
    }

    public async Task<ServiceResult<bool>> MarkEventAsResolvedAsync(int eventId, string resolvedBy, string resolution)
    {
        var updateEvent = new SecurityEvent
        {
            EventType = "security_event_resolved",
            Description = $"Security Event {eventId} resolved by {resolvedBy}. Resolution: {resolution}",
            Severity = "Info",
            IPAddress = "Internal",
            Details = JsonSerializer.Serialize(new { OriginalEventId = eventId, Resolution = resolution, Admin = resolvedBy })
        };

        await LogEventAsync(updateEvent);
        return ServiceResult<bool>.Success(true);
    }

    // Interface implementation stubs for others...

    public async Task<ServiceResult<bool>> LogEventAsync(string eventType, string description, int? userId = null, int? adminUserId = null, string? ipAddress = null, string? userAgent = null, string? correlationId = null, Dictionary<string, object>? metadata = null)
    {
        var evt = new SecurityEvent
        {
            EventType = eventType,
            Description = description,
            UserId = userId,
            AdminUserId = adminUserId,
            IPAddress = ipAddress ?? "Unknown",
            UserAgent = userAgent,
            CorrelationId = correlationId,
            Details = metadata != null ? JsonSerializer.Serialize(metadata) : null,
            Severity = DetermineSeverity(eventType)
        };

        await LogEventAsync(evt);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<List<SecurityEvent>>> GetEventsAsync(string? eventType = null, DateTime? from = null, DateTime? to = null, int? userId = null, int? adminUserId = null, int page = 1, int pageSize = 50)
    {
        // Delegate to main implementation and extract items list
        var result = await GetEventsAsync(eventType: eventType, severity: null, fromDate: from, toDate: to, userId: userId, adminUserId: adminUserId, page: page, pageSize: pageSize);
        if (result.IsSuccess && result.Data != null)
        {
            return ServiceResult<List<SecurityEvent>>.Success(result.Data.Items);
        }
        return ServiceResult<List<SecurityEvent>>.Failure(result.ErrorMessage ?? "Failed to retrieve events");
    }

    public async Task<ServiceResult<List<SecurityEvent>>> GetEventsByCorrelationAsync(string correlationId)
    {
        try
        {
            var query = "{app=\"ecommerce-api\", EventType=~\".+\"}";
            var events = await _lokiClient.QueryAsync(query, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, 1000);
            events = events
                .Where(e => string.Equals(e.CorrelationId, correlationId, StringComparison.OrdinalIgnoreCase))
                .Take(100)
                .ToList();
            return ServiceResult<List<SecurityEvent>>.Success(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting events by correlation from Loki");
            return ServiceResult<List<SecurityEvent>>.Failure("Failed to retrieve correlated events");
        }
    }

    public async Task<ServiceResult<Dictionary<string, int>>> GetEventStatisticsAsync(DateTime from, DateTime to)
    {
        try
        {
            var stats = new Dictionary<string, int>();

            // Query counts for each common event type
            var eventTypes = new[] { "login_failed", "login_success", "ip_blocked", "rate_limit_exceeded", "suspicious_activity", "admin_action" };

            foreach (var eventType in eventTypes)
            {
                var query = $"{{app=\"ecommerce-api\", EventType=\"{eventType}\"}}";
                var count = await _lokiClient.CountAsync(query, from, to);
                if (count > 0)
                {
                    stats[eventType] = count;
                }
            }

            // Get total count
            var totalQuery = "{app=\"ecommerce-api\", EventType=~\".+\"}";
            stats["total"] = await _lokiClient.CountAsync(totalQuery, from, to);

            return ServiceResult<Dictionary<string, int>>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event statistics from Loki");
            return ServiceResult<Dictionary<string, int>>.Failure("Failed to retrieve event statistics");
        }
    }

    public async Task<ServiceResult<bool>> MarkEventInvestigatedAsync(int eventId, int adminUserId, string notes)
    {
        await UpdateEventAsync(eventId, "investigated", adminUserId.ToString(), notes);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<List<SecurityEvent>>> GetSuspiciousActivityAsync(DateTime from, DateTime to, int threshold = 10)
    {
        try
        {
            var query = "{app=\"ecommerce-api\", EventType=~\".+\"}";
            var events = await _lokiClient.QueryAsync(query, from, to, 1000);

            var suspiciousTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "failed_login",
                "failed_password_change",
                "rate_limit_exceeded",
                "ip_access_blocked",
                "ip_blocked",
                "http_404_not_found",
                "csp_violation"
            };

            var suspiciousIps = events
                .Where(e => !string.IsNullOrWhiteSpace(e.IPAddress) && e.IPAddress != "Unknown")
                .GroupBy(e => e.IPAddress, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count(e =>
                    suspiciousTypes.Contains(e.EventType)
                    || IsHighSeverity(e.Severity)) >= threshold)
                .Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var suspiciousEvents = events
                .Where(e =>
                    suspiciousTypes.Contains(e.EventType)
                    || IsHighSeverity(e.Severity)
                    || suspiciousIps.Contains(e.IPAddress))
                .OrderByDescending(e => e.CreatedAt)
                .Take(200)
                .ToList();

            return ServiceResult<List<SecurityEvent>>.Success(suspiciousEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suspicious activity from Loki");
            return ServiceResult<List<SecurityEvent>>.Failure("Failed to retrieve suspicious activity");
        }
    }

    public async Task<ServiceResult<bool>> CreateAlertRuleAsync(string name, string eventType, int threshold, TimeSpan window, string? description = null, int? adminUserId = null)
    {
        _logger.LogInformation("Alert rule created: {Name}", name);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> DeleteOldEventsAsync(DateTime olderThan)
    {
        // Loki handles retention. Cannot delete.
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<EcommerceLaptop.Core.DTOs.Admin.SecurityMetricsDto>> GetSecurityMetricsAsync(int days = 7)
    {
        try
        {
            // 1. Fetch Rules metrics from SQL
            var activeRules = await _context.RateLimitRules.CountAsync(r => r.IsActive);

            // 2. Fetch Event metrics from Loki
            var to = DateTime.UtcNow;
            var from = to.AddDays(-days);

            var securityEvents = await _lokiClient.QueryAsync("{app=\"ecommerce-api\", EventType=~\".+\"}", from, to, 1000);
            var blockedEvents = securityEvents
                .Where(e => e.EventType.Equals("ip_blocked", StringComparison.OrdinalIgnoreCase)
                    || e.EventType.Equals("ip_access_blocked", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var totalBlockedEvents = blockedEvents.Count;
            var rateLimitViolations = await _lokiClient.CountAsync($"{{app=\"ecommerce-api\", EventType=\"rate_limit_exceeded\"}}", from, to);

            var suspiciousCount = securityEvents
                .Where(e => !string.IsNullOrWhiteSpace(e.IPAddress) && e.IPAddress != "Unknown")
                .Where(e => IsHighSeverity(e.Severity)
                    || e.EventType.Equals("failed_login", StringComparison.OrdinalIgnoreCase)
                    || e.EventType.Equals("failed_password_change", StringComparison.OrdinalIgnoreCase)
                    || e.EventType.Equals("http_404_not_found", StringComparison.OrdinalIgnoreCase))
                .Select(e => e.IPAddress)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            var todayFrom = DateTime.UtcNow.Date;
            var todayBlocked = blockedEvents.Count(e => e.CreatedAt >= todayFrom);

            var topBlockedIps = blockedEvents
                .Where(e => !string.IsNullOrWhiteSpace(e.IPAddress) && e.IPAddress != "Unknown")
                .GroupBy(e => e.IPAddress, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new EcommerceLaptop.Core.DTOs.Admin.TopBlockedIpDto
                {
                    Ip = g.Key,
                    Count = g.Count(),
                    LastSeen = g.Max(e => e.CreatedAt)
                })
                .ToList();

            var rateLimitStats = securityEvents
                .Where(e => e.EventType.Equals("rate_limit_exceeded", StringComparison.OrdinalIgnoreCase))
                .GroupBy(e => ExtractDetailValue(e.Details, "endpoint") ?? ExtractDetailValue(e.Details, "Endpoint") ?? "Unknown")
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new EcommerceLaptop.Core.DTOs.Admin.EndpointRateLimitStatDto
                {
                    Endpoint = g.Key,
                    Violations = g.Count(),
                    LastViolation = g.Max(e => e.CreatedAt)
                })
                .ToList();

            return ServiceResult<EcommerceLaptop.Core.DTOs.Admin.SecurityMetricsDto>.Success(new EcommerceLaptop.Core.DTOs.Admin.SecurityMetricsDto
            {
                ActiveRules = activeRules,
                TotalBlocked = totalBlockedEvents,
                RateLimitViolations = rateLimitViolations,
                SuspiciousIPs = suspiciousCount,
                TodayBlocked = todayBlocked,
                TopBlockedIPs = topBlockedIps,
                RateLimitStats = rateLimitStats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security metrics");
            return ServiceResult<EcommerceLaptop.Core.DTOs.Admin.SecurityMetricsDto>.Failure("Failed to retrieve metrics");
        }
    }

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

    private static bool IsHighSeverity(string? severity)
    {
        return severity?.Equals("High", StringComparison.OrdinalIgnoreCase) == true
            || severity?.Equals("Critical", StringComparison.OrdinalIgnoreCase) == true
            || severity?.Equals("Error", StringComparison.OrdinalIgnoreCase) == true
            || severity?.Equals("Fatal", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string? ExtractDetailValue(string? details, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(details))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(details);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty(propertyName, out var property))
            {
                return property.ValueKind == JsonValueKind.String
                    ? property.GetString()
                    : property.ToString();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }
}

/// <summary>
/// Comprehensive audit logging service for tracking all system activities
/// Supports both admin and user actions with detailed metadata
/// </summary>
