using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Models.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services.AI
{
    public class SemanticCacheService : ISemanticCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<SemanticCacheService> _logger;
        private const string CachePrefix = "rag_cache:";

        public SemanticCacheService(IDistributedCache cache, ILogger<SemanticCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<ChatResponseChunk?> GetCachedResponseAsync(string query)
        {
            var key = GenerateCacheKey(query);
            var cachedData = await _cache.GetStringAsync(key);

            if (string.IsNullOrEmpty(cachedData))
            {
                return null;
            }

            _logger.LogInformation("Semantic Cache HIT for query: {Query}", query);
            return new ChatResponseChunk
            {
                Content = cachedData,
                IsComplete = true,
                Sources = new System.Collections.Generic.List<string> { "Cache" }
            };
        }

        public async Task CacheResponseAsync(string query, string responseContent)
        {
            var key = GenerateCacheKey(query);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) // Cache for 24 hours
            };

            await _cache.SetStringAsync(key, responseContent, options);
            _logger.LogInformation("Cached response for query: {Query}", query);
        }

        private string GenerateCacheKey(string query)
        {
            // Normalize query: lowercase, trim
            var normalized = query.Trim().ToLowerInvariant();
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            var hash = Convert. ToHexString(bytes);
            return $"{CachePrefix}{hash}";
        }
    }
}
