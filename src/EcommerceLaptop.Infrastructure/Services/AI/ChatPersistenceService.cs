using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace EcommerceLaptop.Infrastructure.Services.AI
{
    public class ChatPersistenceService : IChatPersistenceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache; // Using IDistributedCache for Redis abstraction
        
        // Cache Key Pattern: "chat_session:{sessionId}"
        private const string CacheKeyPrefix = "chat_session:";
        private readonly DistributedCacheEntryOptions _cacheOptions;

        public ChatPersistenceService(ApplicationDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) // Keep active chats in Redis for 24h
            };
        }

        public async Task<string> CreateSessionAsync(string userId, string title)
        {
            var session = new ChatSession
            {
                UserId = userId,
                Title = title,
                StartedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };

            // Write to SQL
            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();
            
            // Note: We don't necessarily need to cache an empty session immediately required features
            // But we could cache the metadata.
            
            return session.Id.ToString();
        }

        public async Task SaveMessageAsync(string sessionId, string role, string content, string? metadata = null)
        {
            if (!int.TryParse(sessionId, out int sId)) return;

            var message = new ChatMessage
            {
                SessionId = sId,
                Role = role,
                Content = content,
                Timestamp = DateTime.UtcNow,
                MetadataJson = metadata
            };

            // 1. Write to SQL (Database of Record)
            _context.ChatMessages.Add(message);
            
            // Update Session Timestamp
            var session = await _context.ChatSessions.FindAsync(sId);
            if (session != null)
            {
                session.LastUpdatedAt = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();

            // 2. Invalidate Cache (or Append if using complex Redis types, but IDistributedCache is key-value)
            // Strategy: Remove the cached history so next read fetches fresh from DB
            await _cache.RemoveAsync(CacheKeyPrefix + sessionId);
            
            // Optimization for later: Use ConnectionMultiplexer to append to Redis List directly
        }

        public async Task<IEnumerable<ChatMessage>> GetSessionHistoryAsync(string sessionId)
        {
            if (!int.TryParse(sessionId, out int sId)) return new List<ChatMessage>();

            var cacheKey = CacheKeyPrefix + sessionId;
            
            // 1. Try Read from Cache
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                try 
                {
                    return JsonSerializer.Deserialize<IEnumerable<ChatMessage>>(cachedData);
                }
                catch 
                {
                    // If deserialize fails, fallback to DB
                }
            }

            // 2. Fallback to SQL
            var messages = await _context.ChatMessages
                .Where(m => m.SessionId == sId)
                .OrderBy(m => m.Timestamp)
                .AsNoTracking() // Read-only optimization
                .ToListAsync();

            if (messages.Any())
            {
                // 3. Write to Cache (Read-Through)
                // Serialize with ReferenceHandler if needed? 
                // Entities usually have cycles (Session -> Messages), but we just want the list of messages.
                // We should project to DTOs normally, but for now we cache the entities (ignoring navigation props via serialization options)
                
                var json = JsonSerializer.Serialize(messages, new JsonSerializerOptions 
                { 
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles 
                });
                
                await _cache.SetStringAsync(cacheKey, json, _cacheOptions);
            }

            return messages;
        }

        public async Task<IEnumerable<ChatSession>> GetUserSessionsAsync(string userId)
        {
            // Usually not cached unless highly accessed
            return await _context.ChatSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.LastUpdatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task DeleteSessionAsync(string sessionId, string userId)
        {
            if (!int.TryParse(sessionId, out int sId)) return;

            var session = await _context.ChatSessions.FirstOrDefaultAsync(s => s.Id == sId && s.UserId == userId);
            if (session != null)
            {
                _context.ChatSessions.Remove(session);
                await _context.SaveChangesAsync();
                
                // Clear Cache
                await _cache.RemoveAsync(CacheKeyPrefix + sessionId);
            }
        }
    }
}
