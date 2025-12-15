using System.Collections.Generic;
using System.Threading.Tasks;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.DTOs.Chat;

namespace EcommerceLaptop.Core.Interfaces.Services
{
    public interface IChatPersistenceService
    {
        Task<string> CreateSessionAsync(string userId, string title);
        Task SaveMessageAsync(string sessionId, string role, string content, string? metadata = null);
        Task<IEnumerable<ChatMessage>> GetSessionHistoryAsync(string sessionId);
        Task<IEnumerable<ChatSession>> GetUserSessionsAsync(string userId);
        Task DeleteSessionAsync(string sessionId, string userId);
    }
}
