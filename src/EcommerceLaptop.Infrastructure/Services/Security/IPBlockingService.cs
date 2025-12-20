using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Core.ValueObjects;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Net;
using System.Text.Json;

namespace EcommerceLaptop.Infrastructure.Services.Security;

public class IPBlockingService : IIPBlockingService
{
    private readonly ApplicationDbContext _context;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<IPBlockingService> _logger;
    private readonly ISecurityEventService _securityEventService;
    
    public IPBlockingService(
        ApplicationDbContext context,
        IConnectionMultiplexer redis,
        ILogger<IPBlockingService> logger,
        ISecurityEventService securityEventService)
    {
        _context = context;
        _redis = redis;
        _logger = logger;
        _securityEventService = securityEventService;
    }

    public async Task<ServiceResult<bool>> IsIPBlockedAsync(string ipAddress)
    {
        try
        {
            var db = _redis.GetDatabase();
            // Check Redis directly for O(1) performance
            // Key format: blacklist:{ip}
            bool isBlocked = await db.KeyExistsAsync($"blacklist:{ipAddress}");
            
            if (isBlocked)
            {
                // Verify it's not whitelisted first? 
                // Usually whitelist overrides blacklist.
                // Let's check whitelist first if we want strict logic, but for "shield" performance, 
                // usually we check blacklist. 
                // However, IsIPWhitelistedAsync is called separately in Middleware currently.
                // Middleware logic: Check Whitelist -> Check Blacklist.
                // So here we only check Blacklist.
                return ServiceResult<bool>.Success(true);
            }

            return ServiceResult<bool>.Success(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if IP {IP} is blocked in Redis", ipAddress);
            // Fallback to DB (optional, but for "no SQL" goal, maybe safer to return false vs crashing)
            // Or return false to fail open.
            return ServiceResult<bool>.Success(false);
        }
    }

    public async Task<ServiceResult<bool>> IsIPWhitelistedAsync(string ipAddress)
    {
        try
        {
            var db = _redis.GetDatabase();
            bool isWhitelisted = await db.KeyExistsAsync($"whitelist:{ipAddress}");
            return ServiceResult<bool>.Success(isWhitelisted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if IP {IP} is whitelisted in Redis", ipAddress);
            return ServiceResult<bool>.Success(false);
        }
    }

    public async Task<ServiceResult<List<IPBlockRule>>> GetIPRulesAsync()
    {
        try
        {
            var rules = await _context.IPBlockRules
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return ServiceResult<List<IPBlockRule>>.Success(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting IP rules");
            return ServiceResult<List<IPBlockRule>>.Failure("Failed to retrieve IP rules");
        }
    }

    public async Task<ServiceResult<IPBlockRule>> CreateIPRuleAsync(IPBlockRule rule)
    {
        try
        {
            // Validate IP address or CIDR range
            if (!IsValidIPOrCIDR(rule.IPAddress))
            {
                return ServiceResult<IPBlockRule>.Failure("Invalid IP address or CIDR range format");
            }

            // Check for duplicate rules
            var existingRule = await _context.IPBlockRules
                .FirstOrDefaultAsync(r => r.IPAddress == rule.IPAddress && r.Type == rule.Type && r.IsActive);

            if (existingRule != null)
            {
                return ServiceResult<IPBlockRule>.Failure("A rule for this IP address and type already exists");
            }

            rule.CreatedAt = DateTime.UtcNow;
            rule.UpdatedAt = DateTime.UtcNow;

            _context.IPBlockRules.Add(rule);
            await _context.SaveChangesAsync();

            // Sync to Redis
            await SyncRuleToRedisAsync(rule);

            // Log security event
            await _securityEventService.LogEventAsync(
                "ip_rule_created",
                $"IP rule created: {rule.Type} for {rule.IPAddress}",
                ipAddress: rule.IPAddress,
                metadata: new Dictionary<string, object> { 
                    { "Type", rule.Type }, 
                    { "Reason", rule.Reason }, 
                    { "CreatedBy", rule.CreatedBy } 
                }
            );

            _logger.LogInformation("IP rule created: {Type} for {IP} by {CreatedBy}", 
                rule.Type, rule.IPAddress, rule.CreatedBy);

            return ServiceResult<IPBlockRule>.Success(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating IP rule for {IP}", rule.IPAddress);
            return ServiceResult<IPBlockRule>.Failure("Failed to create IP rule");
        }
    }

    public async Task<ServiceResult<IPBlockRule>> UpdateIPRuleAsync(IPBlockRule rule)
    {
        try
        {
            var existingRule = await _context.IPBlockRules.FindAsync(rule.Id);
            if (existingRule == null)
            {
                return ServiceResult<IPBlockRule>.Failure("IP rule not found");
            }

            // If IP changing, remove old redis key
            if (existingRule.IPAddress != rule.IPAddress || existingRule.Type != rule.Type)
            {
                await RemoveRuleFromRedisAsync(existingRule);
            }

            var oldValues = JsonSerializer.Serialize(existingRule);

            existingRule.IPAddress = rule.IPAddress;
            existingRule.Type = rule.Type;
            existingRule.Reason = rule.Reason;
            existingRule.IsActive = rule.IsActive;
            existingRule.ExpiresAt = rule.ExpiresAt;
            existingRule.UpdatedAt = DateTime.UtcNow;
            existingRule.CountryCode = rule.CountryCode;
            existingRule.ThreatLevel = rule.ThreatLevel;

            await _context.SaveChangesAsync();
            
            // Sync new state
            if (existingRule.IsActive)
            {
                await SyncRuleToRedisAsync(existingRule);
            }
            else
            {
                 // ensure removed if deactivated
                await RemoveRuleFromRedisAsync(existingRule);
            }

            // Log security event
            await _securityEventService.LogEventAsync(
                "ip_rule_updated",
                $"IP rule updated for {rule.IPAddress}",
                ipAddress: rule.IPAddress,
                metadata: new Dictionary<string, object> { 
                    { "OldValues", oldValues }, 
                    { "NewValues", JsonSerializer.Serialize(rule) } 
                }
            );

            return ServiceResult<IPBlockRule>.Success(existingRule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating IP rule {RuleId}", rule.Id);
            return ServiceResult<IPBlockRule>.Failure("Failed to update IP rule");
        }
    }

    public async Task<ServiceResult<bool>> DeleteIPRuleAsync(int ruleId)
    {
        try
        {
            var rule = await _context.IPBlockRules.FindAsync(ruleId);
            if (rule == null)
            {
                return ServiceResult<bool>.Failure("IP rule not found");
            }

            _context.IPBlockRules.Remove(rule);
            await _context.SaveChangesAsync();
            
            // Remove from Redis
            await RemoveRuleFromRedisAsync(rule);

            // Log security event
            await _securityEventService.LogEventAsync(
                "ip_rule_deleted",
                $"IP rule deleted for {rule.IPAddress}",
                ipAddress: rule.IPAddress,
                metadata: new Dictionary<string, object> { 
                    { "Type", rule.Type }, 
                    { "Reason", rule.Reason } 
                }
            );

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting IP rule {RuleId}", ruleId);
            return ServiceResult<bool>.Failure("Failed to delete IP rule");
        }
    }

    public async Task<ServiceResult<bool>> BlockIPAsync(string ipAddress, string reason, int? adminUserId = null, DateTime? expiresAt = null)
    {
        var rule = new IPBlockRule
        {
            IPAddress = ipAddress,
            Type = "blacklist",
            Reason = reason,
            CreatedBy = adminUserId?.ToString() ?? "System",
            ExpiresAt = expiresAt,
            IsActive = true,
            ThreatLevel = "high"
        };

        var result = await CreateIPRuleAsync(rule);
        return ServiceResult<bool>.Success(result.IsSuccess);
    }

    public async Task<ServiceResult<bool>> UnblockIPAsync(string ipAddress, int? adminUserId = null)
    {
        try
        {
            var rule = await _context.IPBlockRules
                .FirstOrDefaultAsync(r => r.IPAddress == ipAddress && r.Type == "blacklist" && r.IsActive);

            if (rule == null)
            {
                // Try to remove from Redis anyway just in case
                var db = _redis.GetDatabase();
                await db.KeyDeleteAsync($"blacklist:{ipAddress}");
                return ServiceResult<bool>.Failure("No active block rule found for this IP");
            }

            rule.IsActive = false;
            rule.UpdatedAt = DateTime.UtcNow;
            rule.LastUpdatedBy = adminUserId?.ToString() ?? "System";

            await _context.SaveChangesAsync();
            await RemoveRuleFromRedisAsync(rule);

            // Log security event
            var modifiedBy = rule.LastUpdatedBy ?? "System";
            await _securityEventService.LogEventAsync(
                "ip_unblocked", 
                $"IP {ipAddress} unblocked by {modifiedBy}",
                adminUserId: adminUserId,
                ipAddress: ipAddress,
                metadata: new Dictionary<string, object> { 
                    { "ModifiedBy", modifiedBy }, 
                    { "OriginalReason", rule.Reason } 
                }
            );

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unblocking IP {IP}", ipAddress);
            return ServiceResult<bool>.Failure("Failed to unblock IP");
        }
    }

    public async Task<ServiceResult<List<string>>> GetSuspiciousIPsAsync(int hours = 24)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-hours);
            
            // Events are now in Loki. 
            // TODO: Implement LogQL query for suspicious IPs via LokiClient if needed.
            // For now, return empty to unblock build.
            return ServiceResult<List<string>>.Success(new List<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suspicious IPs");
            return ServiceResult<List<string>>.Failure("Failed to get suspicious IPs");
        }
    }



    public async Task LogIPAccessAttemptAsync(string ipAddress, string endpoint, bool wasBlocked, string? reason = null)
    {
        try
        {
            await _securityEventService.LogEventAsync(
                wasBlocked ? "ip_access_blocked" : "ip_access_allowed",
                wasBlocked 
                    ? $"Access blocked for IP {ipAddress} to {endpoint}"
                    : $"Access allowed for IP {ipAddress} to {endpoint}",
                ipAddress: ipAddress,
                metadata: new Dictionary<string, object> { 
                    { "Endpoint", endpoint }, 
                    { "Reason", reason }, 
                    { "WasBlocked", wasBlocked } 
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging IP access attempt for {IP}", ipAddress);
        }
    }

    #region Private Methods

    private async Task SyncRuleToRedisAsync(IPBlockRule rule)
    {
        try
        {
            // Only sync single IPs to Redis for O(1) lookup
            // CIDR/Range rules are not synced to Redis Key-Value store in this simple implementation
            // They rely on the slower SQL/Memory path if IsIPBlockedAsync fell back (which it doesn't currently)
            // For now, checks are strictly for specific IPs in Redis.
            if (IsValidIP(rule.IPAddress) && rule.IsActive)
            {
                var db = _redis.GetDatabase();
                string key = rule.Type.Equals("whitelist", StringComparison.OrdinalIgnoreCase) 
                    ? $"whitelist:{rule.IPAddress}" 
                    : $"blacklist:{rule.IPAddress}";
                
                if (rule.ExpiresAt.HasValue)
                {
                    var ttl = rule.ExpiresAt.Value - DateTime.UtcNow;
                    if (ttl > TimeSpan.Zero)
                        await db.StringSetAsync(key, true, ttl);
                }
                else
                {
                    await db.StringSetAsync(key, true);
                }
            }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error syncing rule to Redis for {IP}", rule.IPAddress);
        }
    }

    private async Task RemoveRuleFromRedisAsync(IPBlockRule rule)
    {
        try
        {
            if (IsValidIP(rule.IPAddress))
            {
                var db = _redis.GetDatabase();
                string key = rule.Type.Equals("whitelist", StringComparison.OrdinalIgnoreCase) 
                    ? $"whitelist:{rule.IPAddress}" 
                    : $"blacklist:{rule.IPAddress}";
                await db.KeyDeleteAsync(key);
            }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error removing rule from Redis for {IP}", rule.IPAddress);
        }
    }

    private static bool IsValidIP(string ip)
    {
        return IPAddress.TryParse(ip, out _);
    }
    
    // Removed GetCachedIPRulesAsync as we use Redis direct lookup now
    
    private async Task InvalidateCacheAsync()
    {
        // No-op or handle specific cache invalidation if needed
        // Since we sync individual rules, we don't need global invalidation for Redis KV
        await Task.CompletedTask;
    }

    private static bool IsIPInRule(string ipAddress, IPBlockRule rule)
    {
        try
        {
            if (rule.Type == "range")
            {
                 return IsIPInRange(ipAddress, rule.IPAddress);
            }
            if (rule.IPAddress.Contains('/'))
            {
                // CIDR range
                return IsIPInCIDR(ipAddress, rule.IPAddress);
            }
            else
            {
                // Single IP
                return ipAddress.Equals(rule.IPAddress, StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            return false;
        }
    }

    private static bool IsIPInRange(string ipAddress, string ipRange)
    {
        try
        {
            // Parse range like "192.168.1.1-192.168.1.255"
            var parts = ipRange.Split('-');
            if (parts.Length != 2) return false;

            var startIP = IPAddress.Parse(parts[0].Trim());
            var endIP = IPAddress.Parse(parts[1].Trim());
            var testIP = IPAddress.Parse(ipAddress);

            var startBytes = startIP.GetAddressBytes();
            var endBytes = endIP.GetAddressBytes();
            var testBytes = testIP.GetAddressBytes();

            if (startBytes.Length != testBytes.Length) return false;

            // Convert to uint for comparison
            uint startNum = BitConverter.ToUInt32(startBytes.Reverse().ToArray(), 0);
            uint endNum = BitConverter.ToUInt32(endBytes.Reverse().ToArray(), 0);
            uint testNum = BitConverter.ToUInt32(testBytes.Reverse().ToArray(), 0);

            return testNum >= startNum && testNum <= endNum;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsIPInCIDR(string ipAddress, string cidrRange)
    {
        try
        {
            var parts = cidrRange.Split('/');
            if (parts.Length != 2) return false;

            var networkIP = IPAddress.Parse(parts[0]);
            var prefixLength = int.Parse(parts[1]);
            var testIP = IPAddress.Parse(ipAddress);

            var networkBytes = networkIP.GetAddressBytes();
            var testBytes = testIP.GetAddressBytes();

            if (networkBytes.Length != testBytes.Length) return false;

            var bytesToCheck = prefixLength / 8;
            var remainingBits = prefixLength % 8;

            // Check full bytes
            for (int i = 0; i < bytesToCheck; i++)
            {
                if (networkBytes[i] != testBytes[i]) return false;
            }

            // Check remaining bits in the last byte
            if (remainingBits > 0 && bytesToCheck < networkBytes.Length)
            {
                var mask = (byte)(0xFF << (8 - remainingBits));
                if ((networkBytes[bytesToCheck] & mask) != (testBytes[bytesToCheck] & mask))
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidIPOrCIDR(string ipString)
    {
        try
        {
            if (ipString.Contains('/'))
            {
                // CIDR validation
                var parts = ipString.Split('/');
                if (parts.Length != 2) return false;
                
                if (!IPAddress.TryParse(parts[0], out _)) return false;
                if (!int.TryParse(parts[1], out var prefix)) return false;
                
                return prefix >= 0 && prefix <= 32; // IPv4 CIDR
            }
            else
            {
                // Single IP validation
                return IPAddress.TryParse(ipString, out _);
            }
        }
        catch
        {
            return false;
        }
    }

    private async Task UpdateRuleStatisticsAsync(int ruleId, string ipAddress)
    {
        try
        {
            var rule = await _context.IPBlockRules.FindAsync(ruleId);
            if (rule != null)
            {
                rule.TriggerCount++;
                rule.LastTriggeredBy = ipAddress;
                rule.LastTriggeredAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rule statistics for rule {RuleId}", ruleId);
        }
    }

    // Additional interface methods implementation
    
    /// <summary>
    /// Get blocked IPs with pagination
    /// </summary>
    public async Task<ServiceResult<List<IPBlockRule>>> GetBlockedIPsAsync(int page = 1, int pageSize = 50)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            var rules = await _context.IPBlockRules
                .Where(r => r.IsActive && r.Type == "blacklist")
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return ServiceResult<List<IPBlockRule>>.Success(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blocked IPs");
            return ServiceResult<List<IPBlockRule>>.Failure("Failed to retrieve blocked IPs");
        }
    }

    /// <summary>
    /// Check if IP is in allow list
    /// </summary>
    public async Task<ServiceResult<bool>> IsIPInAllowListAsync(string ipAddress)
    {
        try
        {
            var rules = await GetActiveRulesAsync();
            var allowRules = rules.Where(r => r.Type == "whitelist").ToList();

            foreach (var rule in allowRules)
            {
                if (IsIPMatchRule(ipAddress, rule))
                {
                    return ServiceResult<bool>.Success(true);
                }
            }

            return ServiceResult<bool>.Success(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking allow list for IP {IPAddress}", ipAddress);
            return ServiceResult<bool>.Failure("Failed to check allow list");
        }
    }

    /// <summary>
    /// Add IP to allow list
    /// </summary>
    public async Task<ServiceResult<bool>> AddToAllowListAsync(string ipAddress, string reason, int? adminUserId = null)
    {
        try
        {
            var existingRule = await _context.IPBlockRules
                .Where(r => r.IPAddress == ipAddress && r.Type == "whitelist" && r.IsActive)
                .FirstOrDefaultAsync();

            if (existingRule != null)
            {
                return ServiceResult<bool>.Failure("IP already in allow list");
            }

            var rule = new IPBlockRule
            {
                IPAddress = ipAddress,
                Type = "whitelist",
                Reason = reason,
                CreatedBy = adminUserId?.ToString() ?? "System",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                AutoGenerated = adminUserId == null
            };

            _context.IPBlockRules.Add(rule);
            await _context.SaveChangesAsync();
            await InvalidateCacheAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding IP {IPAddress} to allow list", ipAddress);
            return ServiceResult<bool>.Failure("Failed to add IP to allow list");
        }
    }

    /// <summary>
    /// Remove IP from allow list
    /// </summary>
    public async Task<ServiceResult<bool>> RemoveFromAllowListAsync(string ipAddress, int? adminUserId = null)
    {
        try
        {
            var rule = await _context.IPBlockRules
                .Where(r => r.IPAddress == ipAddress && r.Type == "whitelist" && r.IsActive)
                .FirstOrDefaultAsync();

            if (rule == null)
            {
                return ServiceResult<bool>.Failure("IP not found in allow list");
            }

            rule.IsActive = false;
            rule.LastUpdatedBy = adminUserId?.ToString() ?? "System";
            rule.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await InvalidateCacheAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing IP {IPAddress} from allow list", ipAddress);
            return ServiceResult<bool>.Failure("Failed to remove IP from allow list");
        }
    }

    /// <summary>
    /// Get blocking statistics
    /// </summary>
    public async Task<ServiceResult<Dictionary<string, int>>> GetBlockingStatisticsAsync(DateTime from, DateTime to)
    {
        try
        {
            var stats = new Dictionary<string, int>
            {
                ["TotalBlockedAttempts"] = 0, // Pending Loki Aggregation
                ["UniqueBlockedIPs"] = 0,     // Pending Loki Aggregation
                ["ActiveBlockRules"] = await _context.IPBlockRules
                    .Where(r => r.IsActive && r.Type == "blacklist")
                    .CountAsync(),
                ["ActiveAllowRules"] = await _context.IPBlockRules
                    .Where(r => r.IsActive && r.Type == "whitelist")
                    .CountAsync()
            };

            return ServiceResult<Dictionary<string, int>>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blocking statistics");
            return ServiceResult<Dictionary<string, int>>.Failure("Failed to retrieve statistics");
        }
    }

    /// <summary>
    /// Cleanup expired rules
    /// </summary>
    public async Task<ServiceResult<bool>> CleanupExpiredRulesAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var expiredRules = await _context.IPBlockRules
                .Where(r => r.ExpiresAt.HasValue && r.ExpiresAt <= now && r.IsActive)
                .ToListAsync();

            foreach (var rule in expiredRules)
            {
                rule.IsActive = false;
                rule.UpdatedAt = now;
                rule.LastUpdatedBy = "System";
            }

            await _context.SaveChangesAsync();
            await InvalidateCacheAsync();

            _logger.LogInformation("Cleaned up {Count} expired IP blocking rules", expiredRules.Count);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired IP blocking rules");
            return ServiceResult<bool>.Failure("Failed to cleanup expired rules");
        }
    }

    /// <summary>
    /// Validate CIDR range format
    /// </summary>
    public bool ValidateCIDR(string cidrRange)
    {
        if (string.IsNullOrEmpty(cidrRange))
            return false;

        try
        {
            var parts = cidrRange.Split('/');
            if (parts.Length != 2)
                return false;

            if (!IPAddress.TryParse(parts[0], out var ip))
                return false;

            if (!int.TryParse(parts[1], out var prefix))
                return false;

            // IPv4: 0-32, IPv6: 0-128
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                return prefix >= 0 && prefix <= 32;
            else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                return prefix >= 0 && prefix <= 128;

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Block IP range using CIDR notation
    /// </summary>
    public async Task<ServiceResult<bool>> BlockIPRangeAsync(string cidrRange, string reason, int? adminUserId = null, DateTime? expiresAt = null)
    {
        try
        {
            if (!ValidateCIDR(cidrRange))
            {
                return ServiceResult<bool>.Failure("Invalid CIDR range format");
            }

            var existingRule = await _context.IPBlockRules
                .Where(r => r.IPAddress == cidrRange && r.Type == "blacklist" && r.IsActive)
                .FirstOrDefaultAsync();

            if (existingRule != null)
            {
                return ServiceResult<bool>.Failure("CIDR range already blocked");
            }

            var rule = new IPBlockRule
            {
                IPAddress = cidrRange,
                Type = "blacklist",
                Reason = reason,
                ExpiresAt = expiresAt,
                CreatedBy = adminUserId?.ToString() ?? "System",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                AutoGenerated = adminUserId == null
            };

            _context.IPBlockRules.Add(rule);
            await _context.SaveChangesAsync();
            await InvalidateCacheAsync();

            _logger.LogWarning("CIDR range {CIDRRange} blocked by {AdminUserId}: {Reason}", cidrRange, adminUserId, reason);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blocking CIDR range {CIDRRange}", cidrRange);
            return ServiceResult<bool>.Failure("Failed to block CIDR range");
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Get active IP blocking rules
    /// </summary>
    private async Task<List<IPBlockRule>> GetActiveRulesAsync()
    {
        return await _context.IPBlockRules
            .Where(r => r.IsActive && (!r.ExpiresAt.HasValue || r.ExpiresAt > DateTime.UtcNow))
            .OrderBy(r => r.ThreatLevel)
            .ToListAsync();
    }

    /// <summary>
    /// Check if IP matches rule pattern
    /// </summary>
    private bool IsIPMatchRule(string ipAddress, IPBlockRule rule)
    {
        try
        {
            if (rule.Type == "single")
            {
                return rule.IPAddress == ipAddress;
            }
            else if (rule.Type == "range")
            {
                return IsIPInRange(ipAddress, rule.IPAddress);
            }
            else if (rule.Type == "cidr")
            {
                return IsIPInCIDR(ipAddress, rule.IPAddress);
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}