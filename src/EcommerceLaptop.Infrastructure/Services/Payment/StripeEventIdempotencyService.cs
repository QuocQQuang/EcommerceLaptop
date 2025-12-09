using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Stripe;

namespace EcommerceLaptop.Infrastructure.Services.Payment;

/// <summary>
/// Service for handling Stripe event idempotency to prevent duplicate webhook processing
/// Implements Stripe best practices for handling duplicate events
/// </summary>
public class StripeEventIdempotencyService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<StripeEventIdempotencyService> _logger;
    private readonly TimeSpan _defaultCacheExpiry = TimeSpan.FromHours(24);

    public StripeEventIdempotencyService(
        IMemoryCache cache,
        ILogger<StripeEventIdempotencyService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Checks if a Stripe event has already been processed (idempotency check)
    /// </summary>
    /// <param name="stripeEvent">The Stripe event to check</param>
    /// <returns>True if event has already been processed</returns>
    public bool HasEventBeenProcessed(Event stripeEvent)
    {
        if (stripeEvent == null)
        {
            _logger.LogWarning("Stripe event is null for idempotency check");
            return true; // Treat null events as already processed
        }

        var cacheKey = GenerateEventCacheKey(stripeEvent);
        var hasBeenProcessed = _cache.TryGetValue(cacheKey, out _);

        if (hasBeenProcessed)
        {
            _logger.LogInformation("Stripe event {EventId} of type {EventType} has already been processed", 
                stripeEvent.Id, stripeEvent.Type);
        }

        return hasBeenProcessed;
    }

    /// <summary>
    /// Marks a Stripe event as processed for idempotency
    /// </summary>
    /// <param name="stripeEvent">The Stripe event to mark as processed</param>
    public void MarkEventAsProcessed(Event stripeEvent)
    {
        if (stripeEvent == null)
        {
            _logger.LogWarning("Cannot mark null Stripe event as processed");
            return;
        }

        var cacheKey = GenerateEventCacheKey(stripeEvent);
        
        _cache.Set(cacheKey, true, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _defaultCacheExpiry,
            Priority = CacheItemPriority.Normal
        });

        _logger.LogDebug("Marked Stripe event {EventId} of type {EventType} as processed", 
            stripeEvent.Id, stripeEvent.Type);
    }

    /// <summary>
    /// Checks for duplicate events using object ID and event type
    /// This handles cases where the same action generates multiple events
    /// </summary>
    /// <param name="stripeEvent">The Stripe event to check</param>
    /// <returns>True if a similar event has been processed</returns>
    public bool HasSimilarEventBeenProcessed(Event stripeEvent)
    {
        if (stripeEvent?.Data?.Object == null)
        {
            return true;
        }

        // Extract object ID from the event data
        var objectId = ExtractObjectId(stripeEvent);
        if (string.IsNullOrEmpty(objectId))
        {
            return false;
        }

        var similarEventCacheKey = GenerateSimilarEventCacheKey(stripeEvent.Type, objectId);
        var hasBeenProcessed = _cache.TryGetValue(similarEventCacheKey, out _);

        if (hasBeenProcessed)
        {
            _logger.LogInformation("Similar Stripe event for object {ObjectId} of type {EventType} has already been processed", 
                objectId, stripeEvent.Type);
        }

        return hasBeenProcessed;
    }

    /// <summary>
    /// Marks a similar event as processed using object ID and event type
    /// </summary>
    /// <param name="stripeEvent">The Stripe event to mark</param>
    public void MarkSimilarEventAsProcessed(Event stripeEvent)
    {
        if (stripeEvent?.Data?.Object == null)
        {
            return;
        }

        var objectId = ExtractObjectId(stripeEvent);
        if (string.IsNullOrEmpty(objectId))
        {
            return;
        }

        var similarEventCacheKey = GenerateSimilarEventCacheKey(stripeEvent.Type, objectId);
        
        _cache.Set(similarEventCacheKey, true, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _defaultCacheExpiry,
            Priority = CacheItemPriority.Normal
        });

        _logger.LogDebug("Marked similar Stripe event for object {ObjectId} of type {EventType} as processed", 
            objectId, stripeEvent.Type);
    }

    /// <summary>
    /// Clears the idempotency cache for a specific event (for testing or manual retry)
    /// </summary>
    /// <param name="stripeEvent">The event to clear from cache</param>
    public void ClearEventFromCache(Event stripeEvent)
    {
        if (stripeEvent == null) return;

        var cacheKey = GenerateEventCacheKey(stripeEvent);
        _cache.Remove(cacheKey);

        var objectId = ExtractObjectId(stripeEvent);
        if (!string.IsNullOrEmpty(objectId))
        {
            var similarEventCacheKey = GenerateSimilarEventCacheKey(stripeEvent.Type, objectId);
            _cache.Remove(similarEventCacheKey);
        }

        _logger.LogInformation("Cleared Stripe event {EventId} from idempotency cache", stripeEvent.Id);
    }

    private string GenerateEventCacheKey(Event stripeEvent)
    {
        return $"stripe_event_{stripeEvent.Id}";
    }

    private string GenerateSimilarEventCacheKey(string eventType, string objectId)
    {
        return $"stripe_similar_{eventType}_{objectId}";
    }

    private string? ExtractObjectId(Event stripeEvent)
    {
        try
        {
            // Handle different Stripe object types
            var obj = stripeEvent.Data.Object;
            
            // Check specific types first
            if (obj is PaymentIntent pi) return pi.Id;
            if (obj is Charge charge) return charge.Id;
            if (obj is Customer customer) return customer.Id;
            if (obj is Invoice invoice) return invoice.Id;
            if (obj is SetupIntent si) return si.Id;
            if (obj is Subscription subscription) return subscription.Id;
            
            // Try to get Id from any object using reflection
            return GetIdFromObject(obj);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract object ID from Stripe event {EventId}", stripeEvent.Id);
            return null;
        }
    }

    private string? GetIdFromObject(object obj)
    {
        try
        {
            // Use reflection to get the Id property if available
            var idProperty = obj.GetType().GetProperty("Id");
            return idProperty?.GetValue(obj)?.ToString();
        }
        catch
        {
            return null;
        }
    }
}