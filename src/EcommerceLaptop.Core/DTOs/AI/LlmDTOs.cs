using System;
using System.Collections.Generic;

namespace EcommerceLaptop.Core.DTOs.AI
{
    public class FetchModelsRequest
    {
        public string ProviderType { get; set; } = "openai";
        public string? BaseUrl { get; set; }
        public string? ApiKey { get; set; }
        public Dictionary<string, string>? CustomHeaders { get; set; }
    }

    public class TestChatRequest
    {
        public string ProviderType { get; set; } = "openai";
        public string? BaseUrl { get; set; }
        public string? ApiKey { get; set; }
        public string ModelId { get; set; } = string.Empty;
        public Dictionary<string, string>? CustomHeaders { get; set; }
         public string Message { get; set; } = "Hello";
    }

    /// <summary>
    /// Safe DTO for returning LLM profile data - never includes the raw ApiKey.
    /// </summary>
    public class LlmProfileResponseDto
    {
        public int Id { get; set; }
        public int ProviderId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        /// <summary>Indicates whether an API key is stored, without revealing it.</summary>
        public bool HasApiKey { get; set; }
        public string ConfigJson { get; set; } = "{}";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ChatTestResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Latency { get; set; }
        public string? Error { get; set; }
    }
}
