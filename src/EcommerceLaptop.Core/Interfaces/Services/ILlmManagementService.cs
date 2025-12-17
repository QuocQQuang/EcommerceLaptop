using System.Collections.Generic;
using System.Threading.Tasks;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Interfaces.Services
{
    public interface ILlmManagementService
    {
        // Provider Management
        Task<IEnumerable<LlmProvider>> GetAllProvidersAsync();
        Task<LlmProvider?> GetProviderByIdAsync(int id);
        Task<LlmProvider> CreateProviderAsync(LlmProvider provider);
        Task UpdateProviderAsync(LlmProvider provider);
        Task DeleteProviderAsync(int id);

        // Profile Management
        Task<IEnumerable<LlmProfile>> GetProfilesByProviderIdAsync(int providerId);
        Task<LlmProfile?> GetProfileByIdAsync(int id);
        Task<LlmProfile> CreateProfileAsync(LlmProfile profile);
        Task UpdateProfileAsync(LlmProfile profile);
        Task DeleteProfileAsync(int id);
        Task SetActiveProfileAsync(int profileId);
        Task<int?> GetActiveRewritingProfileIdAsync();
        Task SetActiveRewritingProfileAsync(int? profileId);
        
        // Testing & Utilities
        Task<bool> TestConnectionAsync(int profileId);
        Task<IEnumerable<string>> FetchRemoteModelsAsync(EcommerceLaptop.Core.DTOs.AI.FetchModelsRequest request);
        Task<EcommerceLaptop.Core.DTOs.AI.ChatTestResponse> TestChatAsync(EcommerceLaptop.Core.DTOs.AI.TestChatRequest request);
    }
}
