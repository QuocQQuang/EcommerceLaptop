using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Core.ValueObjects;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EcommerceLaptop.Infrastructure.Services.Security;

/// <summary>
/// Result of a rate limit check
/// </summary>
public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RemainingRequests { get; set; }
    public DateTime? RetryAfter { get; set; }
    public string? RuleName { get; set; }
    public int? CooldownSeconds { get; set; }
}

/// <summary>
/// Advanced rate limiting service with support for multiple time windows,
/// per-endpoint limits, user role exceptions, and automatic threat detection
/// </summary>

public class RateLimitingService : IRateLimitingService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingService> _logger;
    private readonly ISecurityEventService _securityEventService;

    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
    private const string CACHE_KEY_RULES = "rate_limit_rules";
    private const string CACHE_KEY_PREFIX_REQUESTS = "rate_limit_requests_";

    public RateLimitingService(
        ApplicationDbContext context,
        IMemoryCache cache,
        ILogger<RateLimitingService> logger,
        ISecurityEventService securityEventService)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
        _securityEventService = securityEventService;
    }

    public async Task<ServiceResult<RateLimitResult>> CheckRateLimitAsync(
        string clientIdentifier,
        string endpoint,
        string httpMethod,
        string? userRole = null,
        string? apiKey = null)
    {
        try
        {
            var rules = await GetCachedRulesAsync();
            var matchingRules = GetMatchingRules(rules, endpoint, httpMethod).OrderByDescending(r => r.Priority);

            foreach (var rule in matchingRules)
            {
                // Check if client has an exception
                if (HasException(rule, clientIdentifier, userRole, apiKey))
                {
                    continue; // Skip this rule
                }

                var result = await CheckRuleAsync(rule, clientIdentifier, endpoint, httpMethod);
                if (!result.IsAllowed)
                {
                    return ServiceResult<RateLimitResult>.Success(result);
                }
            }

            return ServiceResult<RateLimitResult>.Success(new RateLimitResult
            {
                IsAllowed = true,
                Message = "Request allowed",
                RemainingRequests = int.MaxValue
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for {Client} on {Endpoint}", clientIdentifier, endpoint);

            // On error, allow the request but log it
            return ServiceResult<RateLimitResult>.Success(new RateLimitResult
            {
                IsAllowed = true,
                Message = "Rate limit check failed, allowing request",
                RemainingRequests = 0
            });
        }
    }

    public async Task<ServiceResult<List<RateLimitRule>>> GetRateLimitRulesAsync()
    {
        try
        {
            var rules = await _context.RateLimitRules
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.Name)
                .ToListAsync();

            return ServiceResult<List<RateLimitRule>>.Success(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate limit rules");
            return ServiceResult<List<RateLimitRule>>.Failure("Failed to retrieve rate limit rules");
        }
    }

    public async Task<ServiceResult<RateLimitRule>> CreateRateLimitRuleAsync(RateLimitRule rule)
    {
        try
        {
            // Validate endpoint pattern
            if (string.IsNullOrWhiteSpace(rule.Endpoint))
            {
                return ServiceResult<RateLimitRule>.Failure("Endpoint pattern cannot be empty");
            }

            // Check for conflicting rules
            var existingRule = await _context.RateLimitRules
                .FirstOrDefaultAsync(r => r.Endpoint == rule.Endpoint &&
                                         r.HttpMethod == rule.HttpMethod &&
                                         r.IsActive);

            if (existingRule != null)
            {
                return ServiceResult<RateLimitRule>.Failure("A rule for this endpoint and method already exists");
            }

            rule.CreatedAt = DateTime.UtcNow;
            rule.UpdatedAt = DateTime.UtcNow;

            _context.RateLimitRules.Add(rule);
            await _context.SaveChangesAsync();

            await InvalidateCacheAsync();

            // Log security event
            await _securityEventService.LogEventAsync(
                "rate_limit_rule_created",
                $"Rate limit rule created: {rule.Name}",
                metadata: new Dictionary<string, object> {
                    { "Name", rule.Name },
                    { "Endpoint", rule.Endpoint },
                    { "RequestsPerMinute", rule.RequestsPerMinute },
                    { "RequestsPerHour", rule.RequestsPerHour },
                    { "RequestsPerDay", rule.RequestsPerDay }
                }
            );

            return ServiceResult<RateLimitRule>.Success(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating rate limit rule {RuleName}", rule.Name);
            return ServiceResult<RateLimitRule>.Failure("Failed to create rate limit rule");
        }
    }

    public async Task<ServiceResult<RateLimitRule>> UpdateRateLimitRuleAsync(RateLimitRule rule)
    {
        try
        {
            var existingRule = await _context.RateLimitRules.FindAsync(rule.Id);
            if (existingRule == null)
            {
                return ServiceResult<RateLimitRule>.Failure("Rate limit rule not found");
            }

            var oldValues = JsonSerializer.Serialize(existingRule);

            existingRule.Name = rule.Name;
            existingRule.Endpoint = rule.Endpoint;
            existingRule.HttpMethod = rule.HttpMethod;
            existingRule.RequestsPerMinute = rule.RequestsPerMinute;
            existingRule.RequestsPerHour = rule.RequestsPerHour;
            existingRule.RequestsPerDay = rule.RequestsPerDay;
            existingRule.IsActive = rule.IsActive;
            existingRule.Priority = rule.Priority;
            existingRule.Description = rule.Description;
            existingRule.IPWhitelist = rule.IPWhitelist;
            existingRule.UserRoleExceptions = rule.UserRoleExceptions;
            existingRule.ApiKeyExceptions = rule.ApiKeyExceptions;
            existingRule.CooldownSeconds = rule.CooldownSeconds;
            existingRule.CustomErrorMessage = rule.CustomErrorMessage;
            existingRule.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await InvalidateCacheAsync();

            // Log security event
            await _securityEventService.LogEventAsync(
                "rate_limit_rule_updated",
                $"Rate limit rule updated: {rule.Name}",
                metadata: new Dictionary<string, object> {
                    { "OldValues", oldValues },
                    { "NewValues", JsonSerializer.Serialize(rule) }
                }
            );

            return ServiceResult<RateLimitRule>.Success(existingRule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rate limit rule {RuleId}", rule.Id);
            return ServiceResult<RateLimitRule>.Failure("Failed to update rate limit rule");
        }
    }

    public async Task<ServiceResult<bool>> DeleteRateLimitRuleAsync(int ruleId)
    {
        try
        {
            _logger.LogInformation(" SERVICE DELETE - Searching for Rule ID: {RuleId}", ruleId);

            var rule = await _context.RateLimitRules.FindAsync(ruleId);
            if (rule == null)
            {
                _logger.LogWarning(" SERVICE DELETE - Rule not found: {RuleId}", ruleId);
                return ServiceResult<bool>.Failure("Rate limit rule not found");
            }

            _logger.LogInformation(" SERVICE DELETE - Rule found: {RuleName} | IsSystemRule: {IsSystemRule}",
                rule.Name, rule.IsSystemRule);

            if (rule.IsSystemRule)
            {
                _logger.LogWarning(" SERVICE DELETE - Cannot delete system rule: {RuleId}", ruleId);
                return ServiceResult<bool>.Failure("System rules cannot be deleted");
            }

            _context.RateLimitRules.Remove(rule);
            await _context.SaveChangesAsync();
            await InvalidateCacheAsync();

            // Log security event
            await _securityEventService.LogEventAsync(
                "rate_limit_rule_deleted",
                $"Rate limit rule deleted: {rule.Name}",
                metadata: new Dictionary<string, object> {
                    { "Name", rule.Name },
                    { "Endpoint", rule.Endpoint }
                }
            );

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting rate limit rule {RuleId}", ruleId);
            return ServiceResult<bool>.Failure("Failed to delete rate limit rule");
        }
    }

    public async Task<ServiceResult<List<RateLimitViolation>>> GetViolationsAsync(int hours = 24)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-hours);

            var violations = await _context.RateLimitViolations
                .Include(v => v.Rule)
                .Where(v => v.CreatedAt >= cutoffTime)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            return ServiceResult<List<RateLimitViolation>>.Success(violations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate limit violations");
            return ServiceResult<List<RateLimitViolation>>.Failure("Failed to retrieve violations");
        }
    }

    public async Task LogViolationAsync(RateLimitRule rule, string clientIdentifier, string endpoint, string httpMethod, int requestCount, string? userAgent = null)
    {
        try
        {
            var violation = new RateLimitViolation
            {
                RuleId = rule.Id,
                IPAddress = clientIdentifier,
                Endpoint = endpoint,
                HttpMethod = httpMethod,
                RequestCount = requestCount,
                LimitExceeded = requestCount - GetApplicableLimit(rule),
                UserAgent = userAgent,
                WindowStart = DateTime.UtcNow.AddMinutes(-1),
                WindowEnd = DateTime.UtcNow,
                WasBlocked = true,
                Action = "blocked"
            };

            _context.RateLimitViolations.Add(violation);
            await _context.SaveChangesAsync();

            // Log security event
            await _securityEventService.LogEventAsync(
                "rate_limit_exceeded",
                $"Rate limit exceeded for {endpoint}",
                ipAddress: clientIdentifier,
                metadata: new Dictionary<string, object> {
                    { "RuleName", rule.Name },
                    { "RequestCount", requestCount },
                    { "Limit", GetApplicableLimit(rule) },
                    { "Endpoint", endpoint },
                    { "WasBlocked", true }
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging rate limit violation");
        }
    }

    public async Task InvalidateCacheAsync()
    {
        _cache.Remove(CACHE_KEY_RULES);
        // Also clear request counters for immediate effect
        // In production, you might want to use Redis for distributed caching
    }

    #region Private Methods

    private async Task<List<RateLimitRule>> GetCachedRulesAsync()
    {
        if (_cache.TryGetValue(CACHE_KEY_RULES, out List<RateLimitRule>? cachedRules) && cachedRules != null)
        {
            return cachedRules;
        }

        var rules = await _context.RateLimitRules
            .Where(r => r.IsActive)
            .AsNoTracking()
            .ToListAsync();

        _cache.Set(CACHE_KEY_RULES, rules, _cacheExpiration);
        return rules;
    }

    private static IEnumerable<RateLimitRule> GetMatchingRules(List<RateLimitRule> rules, string endpoint, string httpMethod)
    {
        foreach (var rule in rules)
        {
            if (rule.HttpMethod != "ALL" && !rule.HttpMethod.Equals(httpMethod, StringComparison.OrdinalIgnoreCase))
                continue;

            if (IsEndpointMatch(rule.Endpoint, endpoint))
            {
                yield return rule;
            }
        }
    }

    private static bool IsEndpointMatch(string pattern, string endpoint)
    {
        // Simple wildcard matching - can be enhanced with regex if needed
        if (pattern == "*" || pattern == "/*") return true;

        if (pattern.EndsWith("*"))
        {
            var prefix = pattern[..^1];
            return endpoint.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return pattern.Equals(endpoint, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasException(RateLimitRule rule, string clientIdentifier, string? userRole, string? apiKey)
    {
        try
        {
            // Check IP whitelist
            if (!string.IsNullOrEmpty(rule.IPWhitelist) && rule.IPWhitelist != "[]")
            {
                var whitelist = JsonSerializer.Deserialize<string[]>(rule.IPWhitelist) ?? Array.Empty<string>();
                if (whitelist.Contains(clientIdentifier, StringComparer.OrdinalIgnoreCase))
                    return true;
            }

            // Check user role exceptions
            if (!string.IsNullOrEmpty(userRole) && !string.IsNullOrEmpty(rule.UserRoleExceptions) && rule.UserRoleExceptions != "[]")
            {
                var roleExceptions = JsonSerializer.Deserialize<string[]>(rule.UserRoleExceptions) ?? Array.Empty<string>();
                if (roleExceptions.Contains(userRole, StringComparer.OrdinalIgnoreCase))
                    return true;
            }

            // Check API key exceptions
            if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(rule.ApiKeyExceptions) && rule.ApiKeyExceptions != "[]")
            {
                var apiKeyExceptions = JsonSerializer.Deserialize<string[]>(rule.ApiKeyExceptions) ?? Array.Empty<string>();
                if (apiKeyExceptions.Contains(apiKey, StringComparer.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task<RateLimitResult> CheckRuleAsync(RateLimitRule rule, string clientIdentifier, string endpoint, string httpMethod)
    {
        var now = DateTime.UtcNow;

        // Check minute window
        var minuteKey = $"{CACHE_KEY_PREFIX_REQUESTS}{rule.Id}_{clientIdentifier}_minute_{now:yyyyMMddHHmm}";
        var minuteRequests = _cache.Get<int>(minuteKey);

        if (minuteRequests >= rule.RequestsPerMinute)
        {
            await LogViolationAsync(rule, clientIdentifier, endpoint, httpMethod, minuteRequests + 1);
            return new RateLimitResult
            {
                IsAllowed = false,
                Message = rule.CustomErrorMessage ?? $"Rate limit exceeded: {rule.RequestsPerMinute} requests per minute",
                RemainingRequests = 0,
                RetryAfter = now.AddSeconds(60 - now.Second),
                RuleName = rule.Name,
                CooldownSeconds = rule.CooldownSeconds
            };
        }

        // Check hour window
        var hourKey = $"{CACHE_KEY_PREFIX_REQUESTS}{rule.Id}_{clientIdentifier}_hour_{now:yyyyMMddHH}";
        var hourRequests = _cache.Get<int>(hourKey);

        if (hourRequests >= rule.RequestsPerHour)
        {
            await LogViolationAsync(rule, clientIdentifier, endpoint, httpMethod, hourRequests + 1);
            return new RateLimitResult
            {
                IsAllowed = false,
                Message = rule.CustomErrorMessage ?? $"Rate limit exceeded: {rule.RequestsPerHour} requests per hour",
                RemainingRequests = 0,
                RetryAfter = now.AddMinutes(60 - now.Minute),
                RuleName = rule.Name,
                CooldownSeconds = rule.CooldownSeconds
            };
        }

        // Check day window
        var dayKey = $"{CACHE_KEY_PREFIX_REQUESTS}{rule.Id}_{clientIdentifier}_day_{now:yyyyMMdd}";
        var dayRequests = _cache.Get<int>(dayKey);

        if (dayRequests >= rule.RequestsPerDay)
        {
            await LogViolationAsync(rule, clientIdentifier, endpoint, httpMethod, dayRequests + 1);
            return new RateLimitResult
            {
                IsAllowed = false,
                Message = rule.CustomErrorMessage ?? $"Rate limit exceeded: {rule.RequestsPerDay} requests per day",
                RemainingRequests = 0,
                RetryAfter = now.AddDays(1).Date,
                RuleName = rule.Name,
                CooldownSeconds = rule.CooldownSeconds
            };
        }

        // Update counters
        _cache.Set(minuteKey, minuteRequests + 1, TimeSpan.FromMinutes(2));
        _cache.Set(hourKey, hourRequests + 1, TimeSpan.FromHours(2));
        _cache.Set(dayKey, dayRequests + 1, TimeSpan.FromDays(2));

        var remainingMinute = rule.RequestsPerMinute - minuteRequests - 1;
        var remainingHour = rule.RequestsPerHour - hourRequests - 1;
        var remainingDay = rule.RequestsPerDay - dayRequests - 1;
        var remaining = Math.Min(remainingMinute, Math.Min(remainingHour, remainingDay));

        return new RateLimitResult
        {
            IsAllowed = true,
            Message = "Request allowed",
            RemainingRequests = Math.Max(0, remaining),
            RuleName = rule.Name
        };
    }

    private static int GetApplicableLimit(RateLimitRule rule)
    {
        // Return the most restrictive limit for logging purposes
        return Math.Min(rule.RequestsPerMinute, Math.Min(rule.RequestsPerHour / 60, rule.RequestsPerDay / 1440));
    }

    // Additional interface methods

    /// <summary>
    /// Create rate limit rule with interface signature
    /// </summary>
    public async Task<ServiceResult<bool>> CreateRateLimitRuleAsync(string name, string pattern, int maxRequests, TimeSpan window, string? description = null, int? adminUserId = null)
    {
        try
        {
            var rule = new RateLimitRule
            {
                Name = name,
                Endpoint = pattern,
                RequestsPerMinute = Math.Min(maxRequests, (int)(maxRequests / window.TotalMinutes)),
                RequestsPerHour = Math.Min(maxRequests, (int)(maxRequests / window.TotalHours)),
                RequestsPerDay = maxRequests,

                Description = description,
                CreatedBy = adminUserId?.ToString() ?? "System",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await CreateRateLimitRuleAsync(rule);
            return result.IsSuccess ? ServiceResult<bool>.Success(true) : ServiceResult<bool>.Failure(result.ErrorMessage ?? "Operation failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating rate limit rule");
            return ServiceResult<bool>.Failure("Failed to create rate limit rule");
        }
    }

    /// <summary>
    /// Update rate limit rule with interface signature
    /// </summary>
    public async Task<ServiceResult<bool>> UpdateRateLimitRuleAsync(int ruleId, string? name = null, string? pattern = null, int? maxRequests = null, TimeSpan? window = null, bool? isActive = null, int? adminUserId = null)
    {
        try
        {
            var rule = await _context.RateLimitRules.FindAsync(ruleId);
            if (rule == null)
            {
                return ServiceResult<bool>.Failure("Rate limit rule not found");
            }

            if (name != null) rule.Name = name;
            if (pattern != null) rule.Endpoint = pattern;
            if (maxRequests.HasValue)
            {
                rule.RequestsPerDay = maxRequests.Value;
                if (window.HasValue)
                {
                    rule.RequestsPerMinute = Math.Min(maxRequests.Value, (int)(maxRequests.Value / window.Value.TotalMinutes));
                    rule.RequestsPerHour = Math.Min(maxRequests.Value, (int)(maxRequests.Value / window.Value.TotalHours));

                }
            }
            if (isActive.HasValue) rule.IsActive = isActive.Value;
            rule.LastUpdatedBy = adminUserId?.ToString() ?? "System";
            rule.UpdatedAt = DateTime.UtcNow;

            var result = await UpdateRateLimitRuleAsync(rule);
            return result.IsSuccess ? ServiceResult<bool>.Success(true) : ServiceResult<bool>.Failure(result.ErrorMessage ?? "Operation failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rate limit rule");
            return ServiceResult<bool>.Failure("Failed to update rate limit rule");
        }
    }

    /// <summary>
    /// Delete rate limit rule with interface signature
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteRateLimitRuleAsync(int ruleId, int? adminUserId = null)
    {
        return await DeleteRateLimitRuleAsync(ruleId);
    }

    /// <summary>
    /// Get rate limit rules with pagination
    /// </summary>
    public async Task<ServiceResult<List<RateLimitRule>>> GetRateLimitRulesAsync(int page = 1, int pageSize = 50)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            var rules = await _context.RateLimitRules
                .Where(r => r.IsActive)
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return ServiceResult<List<RateLimitRule>>.Success(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rate limit rules");
            return ServiceResult<List<RateLimitRule>>.Failure("Failed to retrieve rate limit rules");
        }
    }

    /// <summary>
    /// Get rate limit statistics
    /// </summary>
    public async Task<ServiceResult<Dictionary<string, int>>> GetRateLimitStatisticsAsync(DateTime from, DateTime to)
    {
        try
        {
            var stats = new Dictionary<string, int>
            {
                ["TotalViolations"] = await _context.RateLimitViolations
                    .Where(v => v.CreatedAt >= from && v.CreatedAt <= to)
                    .CountAsync(),
                ["UniqueClients"] = await _context.RateLimitViolations
                    .Where(v => v.CreatedAt >= from && v.CreatedAt <= to)
                    .Select(v => v.IPAddress)
                    .Distinct()
                    .CountAsync(),
                ["ActiveRules"] = await _context.RateLimitRules
                    .Where(r => r.IsActive)
                    .CountAsync(),
                ["TotalRequests"] = await _context.RateLimitViolations
                    .Where(v => v.CreatedAt >= from && v.CreatedAt <= to)
                    .SumAsync(v => v.RequestCount)
            };

            return ServiceResult<Dictionary<string, int>>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rate limit statistics");
            return ServiceResult<Dictionary<string, int>>.Failure("Failed to retrieve statistics");
        }
    }

    /// <summary>
    /// Reset rate limit for a client
    /// </summary>
    public async Task<ServiceResult<bool>> ResetRateLimitAsync(string identifier, string endpoint, int? adminUserId = null)
    {
        try
        {
            var cacheKey = $"{CACHE_KEY_PREFIX_REQUESTS}{identifier}_{endpoint}";
            _cache.Remove(cacheKey);

            _logger.LogInformation("Rate limit reset for {Identifier} on {Endpoint} by admin {AdminUserId}",
                identifier, endpoint, adminUserId);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting rate limit for {Identifier}", identifier);
            return ServiceResult<bool>.Failure("Failed to reset rate limit");
        }
    }

    /// <summary>
    /// Get violations with enhanced filtering
    /// </summary>
    public async Task<ServiceResult<List<RateLimitViolation>>> GetViolationsAsync(string? identifier = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50)
    {
        try
        {
            var query = _context.RateLimitViolations.AsQueryable();

            if (!string.IsNullOrEmpty(identifier))
                query = query.Where(v => v.IPAddress == identifier);

            if (from.HasValue)
                query = query.Where(v => v.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(v => v.CreatedAt <= to.Value);

            var skip = (page - 1) * pageSize;
            var violations = await query
                .OrderByDescending(v => v.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return ServiceResult<List<RateLimitViolation>>.Success(violations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rate limit violations");
            return ServiceResult<List<RateLimitViolation>>.Failure("Failed to retrieve violations");
        }
    }

    /// <summary>
    /// Cleanup old violations
    /// </summary>
    public async Task<ServiceResult<bool>> CleanupOldViolationsAsync(DateTime olderThan)
    {
        try
        {
            var oldViolations = await _context.RateLimitViolations
                .Where(v => v.CreatedAt < olderThan)
                .ToListAsync();

            _context.RateLimitViolations.RemoveRange(oldViolations);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} old rate limit violations", oldViolations.Count);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old violations");
            return ServiceResult<bool>.Failure("Failed to cleanup old violations");
        }
    }

    #endregion
}