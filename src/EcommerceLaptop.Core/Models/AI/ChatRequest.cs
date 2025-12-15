using System.Collections.Generic;

namespace EcommerceLaptop.Core.Models.AI
{
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public List<ChatMessage> History { get; set; } = new();
    }

    public class ChatMessage
    {
        public string Role { get; set; } = "user"; // user, assistant, system
        public string Content { get; set; } = string.Empty;
    }

    public class ChatResponseChunk
    {
        public string Content { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
        public List<string> Sources { get; set; } = new();
    }
}
