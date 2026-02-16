using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Infrastructure.Services.Security;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace EcommerceLaptop.Infrastructure.Services.AI
{
    public class LlmManagementService : ILlmManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Core.Interfaces.ILlmConfigProvider _llmConfigProvider;
        private readonly ISystemSettingsService _systemSettings;

        public LlmManagementService(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            Core.Interfaces.ILlmConfigProvider llmConfigProvider,
            ISystemSettingsService systemSettings)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _llmConfigProvider = llmConfigProvider;
            _systemSettings = systemSettings;
        }

        // Provider Management
        public async Task<IEnumerable<LlmProvider>> GetAllProvidersAsync()
        {
            return await _context.LlmProviders
                .Include(p => p.Profiles)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<LlmProvider?> GetProviderByIdAsync(int id)
        {
            return await _context.LlmProviders
                .Include(p => p.Profiles)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<LlmProvider> CreateProviderAsync(LlmProvider provider)
        {
            _context.LlmProviders.Add(provider);
            await _context.SaveChangesAsync();
            return provider;
        }

        public async Task UpdateProviderAsync(LlmProvider provider)
        {
            _context.Entry(provider).State = EntityState.Modified;
            provider.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteProviderAsync(int id)
        {
            var provider = await _context.LlmProviders.FindAsync(id);
            if (provider != null)
            {
                _context.LlmProviders.Remove(provider);
                await _context.SaveChangesAsync();
            }
        }

        // Profile Management
        public async Task<IEnumerable<LlmProfile>> GetProfilesByProviderIdAsync(int providerId)
        {
            return await _context.LlmProfiles
                .Where(p => p.ProviderId == providerId)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<LlmProfile?> GetProfileByIdAsync(int id)
        {
            return await _context.LlmProfiles
                .Include(p => p.Provider)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<LlmProfile> CreateProfileAsync(LlmProfile profile)
        {
            if (profile.IsActive)
            {
                // Deactivate others? Or allow multiple active? 
                // Requirement implies "Profile ring, config ring". System setting will point to *one* active profile globally for now.
                // We'll handle "Set Active" explicitly.
            }

            _context.LlmProfiles.Add(profile);
            await _context.SaveChangesAsync();
            return profile;
        }

        public async Task UpdateProfileAsync(LlmProfile profile)
        {
            _context.Entry(profile).State = EntityState.Modified;
            profile.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteProfileAsync(int id)
        {
            var profile = await _context.LlmProfiles.FindAsync(id);
            if (profile != null)
            {
                _context.LlmProfiles.Remove(profile);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SetActiveProfileAsync(int profileId)
        {
            // Update SystemSetting to point to this profile
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Category == "LLM_Config" && s.SettingKey == "ActiveProfileId");

            if (setting == null)
            {
                setting = new SystemSetting
                {
                    Category = "LLM_Config",
                    SettingKey = "ActiveProfileId",
                    DataType = "int"
                };
                _context.SystemSettings.Add(setting);
            }

            setting.SettingValue = profileId.ToString();
            setting.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Invalidate cache so new config takes effect immediately
            _llmConfigProvider.InvalidateCache();
        }

        public async Task<LlmProfile?> GetActiveProfileAsync()
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Category == "LLM_Config" && s.SettingKey == "ActiveProfileId");

            if (setting == null || !int.TryParse(setting.SettingValue, out int profileId))
            {
                return null;
            }

            return await GetProfileByIdAsync(profileId);
        }

        // Testing
        public async Task<bool> TestConnectionAsync(int profileId)
        {
            // Load profile with decrypted key
            var profile = await _context.LlmProfiles
                .Include(p => p.Provider)
                .FirstOrDefaultAsync(p => p.Id == profileId);

            if (profile == null) return false;

            string apiKey = profile.ApiKey; // Assume getter handles decryption or we decrypt here?
            // Current entities setup: getter decryption done via ValueConversion in DbContext. 
            // So 'profile.ApiKey' is ALREADY decrypted when accessed if mapped correctly. 
            // Wait, I used HasConversion in OnModelCreating. So yes, reading it gives plain text.

            var httpClient = _httpClientFactory.CreateClient("llm-test");

            // Simple test based on provider type
            try
            {
                if (profile.Provider.Type.ToLower() == "openai" || profile.Provider.Type.ToLower() == "openrouter")
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"{profile.Provider.BaseUrl ?? "https://api.openai.com/v1"}/models");
                    request.Headers.Add("Authorization", $"Bearer {apiKey}");

                    if (profile.Provider.Type.ToLower() == "openrouter")
                    {
                        request.Headers.Add("HTTP-Referer", "https://ecommercelaps.com");
                        request.Headers.Add("X-Title", "EcommerceLaptop");
                    }

                    var response = await httpClient.SendAsync(request);
                    return response.IsSuccessStatusCode;
                }
                // Add other provider tests...
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<IEnumerable<string>> FetchRemoteModelsAsync(EcommerceLaptop.Core.DTOs.AI.FetchModelsRequest request)
        {
            var httpClient = _httpClientFactory.CreateClient("llm-fetch");
            var baseUrl = request.BaseUrl?.TrimEnd('/');
            if (string.IsNullOrEmpty(baseUrl)) baseUrl = "https://api.openai.com/v1";

            var reqMsg = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/models");
            if (!string.IsNullOrEmpty(request.ApiKey))
            {
                reqMsg.Headers.Add("Authorization", $"Bearer {request.ApiKey}");
            }

            if (request.CustomHeaders != null)
            {
                foreach (var header in request.CustomHeaders)
                {
                    reqMsg.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Special handling for OpenRouter generic logic if needed, but CustomHeaders should cover it.

            var response = await httpClient.SendAsync(reqMsg);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            var models = new List<string>();
            if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in data.EnumerateArray())
                {
                    if (element.TryGetProperty("id", out var id))
                    {
                        models.Add(id.GetString() ?? "");
                    }
                }
            }
            return models.OrderBy(m => m);
        }

        public async Task<EcommerceLaptop.Core.DTOs.AI.ChatTestResponse> TestChatAsync(EcommerceLaptop.Core.DTOs.AI.TestChatRequest request)
        {
            var httpClient = _httpClientFactory.CreateClient("llm-test-chat");
            var baseUrl = request.BaseUrl?.TrimEnd('/');
            if (string.IsNullOrEmpty(baseUrl)) baseUrl = "https://api.openai.com/v1";

            var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
            if (!string.IsNullOrEmpty(request.ApiKey))
            {
                reqMsg.Headers.Add("Authorization", $"Bearer {request.ApiKey}");
            }

            if (request.CustomHeaders != null)
            {
                foreach (var header in request.CustomHeaders)
                {
                    reqMsg.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            var payload = new
            {
                model = request.ModelId,
                messages = new[] { new { role = "user", content = request.Message } },
                max_tokens = 50
            };

            reqMsg.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var response = await httpClient.SendAsync(reqMsg);
                sw.Stop();
                var latency = $"{sw.ElapsedMilliseconds}ms";

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    string? reply = null;

                    // Standard OpenAI format: choices[0].message.content
                    if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var firstChoice = choices[0];
                        if (firstChoice.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var contentProp))
                        {
                            reply = contentProp.GetString();
                        }
                    }

                    return new EcommerceLaptop.Core.DTOs.AI.ChatTestResponse
                    {
                        Success = true,
                        Message = reply ?? "Success (No content)",
                        Latency = latency
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return new EcommerceLaptop.Core.DTOs.AI.ChatTestResponse
                    {
                        Success = false,
                        Error = $"API Error ({response.StatusCode}): {errorContent}",
                        Latency = latency
                    };
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new EcommerceLaptop.Core.DTOs.AI.ChatTestResponse
                {
                    Success = false,
                    Error = $"Connection Failed: {ex.Message}",
                    Latency = $"{sw.ElapsedMilliseconds}ms"
                };
            }
        }

        // Rewriting Profile Management
        public async Task<int?> GetActiveRewritingProfileIdAsync()
        {
            var value = await _systemSettings.GetSettingValueAsync<string>("ActiveRewritingProfileId");
            return string.IsNullOrEmpty(value) ? null : int.TryParse(value, out var id) ? id : null;
        }

        public async Task SetActiveRewritingProfileAsync(int? profileId)
        {
            await _systemSettings.UpsertSettingAsync(
                "ActiveRewritingProfileId",
                profileId?.ToString() ?? string.Empty, // Use empty string instead of null
                "RAG",
                "LLM Profile ID used for query rewriting (null = disabled)");

            // Invalidate config cache to trigger rewriting kernel rebuild
            _llmConfigProvider.InvalidateCache();
        }
    }
}
