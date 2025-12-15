using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Models.AI;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Infrastructure.Services.Security;

namespace EcommerceLaptop.Infrastructure.Services
{
    public class LlmConfigProvider : ILlmConfigProvider
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;

        private const string CacheKey = "llm_config_v1";
        private const string RedisCacheKey = "global:llm_config_v1";

        public LlmConfigProvider(
            ApplicationDbContext context,
            IMemoryCache memoryCache,
            IDistributedCache distributedCache)
        {
            _context = context;
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
        }

        public async Task<LlmConfiguration> GetConfigAsync()
        {
            // L1: Memory Cache
            if (_memoryCache.TryGetValue(CacheKey, out LlmConfiguration? cachedConfig) && cachedConfig != null)
            {
                return cachedConfig;
            }

            // L2: Redis Cache
            var redisValue = await _distributedCache.GetStringAsync(RedisCacheKey);
            if (!string.IsNullOrEmpty(redisValue))
            {
                cachedConfig = JsonSerializer.Deserialize<LlmConfiguration>(redisValue);
                if (cachedConfig != null)
                {
                    _memoryCache.Set(CacheKey, cachedConfig, TimeSpan.FromMinutes(10));
                    return cachedConfig;
                }
            }

            // Database
            var config = await LoadFromDatabaseAsync();

            // Store in caches
            var serialized = JsonSerializer.Serialize(config);
            await _distributedCache.SetStringAsync(RedisCacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });
            _memoryCache.Set(CacheKey, config, TimeSpan.FromMinutes(10));

            return config;
        }

        public async Task UpdateConfigAsync(LlmConfiguration config)
        {
            // Save individual settings
            await SaveSettingAsync("llm_provider", config.Provider, false);
            await SaveSettingAsync("llm_model_id", config.ModelId, false);
            await SaveSettingAsync("llm_api_key", config.ApiKey, true);
            await SaveSettingAsync("llm_base_url", config.BaseUrl, false);
            await SaveSettingAsync("llm_temperature", config.Temperature.ToString(), false);
            await SaveSettingAsync("llm_max_tokens", config.MaxTokens.ToString(), false);
            await SaveSettingAsync("llm_streaming", config.StreamingEnabled.ToString(), false);

            await _context.SaveChangesAsync();

            // Invalidate Caches
            InvalidateCache();
        }

        public async Task<bool> ValidateConfigAsync(LlmConfiguration config)
        {
            // Basic validation logic
            if (string.IsNullOrWhiteSpace(config.ApiKey) && config.Provider != "Ollama")
                return false;
            
            if (string.IsNullOrWhiteSpace(config.ModelId))
                return false;

            return await Task.FromResult(true);
        }

        public void InvalidateCache()
        {
            _memoryCache.Remove(CacheKey);
            _distributedCache.Remove(RedisCacheKey);
        }

        private async Task<LlmConfiguration> LoadFromDatabaseAsync()
        {
            var settings = await _context.SystemSettings
                .Where(s => s.Category == "LLM_Config")
                .ToDictionaryAsync(s => s.SettingKey, s => s);

            var config = new LlmConfiguration();

            if (settings.TryGetValue("llm_provider", out var provider)) config.Provider = provider.SettingValue ?? "OpenAI";
            if (settings.TryGetValue("llm_model_id", out var model)) config.ModelId = model.SettingValue ?? "gpt-4o-mini";
            if (settings.TryGetValue("llm_base_url", out var baseUrl)) config.BaseUrl = baseUrl.SettingValue;
            
            if (settings.TryGetValue("llm_temperature", out var temp) && double.TryParse(temp.SettingValue, out var t))
                config.Temperature = t;
            
            if (settings.TryGetValue("llm_max_tokens", out var tokens) && int.TryParse(tokens.SettingValue, out var m))
                config.MaxTokens = m;
            
            if (settings.TryGetValue("llm_streaming", out var streaming) && bool.TryParse(streaming.SettingValue, out var s))
                config.StreamingEnabled = s;

            // Decrypt API Key
            if (settings.TryGetValue("llm_api_key", out var apiKey))
            {
                config.ApiKey = apiKey.IsEncrypted 
                    ? FieldEncryption.Decrypt(apiKey.SettingValue) 
                    : (apiKey.SettingValue ?? string.Empty);
            }

            return config;
        }

        private async Task SaveSettingAsync(string key, string? value, bool isEncrypted)
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Category == "LLM_Config" && s.SettingKey == key);

            if (setting == null)
            {
                setting = new SystemSetting
                {
                    Category = "LLM_Config",
                    SettingKey = key,
                    DataType = "string"
                };
                _context.SystemSettings.Add(setting);
            }

            setting.IsEncrypted = isEncrypted;
            setting.SettingValue = isEncrypted ? FieldEncryption.Encrypt(value) : value;
            setting.UpdatedAt = DateTime.UtcNow;
        }
    }
}
