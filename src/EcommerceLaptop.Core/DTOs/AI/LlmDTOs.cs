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

    public class ChatTestResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Latency { get; set; }
        public string? Error { get; set; }
    }
}
