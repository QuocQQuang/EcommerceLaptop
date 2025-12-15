using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.DTOs.Chat;

namespace EcommerceLaptop.API.Hubs
{
    [Authorize]
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
            var userId = Context.UserIdentifier;
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
