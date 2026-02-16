using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Core.DTOs.AI;
using System.Threading.Tasks;

namespace EcommerceLaptop.API.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [ApiController]
    [Route("api/[controller]")]
    public class LlmController : ControllerBase
    {
        private readonly ILlmManagementService _llmService;
        private readonly ISystemSettingsService _systemSettings;

        public LlmController(ILlmManagementService llmService, ISystemSettingsService systemSettings)
        {
            _llmService = llmService;
            _systemSettings = systemSettings;
        }

        // --- Providers ---

        [HttpGet("providers")]
        public async Task<IActionResult> GetAllProviders()
        {
            var providers = await _llmService.GetAllProvidersAsync();
            return Ok(providers);
        }

        [HttpGet("providers/{id}")]
        public async Task<IActionResult> GetProvider(int id)
        {
            var provider = await _llmService.GetProviderByIdAsync(id);
            if (provider == null) return NotFound();
            return Ok(provider);
        }

        [HttpPost("providers")]
        public async Task<IActionResult> CreateProvider([FromBody] LlmProvider provider)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var created = await _llmService.CreateProviderAsync(provider);
            return CreatedAtAction(nameof(GetProvider), new { id = created.Id }, created);
        }

        [HttpPut("providers/{id}")]
        public async Task<IActionResult> UpdateProvider(int id, [FromBody] LlmProvider provider)
        {
            if (id != provider.Id) return BadRequest();
            await _llmService.UpdateProviderAsync(provider);
            return NoContent();
        }

        [HttpDelete("providers/{id}")]
        public async Task<IActionResult> DeleteProvider(int id)
        {
            await _llmService.DeleteProviderAsync(id);
            return NoContent();
        }

        // --- Profiles ---

        [HttpGet("profiles/active")]
        public async Task<IActionResult> GetActiveProfile()
        {
            var profile = await _llmService.GetActiveProfileAsync();
            if (profile == null) return NotFound();
            // Frontend expects: { id: number; name: string; providerId: number }
            return Ok(new
            {
                id = profile.Id,
                name = profile.Name,
                providerId = profile.ProviderId
            });
        }

        [HttpGet("providers/{providerId}/profiles")]
        public async Task<IActionResult> GetProfiles(int providerId)
        {
            var profiles = await _llmService.GetProfilesByProviderIdAsync(providerId);
            return Ok(profiles);
        }

        [HttpGet("profiles/{id}")]
        public async Task<IActionResult> GetProfile(int id)
        {
            var profile = await _llmService.GetProfileByIdAsync(id);
            if (profile == null) return NotFound();
            return Ok(profile);
        }

        [HttpPost("profiles")]
        public async Task<IActionResult> CreateProfile([FromBody] LlmProfile profile)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var created = await _llmService.CreateProfileAsync(profile);
            return CreatedAtAction(nameof(GetProfile), new { id = created.Id }, created);
        }

        [HttpPut("profiles/{id}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] LlmProfile profile)
        {
            if (id != profile.Id) return BadRequest();
            await _llmService.UpdateProfileAsync(profile);
            return NoContent();
        }

        [HttpDelete("profiles/{id}")]
        public async Task<IActionResult> DeleteProfile(int id)
        {
            await _llmService.DeleteProfileAsync(id);
            return NoContent();
        }

        // --- Rewriting Profile ---

        [HttpGet("rewriting-profile")]
        public async Task<IActionResult> GetActiveRewritingProfile()
        {
            var profileId = await _llmService.GetActiveRewritingProfileIdAsync();
            return Ok(new { profileId });
        }

        [HttpPost("rewriting-profile")]
        public async Task<IActionResult> SetActiveRewritingProfile([FromBody] UpdateRewritingProfileRequest request)
        {
            await _llmService.SetActiveRewritingProfileAsync(request.ProfileId);
            return Ok();
        }

        [HttpPost("profiles/{id}/test")]
        public async Task<IActionResult> TestConnection(int id)
        {
            bool success = await _llmService.TestConnectionAsync(id);
            if (success) return Ok(new { message = "Connection successful" });
            return BadRequest(new { message = "Connection failed" });
        }

        [HttpPost("profiles/{id}/activate")]
        public async Task<IActionResult> ActivateProfile(int id)
        {
            await _llmService.SetActiveProfileAsync(id);
            return Ok(new { message = "Profile activated" });
        }

        // --- Click & Play Utilities ---

        [HttpPost("providers/models")]
        public async Task<IActionResult> FetchModels([FromBody] FetchModelsRequest request)
        {
            try
            {
                var models = await _llmService.FetchRemoteModelsAsync(request);
                return Ok(models);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("test-chat")]
        public async Task<IActionResult> TestChat([FromBody] TestChatRequest request)
        {
            var result = await _llmService.TestChatAsync(request);
            return Ok(result);
        }

        [HttpPost("test-embedding")]
        public async Task<IActionResult> TestEmbedding([FromServices] EcommerceLaptop.Core.Interfaces.IEmbeddingService embeddingService)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var vector = await embeddingService.GenerateEmbeddingAsync("Test latency string");
                sw.Stop();
                return Ok(new
                {
                    success = true,
                    latencyMs = sw.ElapsedMilliseconds,
                    dimensions = vector.Length,
                    message = $"Embedding generated in {sw.ElapsedMilliseconds}ms ({vector.Length} dims)"
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                return BadRequest(new
                {
                    success = false,
                    latencyMs = sw.ElapsedMilliseconds,
                    message = ex.Message
                });
            }
        }
    }


    public class UpdateRewritingProfileRequest
    {
        public int? ProfileId { get; set; }
    }
}
