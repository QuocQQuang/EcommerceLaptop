using EcommerceLaptop.Core.ValueObjects;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Infrastructure.Services.Security;

/// <summary>
/// Service for advanced rate limiting with multi-window support
/// </summary>
public interface IRateLimitingService
{
    Task<ServiceResult<RateLimitResult>> CheckRateLimitAsync(string clientIdentifier, string endpoint, string httpMethod, string? userRole = null, string? apiKey = null);
    Task<ServiceResult<bool>> CreateRateLimitRuleAsync(string name, string pattern, int maxRequests, TimeSpan window, string? description = null, int? adminUserId = null);
    Task<ServiceResult<bool>> UpdateRateLimitRuleAsync(int ruleId, string? name = null, string? pattern = null, int? maxRequests = null, TimeSpan? window = null, bool? isActive = null, int? adminUserId = null);
    Task<ServiceResult<bool>> DeleteRateLimitRuleAsync(int ruleId, int? adminUserId = null);
    Task<ServiceResult<List<RateLimitRule>>> GetRateLimitRulesAsync(int page = 1, int pageSize = 50);
    Task<ServiceResult<Dictionary<string, int>>> GetRateLimitStatisticsAsync(DateTime from, DateTime to);
    Task<ServiceResult<bool>> ResetRateLimitAsync(string identifier, string endpoint, int? adminUserId = null);
    Task<ServiceResult<List<RateLimitViolation>>> GetViolationsAsync(string? identifier = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50);
    Task<ServiceResult<bool>> CleanupOldViolationsAsync(DateTime olderThan);

    // Entity-based create/update for full-field persistence
    Task<ServiceResult<RateLimitRule>> CreateRateLimitRuleAsync(RateLimitRule rule);
    Task<ServiceResult<RateLimitRule>> UpdateRateLimitRuleAsync(RateLimitRule rule);
}