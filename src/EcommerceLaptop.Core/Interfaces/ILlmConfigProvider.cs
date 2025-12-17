using System.Threading.Tasks;
using EcommerceLaptop.Core.Models.AI;

namespace EcommerceLaptop.Core.Interfaces
{
    public interface ILlmConfigProvider
    {
        event Action? OnConfigInvalidated;
        
        Task<LlmConfiguration> GetConfigAsync();
        Task<LlmConfiguration?> GetConfigForProfileAsync(int profileId);
        Task UpdateConfigAsync(LlmConfiguration config);
        Task<bool> ValidateConfigAsync(LlmConfiguration config);
        void InvalidateCache();
    }
}
