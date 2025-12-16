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
            
            // Try get Authenticated User ID or Guest ID from Header
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId))
            {
                var httpContext = Context.GetHttpContext();
                if (httpContext != null)
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
