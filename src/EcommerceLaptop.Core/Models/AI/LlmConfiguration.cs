using System.Collections.Generic;

namespace EcommerceLaptop.Core.Models.AI
{
    public class LlmConfiguration
    {
        public string Provider { get; set; } = "OpenAI"; // OpenAI, Azure, Ollama
        public string ModelId { get; set; } = "gpt-4o-mini";
        public string ApiKey { get; set; } = string.Empty;
        public string? BaseUrl { get; set; }
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 4096;
        public bool StreamingEnabled { get; set; } = true;
        public Dictionary<string, object> AdvancedOptions { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }
}
