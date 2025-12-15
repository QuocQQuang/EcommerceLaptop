using System;
using System.Collections.Generic;
using EcommerceLaptop.Core.Common;
using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Core.Entities
{
    public class ChatSession : BaseEntity
    {
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } // Can be null for guest, but we usually enforce auth or guest session ID
        
        [MaxLength(100)]
        public string Title { get; set; }
        
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
        
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }

    public class ChatMessage : BaseEntity
    {
        public int SessionId { get; set; }
        public ChatSession Session { get; set; }

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } // "user", "assistant", "system"
        
        [Required]
        public string Content { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // Metadata for RAG citations or product references
        public string? MetadataJson { get; set; }
    }
}
