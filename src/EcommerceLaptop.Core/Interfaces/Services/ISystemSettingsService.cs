using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.ValueObjects;
using EcommerceLaptop.Core.DTOs;

namespace EcommerceLaptop.Core.Interfaces.Services;

public interface ISystemSettingsService
{
    /// <summary>
    /// Get all system settings grouped by category
    /// </summary>
    Task<ServiceResult<Dictionary<string, List<SystemSetting>>>> GetAllSettingsGroupedAsync();

    /// <summary>
    /// Get settings by category
    /// </summary>
    Task<ServiceResult<List<SystemSetting>>> GetSettingsByCategoryAsync(string category);

    /// <summary>
    /// Get a single setting by key
    /// </summary>
    Task<ServiceResult<SystemSetting?>> GetSettingByKeyAsync(string key);

    /// <summary>
    /// Get setting value by key (typed)
    /// </summary>
    Task<T?> GetSettingValueAsync<T>(string key, T? defaultValue = default);

    /// <summary>
    /// Update or create a setting
    /// </summary>
    Task<ServiceResult<SystemSetting>> UpsertSettingAsync(string key, string value, string category = "General", string? description = null);

    /// <summary>
    /// Update multiple settings
    /// </summary>
    Task<ServiceResult<bool>> UpdateSettingsAsync(List<SystemSettingUpdateRequest> settings);

    /// <summary>
    /// Delete a setting
    /// </summary>
    Task<ServiceResult<bool>> DeleteSettingAsync(string key);

    /// <summary>
    /// Reset setting to default value
    /// </summary>
    Task<ServiceResult<SystemSetting>> ResetSettingToDefaultAsync(string key);

    /// <summary>
    /// Get all available setting categories
    /// </summary>
    Task<ServiceResult<List<string>>> GetCategoriesAsync();

    // SMTP Settings
    Task<ServiceResult<string>> GetSmtpHostAsync();
    Task<ServiceResult<int>> GetSmtpPortAsync();
    Task<ServiceResult<string>> GetSmtpUsernameAsync();
    Task<ServiceResult<string>> GetSmtpPasswordAsync();
    Task<ServiceResult<bool>> GetSmtpEnableTlsAsync();
    Task<ServiceResult<bool>> GetSmtpEnableSslAsync();

    // Email Rate Limit Settings
    Task<ServiceResult<int>> GetEmailRateLimitAsync();
    Task<ServiceResult<int>> GetEmailRateLimitWindowSecondsAsync();
    Task<ServiceResult<int>> GetEmailRateLimitMaxRequestsAsync();
    Task<ServiceResult<int>> GetEmailRateLimitWindowSecondsNewAsync();
    Task<ServiceResult<bool>> GetEmailRateLimitEnabledAsync();
    Task<ServiceResult<int>> GetEmailRateLimitCooldownMinutesAsync();
}