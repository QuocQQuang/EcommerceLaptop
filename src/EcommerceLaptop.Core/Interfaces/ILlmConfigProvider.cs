using System.Threading.Tasks;
using EcommerceLaptop.Core.Models.AI;

namespace EcommerceLaptop.Core.Interfaces
{
    public interface ILlmConfigProvider
    {
        Task<LlmConfiguration> GetConfigAsync();
        Task UpdateConfigAsync(LlmConfiguration config);
        Task<bool> ValidateConfigAsync(LlmConfiguration config);
        void InvalidateCache();
    }
}
