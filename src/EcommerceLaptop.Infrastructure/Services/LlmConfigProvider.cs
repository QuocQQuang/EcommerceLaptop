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

        private const string CacheKey = "llm_config_v2";
        private const string RedisCacheKey = "global:llm_config_v2";

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
           // NOTE: This legacy method is less relevant now as we update Profiles via LlmManagementService.
           // However, if called, we could try to update the "Active" profile if it matches, 
           // or just invalidate cache. For now, we'll just invalidate cache to force reload.
           InvalidateCache();
           await Task.CompletedTask;
        }

        public async Task<bool> ValidateConfigAsync(LlmConfiguration config)
        {
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
            // 1. Get Active Profile ID
            var activeProfileSetting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Category == "LLM_Config" && s.SettingKey == "ActiveProfileId");

            LlmProfile? profile = null;

            if (activeProfileSetting != null && int.TryParse(activeProfileSetting.SettingValue, out int profileId))
            {
                profile = await _context.LlmProfiles
                    .Include(p => p.Provider)
                    .FirstOrDefaultAsync(p => p.Id == profileId);
            }
            
            // Fallback: If no active profile, try to find any "OpenAI" one or create default
            if (profile == null)
            {
                // Logic to fallback to legacy system settings or default
                return await LoadLegacyOrDefaultConfigAsync();
            }

            // 2. Map Profile to LlmConfiguration
            var config = new LlmConfiguration
            {
                Provider = profile.Provider.Type,
                ModelId = profile.ModelId,
                ApiKey = profile.ApiKey ?? string.Empty, // Value conversion handles decryption
                BaseUrl = profile.Provider.BaseUrl
            };
            
            // 3. Parse JSON config
            if (!string.IsNullOrEmpty(profile.ConfigJson))
            {
                try 
                {
                    var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var doc = JsonDocument.Parse(profile.ConfigJson);
                    
                    if (doc.RootElement.TryGetProperty("temperature", out var temp)) config.Temperature = temp.GetDouble();
                    if (doc.RootElement.TryGetProperty("max_tokens", out var max)) config.MaxTokens = max.GetInt32();
                    if (doc.RootElement.TryGetProperty("streaming", out var stream)) config.StreamingEnabled = stream.GetBoolean();
                    
                    // Parse Headers
                     if (doc.RootElement.TryGetProperty("headers", out var headers))
                     {
                         foreach(var prop in headers.EnumerateObject())
                         {
                             config.Headers[prop.Name] = prop.Value.ToString();
                         }
                     }
                     
                     // Parse everything else into AdvancedOptions
                     foreach(var prop in doc.RootElement.EnumerateObject())
                     {
                         string key = prop.Name.ToLower();
                         if (key != "temperature" && key != "max_tokens" && key != "streaming" && key != "headers")
                         {
                             object? val = prop.Value.ValueKind switch {
                                 JsonValueKind.String => prop.Value.GetString(),
                                 JsonValueKind.Number => prop.Value.GetDouble(),
                                 JsonValueKind.True => true,
                                 JsonValueKind.False => false,
                                 _ => prop.Value.ToString()
                             };
                             if (val != null) config.AdvancedOptions[key] = val;
                         }
                     }
                }
                catch 
                { 
                    // Ignore JSON parse errors, stick to defaults
                }
            }

            return config;
        }

        private async Task<LlmConfiguration> LoadLegacyOrDefaultConfigAsync()
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

            if (settings.TryGetValue("llm_api_key", out var apiKey))
            {
                config.ApiKey = apiKey.IsEncrypted 
                    ? FieldEncryption.Decrypt(apiKey.SettingValue) 
                    : (apiKey.SettingValue ?? string.Empty);
            }
            return config;
        }
    }
}
