using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Core.ValueObjects;
using EcommerceLaptop.Core.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceLaptop.Infrastructure.Services;

public class SystemSettingsService : ISystemSettingsService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SystemSettingsService> _logger;
    private readonly IMemoryCache _cache;

    public SystemSettingsService(IServiceScopeFactory scopeFactory, ILogger<SystemSettingsService> logger, IMemoryCache cache)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _cache = cache;
    }

    public async Task<ServiceResult<Dictionary<string, List<SystemSetting>>>> GetAllSettingsGroupedAsync()
    {
        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var settings = await context.SystemSettings.AsNoTracking().ToListAsync();
                var grouped = settings.GroupBy(s => s.Category).ToDictionary(g => g.Key, g => g.ToList());
                return ServiceResult<Dictionary<string, List<SystemSetting>>>.Success(grouped);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all settings grouped");
            return ServiceResult<Dictionary<string, List<SystemSetting>>>.Failure("Failed to retrieve system settings");
        }
    }

    public async Task<ServiceResult<List<SystemSetting>>> GetSettingsByCategoryAsync(string category)
    {
        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var settings = await context.SystemSettings.AsNoTracking().Where(s => s.Category == category).ToListAsync();
                return ServiceResult<List<SystemSetting>>.Success(settings);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settings for category {Category}", category);
            return ServiceResult<List<SystemSetting>>.Failure($"Failed to retrieve settings for category: {category}");
        }
    }

    public async Task<ServiceResult<SystemSetting?>> GetSettingByKeyAsync(string key)
    {
        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var setting = await context.SystemSettings.AsNoTracking().FirstOrDefaultAsync(s => s.SettingKey == key);
                return ServiceResult<SystemSetting?>.Success(setting);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting setting by key {Key}", key);
            return ServiceResult<SystemSetting?>.Failure($"Failed to retrieve setting: {key}");
        }
    }

    public async Task<T?> GetSettingValueAsync<T>(string key, T? defaultValue = default)
    {
        try
        {
            var result = await GetSettingByKeyAsync(key);
            if (!result.IsSuccess || result.Data == null) return defaultValue;

            var value = result.Data.SettingValue;
            if (string.IsNullOrEmpty(value)) return defaultValue;

            if (typeof(T) == typeof(string)) return (T)(object)value;
            if (typeof(T) == typeof(bool) && bool.TryParse(value, out bool boolVal)) return (T)(object)boolVal;
            if (typeof(T) == typeof(int) && int.TryParse(value, out int intVal)) return (T)(object)intVal;

            return defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    public async Task<ServiceResult<SystemSetting>> UpsertSettingAsync(string key, string value, string category = "General", string? description = null)
    {
        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var existing = await context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
                if (existing != null)
                {
                    existing.SettingValue = value;
                    existing.Category = category;
                    existing.Description = description;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    existing = new SystemSetting
                    {
                        SettingKey = key,
                        SettingValue = value,
                        Category = category,
                        Description = description,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    context.SystemSettings.Add(existing);
                }
                await context.SaveChangesAsync();
                _cache.Remove($"setting_{key}");
                return ServiceResult<SystemSetting>.Success(existing);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting setting {Key}", key);
            return ServiceResult<SystemSetting>.Failure($"Failed to upsert setting: {key}");
        }
    }

    public async Task<ServiceResult<bool>> UpdateSettingsAsync(List<SystemSettingUpdateRequest> settings)
    {
        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                foreach (var settingDto in settings)
                {
                    var setting = await context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == settingDto.Key);
                    if (setting != null)
                    {
                        setting.SettingValue = settingDto.Value;
                        setting.UpdatedAt = DateTime.UtcNow;
                        _cache.Remove($"setting_{setting.SettingKey}");
                    }
                }
                await context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings");
            return ServiceResult<bool>.Failure("Failed to update settings");
        }
    }

    public async Task<ServiceResult<bool>> DeleteSettingAsync(string key)
    {
        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var setting = await context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
                if (setting != null)
                {
                    context.SystemSettings.Remove(setting);
                    await context.SaveChangesAsync();
                    _cache.Remove($"setting_{key}");
                    return ServiceResult<bool>.Success(true);
                }
                return ServiceResult<bool>.Failure("Setting not found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting setting {Key}", key);
            return ServiceResult<bool>.Failure($"Failed to delete setting: {key}");
        }
    }

    public async Task<ServiceResult<SystemSetting>> ResetSettingToDefaultAsync(string key)
    {
        // This method might need a way to know the default value.
        // For now, let's assume it removes the setting, so it falls back to a hardcoded default in the app.
        // A better implementation would have default values stored somewhere.
        _logger.LogWarning("ResetSettingToDefaultAsync for {Key} is not fully implemented. Deleting setting as a fallback.", key);
        var result = await DeleteSettingAsync(key);
        if (result.IsSuccess)
        {
            // Returning a dummy setting object as the interface expects it.
            return ServiceResult<SystemSetting>.Success(new SystemSetting { SettingKey = key, SettingValue = "default" });
        }
        return ServiceResult<SystemSetting>.Failure("Failed to reset setting.");
    }

    public async Task<ServiceResult<List<string>>> GetCategoriesAsync()
    {
        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var categories = await context.SystemSettings
                                              .Select(s => s.Category)
                                              .Distinct()
                                              .ToListAsync();
                return ServiceResult<List<string>>.Success(categories);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting setting categories");
            return ServiceResult<List<string>>.Failure("Failed to retrieve setting categories");
        }
    }

    public async Task<ServiceResult<string>> GetSmtpHostAsync() => ServiceResult<string>.Success(await GetSettingValueAsync<string>("smtp_host", "smtp.gmail.com") ?? "smtp.gmail.com");
    public async Task<ServiceResult<int>> GetSmtpPortAsync() => ServiceResult<int>.Success(await GetSettingValueAsync<int>("smtp_port", 587));
    public async Task<ServiceResult<string>> GetSmtpUsernameAsync() => ServiceResult<string>.Success(await GetSettingValueAsync<string>("smtp_username") ?? string.Empty);
    public async Task<ServiceResult<string>> GetSmtpPasswordAsync() => ServiceResult<string>.Success(await GetSettingValueAsync<string>("smtp_password") ?? string.Empty);
    public async Task<ServiceResult<bool>> GetSmtpEnableTlsAsync() => ServiceResult<bool>.Success(await GetSettingValueAsync<bool>("smtp_enable_tls", true));
    public async Task<ServiceResult<bool>> GetSmtpEnableSslAsync() => ServiceResult<bool>.Success(await GetSettingValueAsync<bool>("smtp_enable_ssl", false));
    public async Task<ServiceResult<int>> GetEmailRateLimitAsync() => ServiceResult<int>.Success(await GetSettingValueAsync<int>("email_rate_limit", 5));
    public async Task<ServiceResult<int>> GetEmailRateLimitWindowSecondsAsync() => ServiceResult<int>.Success(await GetSettingValueAsync<int>("email_rate_limit_window_seconds", 60));

    // Email Rate Limit Settings
    public async Task<ServiceResult<int>> GetEmailRateLimitMaxRequestsAsync() => ServiceResult<int>.Success(await GetSettingValueAsync<int>("email_rate_limit_max_requests", 5));
    public async Task<ServiceResult<int>> GetEmailRateLimitWindowSecondsNewAsync() => ServiceResult<int>.Success(await GetSettingValueAsync<int>("email_rate_limit_window_seconds", 900));
    public async Task<ServiceResult<bool>> GetEmailRateLimitEnabledAsync() => ServiceResult<bool>.Success(await GetSettingValueAsync<bool>("email_rate_limit_enabled", true));
    public async Task<ServiceResult<int>> GetEmailRateLimitCooldownMinutesAsync() => ServiceResult<int>.Success(await GetSettingValueAsync<int>("email_rate_limit_cooldown_minutes", 60));
}
