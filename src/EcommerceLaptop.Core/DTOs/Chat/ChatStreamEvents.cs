using System;
using System.Text.Json.Serialization;

namespace EcommerceLaptop.Core.DTOs.Chat;

// Base Event
[JsonDerivedType(typeof(TokenEvent), typeDiscriminator: "token")]
[JsonDerivedType(typeof(ProductEvent), typeDiscriminator: "product")]
[JsonDerivedType(typeof(MetadataEvent), typeDiscriminator: "metadata")]
[JsonDerivedType(typeof(CompleteEvent), typeDiscriminator: "complete")]
[JsonDerivedType(typeof(ErrorEvent), typeDiscriminator: "error")]
[JsonDerivedType(typeof(ProgressEvent), typeDiscriminator: "progress")]
[JsonDerivedType(typeof(OrderEvent), typeDiscriminator: "order")]
public abstract class ChatStreamEvent
{
    public string Type { get; protected set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// Token Event (Streaming content)
public class TokenEvent : ChatStreamEvent
{
    public TokenEvent()
    {
        Type = "token";
    }
    
    public string Token { get; set; }
    public int Index { get; set; }
}

// Product Event (RAG results)
public class ProductEvent : ChatStreamEvent
{
    public ProductEvent()
    {
        Type = "product";
    }
    
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public double RelevanceScore { get; set; }
    public string Reasoning { get; set; }
    public int Rank { get; set; }
    // Optional: Add Price/Image if needed for UI, but Card usually fetches details or has basic info
    public decimal Price { get; set; }
    public string ImageUrl { get; set; }
    public string Slug { get; set; }
}

// Metadata Event (Stats)
public class MetadataEvent : ChatStreamEvent
{
    public MetadataEvent()
    {
        Type = "metadata";
    }
    
    public string ProcessingStage { get; set; }
    public int ItemsFound { get; set; }
    public bool CacheHit { get; set; }
    public long ElapsedMs { get; set; }
    public string? SessionId { get; set; }
}

// Complete Event (Final Summary)
public class CompleteEvent : ChatStreamEvent
{
    public CompleteEvent()
    {
        Type = "complete";
    }
    
    public string QueryId { get; set; }
    public int TotalTokens { get; set; }
    public long DurationMs { get; set; }
    public double TokensPerSecond { get; set; }
    public bool CacheHit { get; set; }
    public string ModelUsed { get; set; }
}

// Error Event
public class ErrorEvent : ChatStreamEvent
{
    public ErrorEvent()
    {
        Type = "error";
    }
    
    public string Code { get; set; }
    public string Message { get; set; }
    public bool Recoverable { get; set; }
    public int? RetryAfterMs { get; set; }
}

// Progress Event (Loading states)
public class ProgressEvent : ChatStreamEvent
{
    public ProgressEvent()
    {
        Type = "progress";
    }
    
    public string Stage { get; set; }
    public double Progress { get; set; } // 0.0 to 1.0
    public string Message { get; set; }
}

// Order Event (Rich UI)
public class OrderEvent : ChatStreamEvent
{
    public OrderEvent()
    {
        Type = "order";
    }
    
    public int Id { get; set; }
    public string Status { get; set; } // "Pending", "Shipped" etc.
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    // Minimal item details for the card
    public List<string> Items { get; set; } = new List<string>(); 
}
