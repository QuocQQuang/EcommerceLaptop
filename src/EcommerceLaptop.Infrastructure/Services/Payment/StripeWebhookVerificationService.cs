using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Stripe;
using EcommerceLaptop.Core.Configuration;

namespace EcommerceLaptop.Infrastructure.Services.Payment;

/// <summary>
/// Service for verifying Stripe webhook signatures
/// Implements secure webhook validation according to Stripe documentation
/// </summary>
public class StripeWebhookVerificationService
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripeWebhookVerificationService> _logger;

    public StripeWebhookVerificationService(
        IOptions<PaymentGatewaySettings> settings,
        ILogger<StripeWebhookVerificationService> logger)
    {
        _settings = settings.Value.Stripe;
        _logger = logger;
    }

    /// <summary>
    /// Verifies the Stripe webhook signature using the official Stripe .NET SDK
    /// Includes timestamp validation to prevent replay attacks
    /// </summary>
    /// <param name="payload">Raw webhook payload</param>
    /// <param name="signature">Stripe-Signature header value</param>
    /// <param name="headers">All HTTP headers from the webhook request</param>
    /// <returns>True if signature is valid</returns>
    public Task<bool> VerifyWebhookSignatureAsync(string payload, string signature, IDictionary<string, string> headers)
    {
        try
        {
            if (string.IsNullOrEmpty(payload))
            {
                _logger.LogWarning("Webhook payload is empty");
                return Task.FromResult(false);
            }

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Stripe-Signature header is missing");
                return Task.FromResult(false);
            }

            if (string.IsNullOrEmpty(_settings.WebhookSecret))
            {
                _logger.LogError("Stripe webhook secret is not configured");
                return Task.FromResult(false);
            }

            // Use Stripe's built-in webhook signature verification with tolerance
            // Tolerance of 300 seconds (5 minutes) to prevent replay attacks
            var stripeEvent = EventUtility.ConstructEvent(
                payload,
                signature,
                _settings.WebhookSecret,
                tolerance: 300, // 5 minutes tolerance as recommended by Stripe
                throwOnApiVersionMismatch: false);

            _logger.LogInformation("Stripe webhook signature verified successfully for event: {EventType} with ID: {EventId}", 
                stripeEvent.Type, stripeEvent.Id);

            return Task.FromResult(true);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook signature verification failed: {Error}", ex.Message);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Stripe webhook signature verification");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Verifies webhook signature and parses the event with enhanced security
    /// Includes timestamp validation and comprehensive error handling
    /// </summary>
    /// <param name="payload">Raw webhook payload</param>
    /// <param name="signature">Stripe-Signature header value</param>
    /// <returns>Parsed Stripe event if valid, null otherwise</returns>
    public Task<Event?> VerifyAndParseWebhookAsync(string payload, string signature)
    {
        try
        {
            if (string.IsNullOrEmpty(payload))
            {
                _logger.LogWarning("Webhook payload is empty for verification");
                return Task.FromResult<Event?>(null);
            }

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Stripe-Signature header is missing for verification");
                return Task.FromResult<Event?>(null);
            }

            if (string.IsNullOrEmpty(_settings.WebhookSecret) || _settings.WebhookSecret == "whsec_your_stripe_webhook_secret_here")
            {
                _logger.LogWarning("Stripe webhook secret is not configured properly - skipping signature verification for development");
                // For development: parse event without signature verification
                try
                {
                    var eventData = Event.FromJson(payload);
                    return Task.FromResult<Event?>(eventData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse Stripe webhook payload without signature verification");
                    return Task.FromResult<Event?>(null);
                }
            }

            // Enhanced verification with timestamp validation
            var stripeEvent = EventUtility.ConstructEvent(
                payload,
                signature,
                _settings.WebhookSecret,
                tolerance: 300, // 5 minutes tolerance to prevent replay attacks
                throwOnApiVersionMismatch: false);

            _logger.LogInformation("Successfully parsed Stripe webhook event: {EventType} with ID: {EventId} at {Timestamp}", 
                stripeEvent.Type, stripeEvent.Id, stripeEvent.Created);

            return Task.FromResult<Event?>(stripeEvent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to verify and parse Stripe webhook: {Error}", ex.Message);
            return Task.FromResult<Event?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Stripe webhook verification and parsing");
            return Task.FromResult<Event?>(null);
        }
    }

    /// <summary>
    /// Validates that the webhook event is not too old (tolerance check)
    /// </summary>
    /// <param name="stripeEvent">Parsed Stripe event</param>
    /// <param name="toleranceInMinutes">Maximum age of the event in minutes (default: 5 minutes)</param>
    /// <returns>True if event is within tolerance</returns>
    public bool IsEventWithinTolerance(Event stripeEvent, int toleranceInMinutes = 5)
    {
        try
        {
            var eventAge = DateTime.UtcNow - stripeEvent.Created;
            var isWithinTolerance = eventAge.TotalMinutes <= toleranceInMinutes;

            if (!isWithinTolerance)
            {
                _logger.LogWarning("Stripe webhook event {EventId} is too old. Age: {Age} minutes, Tolerance: {Tolerance} minutes", 
                    stripeEvent.Id, eventAge.TotalMinutes, toleranceInMinutes);
            }

            return isWithinTolerance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking event tolerance for Stripe webhook event {EventId}", stripeEvent.Id);
            return false;
        }
    }

    /// <summary>
    /// Validates that the event has the expected API version
    /// </summary>
    /// <param name="stripeEvent">Parsed Stripe event</param>
    /// <returns>True if API version matches configuration</returns>
    public bool IsEventApiVersionValid(Event stripeEvent)
    {
        try
        {
            var expectedVersion = _settings.ApiVersion;
            var eventVersion = stripeEvent.ApiVersion;

            var isValid = string.Equals(eventVersion, expectedVersion, StringComparison.OrdinalIgnoreCase);

            if (!isValid)
            {
                _logger.LogWarning("Stripe webhook event {EventId} has API version {EventVersion}, expected {ExpectedVersion}", 
                    stripeEvent.Id, eventVersion, expectedVersion);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking API version for Stripe webhook event {EventId}", stripeEvent.Id);
            return false; // Fail safe
        }
    }

    /// <summary>
    /// Comprehensive webhook validation including signature, tolerance, and API version checks
    /// </summary>
    /// <param name="payload">Raw webhook payload</param>
    /// <param name="signature">Stripe-Signature header value</param>
    /// <param name="toleranceInMinutes">Maximum age of the event in minutes</param>
    /// <returns>Validated Stripe event if all checks pass, null otherwise</returns>
    public async Task<Event?> ValidateWebhookAsync(string payload, string signature, int toleranceInMinutes = 5)
    {
        try
        {
            // Step 1: Verify signature and parse event
            var stripeEvent = await VerifyAndParseWebhookAsync(payload, signature);
            if (stripeEvent == null)
            {
                return null;
            }

            // Step 2: Check event tolerance (replay attack protection)
            if (!IsEventWithinTolerance(stripeEvent, toleranceInMinutes))
            {
                _logger.LogWarning("Stripe webhook event {EventId} failed tolerance check", stripeEvent.Id);
                return null;
            }

            // Step 3: Check API version compatibility
            if (!IsEventApiVersionValid(stripeEvent))
            {
                _logger.LogWarning("Stripe webhook event {EventId} failed API version check", stripeEvent.Id);
                // Note: We might still want to process events with different API versions
                // depending on business requirements, so this is just a warning for now
            }

            _logger.LogInformation("Stripe webhook event {EventId} passed all validation checks", stripeEvent.Id);
            return stripeEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during comprehensive Stripe webhook validation");
            return null;
        }
    }
}