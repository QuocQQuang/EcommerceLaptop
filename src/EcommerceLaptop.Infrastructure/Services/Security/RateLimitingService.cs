using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Core.ValueObjects;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services.Security;

public class RateLimitingService : IRateLimitingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RateLimitingService> _logger;
    private readonly ISecurityEventService _securityEventService;

    public RateLimitingService(
        ApplicationDbContext context,
        ILogger<RateLimitingService> logger,
        ISecurityEventService securityEventService)
    {
        _context = context;
        _logger = logger;
        _securityEventService = securityEventService;
    }

    public async Task<ServiceResult<List<RateLimitRule>>> GetRateLimitRulesAsync(int page = 1, int pageSize = 50)
    {
        try
        {
            var rules = await _context.RateLimitRules
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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

            // Log security event
            await _securityEventService.LogEventAsync(
                "rate_limit_rule_created",
                $"Rate limit rule created: {rule.Name}",
                metadata: new Dictionary<string, object> {
                    { "Name", rule.Name },
                    { "Endpoint", rule.Endpoint },
                    { "RequestsPerMinute", rule.RequestsPerMinute }
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

            // Log security event
            await _securityEventService.LogEventAsync(
                "rate_limit_rule_updated",
                $"Rate limit rule updated: {rule.Name}",
                metadata: new Dictionary<string, object> {
                    { "Name", rule.Name }
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
            var rule = await _context.RateLimitRules.FindAsync(ruleId);
            if (rule == null)
            {
                return ServiceResult<bool>.Failure("Rate limit rule not found");
            }

            if (rule.IsSystemRule)
            {
                return ServiceResult<bool>.Failure("System rules cannot be deleted");
            }

            _context.RateLimitRules.Remove(rule);
            await _context.SaveChangesAsync();

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

    // Additional interface methods

    public async Task<ServiceResult<bool>> CreateRateLimitRuleAsync(string name, string pattern, int maxRequests, TimeSpan window, string? description = null, int? adminUserId = null)
    {
        var rule = new RateLimitRule
        {
            Name = name,
            Endpoint = pattern,
            RequestsPerMinute = Math.Min(maxRequests, (int)(maxRequests / Math.Max(1, window.TotalMinutes))),
            RequestsPerHour = Math.Min(maxRequests, (int)(maxRequests / Math.Max(1, window.TotalHours))),
            RequestsPerDay = maxRequests,
            Description = description,
            CreatedBy = adminUserId?.ToString() ?? "System",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await CreateRateLimitRuleAsync(rule);
        return result.IsSuccess ? ServiceResult<bool>.Success(true) : ServiceResult<bool>.Failure(result.ErrorMessage ?? "Operation failed");
    }

    public async Task<ServiceResult<bool>> UpdateRateLimitRuleAsync(int ruleId, string? name = null, string? pattern = null, int? maxRequests = null, TimeSpan? window = null, bool? isActive = null, int? adminUserId = null)
    {
        var rule = await _context.RateLimitRules.FindAsync(ruleId);
        if (rule == null) return ServiceResult<bool>.Failure("Rate limit rule not found");

        if (name != null) rule.Name = name;
        if (pattern != null) rule.Endpoint = pattern;
        if (maxRequests.HasValue)
        {
            rule.RequestsPerDay = maxRequests.Value;
            if (window.HasValue)
            {
                rule.RequestsPerMinute = Math.Min(maxRequests.Value, (int)(maxRequests.Value / Math.Max(1, window.Value.TotalMinutes)));
                rule.RequestsPerHour = Math.Min(maxRequests.Value, (int)(maxRequests.Value / Math.Max(1, window.Value.TotalHours)));
            }
        }
        if (isActive.HasValue) rule.IsActive = isActive.Value;
        rule.LastUpdatedBy = adminUserId?.ToString() ?? "System";
        rule.UpdatedAt = DateTime.UtcNow;

        var result = await UpdateRateLimitRuleAsync(rule);
        return result.IsSuccess ? ServiceResult<bool>.Success(true) : ServiceResult<bool>.Failure(result.ErrorMessage ?? "Operation failed");
    }

    public async Task<ServiceResult<bool>> DeleteRateLimitRuleAsync(int ruleId, int? adminUserId = null)
    {
        return await DeleteRateLimitRuleAsync(ruleId);
    }
}