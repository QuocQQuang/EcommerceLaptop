using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Models.AI;

namespace EcommerceLaptop.API.Controllers.Admin
{
    [Authorize(Policy = "AdminOnly")]
    [Route("api/admin/config/llm")]
    [ApiController]
    public class ConfigController : ControllerBase
    {
        private readonly ILlmConfigProvider _configProvider;

        public ConfigController(ILlmConfigProvider configProvider)
        {
            _configProvider = configProvider;
        }

        [HttpGet]
        public async Task<ActionResult<LlmConfiguration>> GetConfig()
        {
            var config = await _configProvider.GetConfigAsync();
            // Mask API key for security in response
            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                config.ApiKey = config.ApiKey.Length > 4 
                    ? $"sk-****{config.ApiKey.Substring(config.ApiKey.Length - 4)}" 
                    : "****";
            }
            return Ok(config);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateConfig([FromBody] LlmConfiguration config)
        {
            // If API Key involves masking (starts with "sk-****" or represents "unchanged"), 
            // we should probably fetch current config to preserve it, OR the frontend handles "don't send if unchanged"
            // For simplicity: if config.ApiKey contains "****", we ignore it (keep existing).
            
            if (config.ApiKey.Contains("****")) 
            {
                var current = await _configProvider.GetConfigAsync();
                config.ApiKey = current.ApiKey;
            }

            var isValid = await _configProvider.ValidateConfigAsync(config);
            if (!isValid)
            {
                return BadRequest("Invalid configuration settings.");
            }

            await _configProvider.UpdateConfigAsync(config);
            return Ok(new { message = "Configuration updated successfully." });
        }

        [HttpPost("reload")]
        public ActionResult ReloadConfig()
        {
            _configProvider.InvalidateCache();
            return Ok(new { message = "Configuration cache invalidated." });
        }
    }
}
