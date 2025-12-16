using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.DTOs.Chat;

namespace EcommerceLaptop.API.Hubs
{
    // [Authorize] // Allow anonymous for Guest access
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task SendQuery(string query, QueryOptions? options = null)
        {
            var cancellationToken = Context.ConnectionAborted;
            
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

            try
            {
                await foreach (var evt in _chatService.StreamChatAsync(query, sessionId, userId, cancellationToken))
                {
                    await Clients.Caller.SendAsync("ReceiveEvent", evt, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ReceiveEvent", new ErrorEvent 
                { 
                    Code = "SYSTEM_ERROR", 
                    Message = ex.Message,
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
