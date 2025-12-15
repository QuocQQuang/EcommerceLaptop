using System.Threading.Tasks;
using EcommerceLaptop.Core.Models.AI;

namespace EcommerceLaptop.Core.Interfaces
{
    public interface ISemanticCacheService
    {
        Task<ChatResponseChunk?> GetCachedResponseAsync(string query);
        Task CacheResponseAsync(string query, string responseContent);
    }
}
