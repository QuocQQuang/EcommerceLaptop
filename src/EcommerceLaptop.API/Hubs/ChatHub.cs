using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.DTOs.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace EcommerceLaptop.API.Hubs
{
    // [Authorize] // Allow anonymous for Guest access
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatHub> _logger;
        private readonly IMemoryCache _cache;
        private static readonly ConcurrentDictionary<string, int> _activeConnections = new();

        public ChatHub(IChatService chatService, ILogger<ChatHub> logger, IMemoryCache cache)
        {
            _chatService = chatService;
            _logger = logger;
            _cache = cache;
        }

        public override async Task OnConnectedAsync()
        {
            var clientIp = GetClientIp();
            _activeConnections.AddOrUpdate(clientIp, 1, (key, count) => count + 1);
            
            // Limit connections per IP
            if (_activeConnections[clientIp] > 10)
            {
                _logger.LogWarning("Too many connections from IP: {IP}", clientIp);
                Context.Abort();
                return;
            }
            
            _logger.LogInformation("Client connected: {ConnectionId} from {IP}", Context.ConnectionId, clientIp);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var clientIp = GetClientIp();
            _activeConnections.AddOrUpdate(clientIp, 0, (key, count) => Math.Max(0, count - 1));
            
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        private string GetClientIp()
        {
            var httpContext = Context.GetHttpContext();
            return httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private bool IsRateLimited(string identifier)
        {
            var rateLimitKey = $"rate_limit_chat_{identifier}";
            var requestCount = _cache.GetOrCreate(rateLimitKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return 0;
            });

            if (requestCount >= 20) // 20 messages per minute
            {
                _logger.LogWarning("Rate limit exceeded for {Identifier}", identifier);
                return true;
            }

            _cache.Set(rateLimitKey, requestCount + 1, TimeSpan.FromMinutes(1));
            return false;
        }

        public async Task SendQuery(string query, QueryOptions? options = null)
        {
            var cancellationToken = Context.ConnectionAborted;
            
            // Input validation
            if (string.IsNullOrWhiteSpace(query))
            {
                await Clients.Caller.SendAsync("ReceiveEvent", new ErrorEvent
                {
                    Code = "INVALID_INPUT",
                    Message = "Query cannot be empty.",
                    Recoverable = true
                }, cancellationToken);
                return;
            }

            if (query.Length > 2000) // Limit query length
            {
                await Clients.Caller.SendAsync("ReceiveEvent", new ErrorEvent
                {
                    Code = "INVALID_INPUT",
                    Message = "Query is too long. Maximum 2000 characters.",
                    Recoverable = true
                }, cancellationToken);
                return;
            }
            
            // Extract User ID from JWT Claims (int format)
            string? userId = null;
            var httpContext = Context.GetHttpContext();
            
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                // Get int userId from NameIdentifier claim
                userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            }
            
            // Fallback to guest ID if not authenticated
            if (string.IsNullOrEmpty(userId) && httpContext != null)
            {
                // Check Header (fallback if valid)
                if (httpContext.Request.Headers.TryGetValue("X-Guest-Id", out var guestIdHeader))
                {
                    userId = guestIdHeader.ToString();
                }
                // Check Query String (Primary for WebSockets)
                else if (httpContext.Request.Query.TryGetValue("guest_id", out var guestIdQuery))
                {
                    userId = guestIdQuery.ToString();
                }
            }

            var sessionId = options?.SessionId;

            // Rate limiting per user/guest
            var rateLimitIdentifier = userId ?? Context.ConnectionId;
            if (IsRateLimited(rateLimitIdentifier))
            {
                await Clients.Caller.SendAsync("ReceiveEvent", new ErrorEvent
                {
                    Code = "RATE_LIMIT_EXCEEDED",
                    Message = "Too many requests. Please wait a moment and try again.",
                    Recoverable = true
                }, cancellationToken);
                return;
            }

            _logger.LogInformation("Processing chat query from {UserId}: {Query}", userId ?? "Guest", query.Substring(0, Math.Min(50, query.Length)));

            try
            {
                await foreach (var evt in _chatService.StreamChatAsync(query, sessionId, userId, cancellationToken))
                {
                    await Clients.Caller.SendAsync("ReceiveEvent", evt, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat query: {Message}", ex.Message);
                
                await Clients.Caller.SendAsync("ReceiveEvent", new ErrorEvent 
                { 
                    Code = "SYSTEM_ERROR", 
                    Message = "I'm encountering a temporary server issue. Please try again in a few moments.",
                    Recoverable = false
                }, cancellationToken);
            }
        }
    }

    public class QueryOptions
    {
        public string? SessionId { get; set; }
    }
}
