using EcommerceLaptop.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;
using System.Data;

namespace EcommerceLaptop.API.Services;

/// <summary>
/// Feature Flag Service for Phase 3 authentication migration
/// </summary>
public interface IFeatureFlagService
{
    bool IsEnabled(string flagName);
    Task<bool> IsEnabledAsync(string flagName);
}

/// <summary>
/// Implementation of Feature Flag Service with simple hardcoded values for Phase 3
/// </summary>
public class FeatureFlagService : IFeatureFlagService
{
    private readonly IMemoryCache _cache;
    private readonly Dictionary<string, bool> _flags;

    public FeatureFlagService(IMemoryCache cache)
    {
        _cache = cache;

        // Phase 4: Updated feature flags - AdminAuthController removed completely
        _flags = new Dictionary<string, bool>
        {
            ["AuthService"] = true,
            ["AdminAuth"] = true,
            ["CustomerAuth"] = true,
            ["PermissionCheck"] = true,
            ["AuditLogs"] = true,
            ["DeprecateLegacyAuthController"] = true, // Phase 4: Enable customer auth deprecation
            ["EnableStandardEndpointsOnly"] = true // Phase 4: Enable standard-only mode
        };
    }

    public bool IsEnabled(string flagName)
    {
        return _flags.GetValueOrDefault(flagName, false);
    }

    public Task<bool> IsEnabledAsync(string flagName)
    {
        return Task.FromResult(IsEnabled(flagName));
    }
}
