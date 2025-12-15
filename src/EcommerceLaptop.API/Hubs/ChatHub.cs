using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Models.AI;

namespace EcommerceLaptop.API.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task SendMessage(ChatRequest request)
        {
            request.ConnectionId = Context.ConnectionId;
            
            await foreach (var chunk in _chatService.ProcessMessageAsync(request))
            {
                await Clients.Caller.SendAsync("ReceiveToken", chunk);
            }
            
            await Clients.Caller.SendAsync("StreamCompleted");
        }
    }
}
