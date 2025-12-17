using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EcommerceLaptop.Core.Entities
{
    /// <summary>
    /// Represents an LLM Provider (e.g., OpenAI, OpenRouter, Azure, Custom)
    /// </summary>
    public class LlmProvider
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // e.g., "OpenRouter", "OpenAI Official"
        
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = "openai"; // openai, azure, ollama, custom
        
        [MaxLength(255)]
        public string? BaseUrl { get; set; } // e.g., https://openrouter.ai/api/v1
        
        [MaxLength(255)]
        public string? Website { get; set; }
        
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<LlmProfile> Profiles { get; set; } = new List<LlmProfile>();
    }

    /// <summary>
    /// Represents a specific configuration profile for an LLM (Model + Settings)
    /// </summary>
    public class LlmProfile
    {
        public int Id { get; set; }
        
        public int ProviderId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // e.g., "Coding - DeepSeek V3"
        
        [Required]
        [MaxLength(100)]
        public string ModelId { get; set; } = string.Empty; // e.g., "deepseek/deepseek-chat"
        
        [MaxLength(255)]
        public string? ApiKey { get; set; } // Encrypted value
        
        public string ConfigJson { get; set; } = "{}"; // JSON for temperature, max_tokens, headers, etc.
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("ProviderId")]
        [JsonIgnore]
        public virtual LlmProvider? Provider { get; set; }
    }
}
