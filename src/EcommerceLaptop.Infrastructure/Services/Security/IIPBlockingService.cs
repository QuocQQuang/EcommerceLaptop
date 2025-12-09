using EcommerceLaptop.Core.ValueObjects;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Infrastructure.Services.Security;

/// <summary>
/// Service for IP-based access control with CIDR support and caching
/// </summary>
public interface IIPBlockingService
{
    Task<ServiceResult<bool>> IsIPBlockedAsync(string ipAddress);
    Task<ServiceResult<bool>> BlockIPAsync(string ipAddress, string reason, int? adminUserId = null, DateTime? expiresAt = null);
    Task<ServiceResult<bool>> UnblockIPAsync(string ipAddress, int? adminUserId = null);
    Task<ServiceResult<List<IPBlockRule>>> GetBlockedIPsAsync(int page = 1, int pageSize = 50);
    Task<ServiceResult<bool>> IsIPInAllowListAsync(string ipAddress);
    Task<ServiceResult<bool>> AddToAllowListAsync(string ipAddress, string reason, int? adminUserId = null);
    Task<ServiceResult<bool>> RemoveFromAllowListAsync(string ipAddress, int? adminUserId = null);
    Task<ServiceResult<Dictionary<string, int>>> GetBlockingStatisticsAsync(DateTime from, DateTime to);
    Task<ServiceResult<bool>> CleanupExpiredRulesAsync();
    bool ValidateCIDR(string cidrRange);
    Task<ServiceResult<bool>> BlockIPRangeAsync(string cidrRange, string reason, int? adminUserId = null, DateTime? expiresAt = null);

    // Additional methods needed by middleware
    Task<ServiceResult<bool>> IsIPWhitelistedAsync(string ipAddress);
    Task LogIPAccessAttemptAsync(string ipAddress, string endpoint, bool wasBlocked, string? reason = null);

    // Required by SecurityController
    Task<ServiceResult<IPBlockRule>> CreateIPRuleAsync(IPBlockRule rule);
    Task<ServiceResult<IPBlockRule>> UpdateIPRuleAsync(IPBlockRule rule);
    Task<ServiceResult<bool>> DeleteIPRuleAsync(int ruleId);
    Task<ServiceResult<List<string>>> GetSuspiciousIPsAsync(int hours = 24);
}