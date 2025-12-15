using System.Collections.Generic;
using System.Threading.Tasks;
using EcommerceLaptop.Core.Models.AI;
using EcommerceLaptop.Core.DTOs.Chat;

namespace EcommerceLaptop.Core.Interfaces
{
    public interface IChatService
    {
        IAsyncEnumerable<ChatStreamEvent> StreamChatAsync(string query, string? sessionId = null, string? userId = null, CancellationToken cancellationToken = default);
        
        // Future methods for persistence (Day 3)
        // Task<string> GetSessionAsync(string sessionId);
    }
}
