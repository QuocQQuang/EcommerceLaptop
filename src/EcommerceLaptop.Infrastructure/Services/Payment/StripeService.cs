using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using EcommerceLaptop.Core.Services.Payment;
using EcommerceLaptop.Core.DTOs.Payment;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Configuration;
using EcommerceLaptop.Core.Utilities.Payment;
using CorePaymentMethod = EcommerceLaptop.Core.Entities.PaymentMethod;
using StripePaymentIntent = Stripe.PaymentIntent;
using StripePaymentMethod = Stripe.PaymentMethod;

namespace EcommerceLaptop.Infrastructure.Services.Payment;

/// <summary>
/// Stripe payment service implementation using official Stripe .NET SDK
/// Follows modern payment processing with Payment Intents API
/// </summary>
public class StripeService : IStripeService
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripeService> _logger;
    private readonly PaymentIntentService _paymentIntentService;
    private readonly SetupIntentService _setupIntentService;
    private readonly PaymentMethodService _paymentMethodService;
    private readonly StripeWebhookVerificationService _webhookVerificationService;
    // TODO: Add idempotency service via DI later
    // private readonly StripeEventIdempotencyService _idempotencyService;

    public PaymentGateway Gateway => PaymentGateway.Stripe;

    public StripeService(
        IOptions<PaymentGatewaySettings> settings,
        ILogger<StripeService> logger,
        StripeWebhookVerificationService webhookVerificationService)
    // TODO: Add idempotency service parameter later
    // StripeEventIdempotencyService idempotencyService)
    {
        _settings = settings.Value.Stripe;
        _logger = logger;
        _webhookVerificationService = webhookVerificationService;
        // TODO: Assign idempotency service later
        // _idempotencyService = idempotencyService;

        // Initialize Stripe SDK
        StripeConfiguration.ApiKey = _settings.SecretKey;

        // Initialize Stripe services
        _paymentIntentService = new PaymentIntentService();
        _setupIntentService = new SetupIntentService();
        _paymentMethodService = new PaymentMethodService();
    }

    public async Task<PaymentInitializationResult> InitializePaymentAsync(InitializePaymentRequest request)
    {
        try
        {
            _logger.LogInformation("Initializing Stripe payment for order {OrderId}", request.OrderId);

            // Stripe always uses USD - ensure currency is correct
            var amount = request.Amount;
            var currency = request.Currency;

            if (currency != "USD")
            {
                _logger.LogWarning("Stripe received non-USD currency {Currency}, converting to USD", currency);
                amount = PaymentHelpers.ConvertCurrency(amount, currency, "USD");
                currency = "USD";
            }

            var paymentIntentRequest = new CreatePaymentIntentRequest
            {
                Amount = amount,
                Currency = currency.ToLowerInvariant(),
                Description = !string.IsNullOrEmpty(request.Description) ? request.Description : $"Order #{request.OrderId}",
                CustomerEmail = request.CustomerEmail,
                PaymentMethodTypes = new List<string> { "card" },
                ReturnUrl = request.ReturnUrl,
                Metadata = new Dictionary<string, string>
                {
                    { "order_id", request.OrderId.ToString() },
                    { "customer_email", request.CustomerEmail },
                    { "customer_name", request.CustomerName }
                }
            };

            var paymentIntentResult = await CreatePaymentIntentAsync(paymentIntentRequest);

            if (!paymentIntentResult.IsSuccess)
            {
                return new PaymentInitializationResult
                {
                    IsSuccess = false,
                    ErrorMessage = paymentIntentResult.ErrorMessage
                };
            }

            return new PaymentInitializationResult
            {
                IsSuccess = true,
                TransactionId = paymentIntentResult.PaymentIntentId,
                PaymentUrl = string.Empty, // Stripe uses client-side integration with client_secret
                AdditionalData = new Dictionary<string, object>
                {
                    { "client_secret", paymentIntentResult.ClientSecret },
                    { "payment_intent_id", paymentIntentResult.PaymentIntentId },
                    { "publishable_key", _settings.PublishableKey }
                }
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error during payment initialization for order {OrderId}: {Error}",
                request.OrderId, ex.Message);
            return new PaymentInitializationResult
            {
                IsSuccess = false,
                ErrorMessage = $"Payment gateway error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Stripe payment initialization for order {OrderId}", request.OrderId);
            return new PaymentInitializationResult
            {
                IsSuccess = false,
                ErrorMessage = "An unexpected error occurred while initializing payment"
            };
        }
    }

    public async Task<PaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request)
    {
        try
        {
            _logger.LogInformation("Processing Stripe payment for transaction {TransactionId}", request.TransactionId);

            // Get the payment intent
            var paymentIntent = await _paymentIntentService.GetAsync(request.TransactionId);

            if (paymentIntent == null)
            {
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Payment intent not found"
                };
            }

            // Convert Stripe payment status to our PaymentStatus enum
            var paymentStatus = ConvertStripeStatus(paymentIntent.Status);

            return new PaymentResult
            {
                IsSuccess = paymentStatus == PaymentStatus.Completed,
                TransactionId = paymentIntent.Id,
                Status = paymentStatus,
                Amount = paymentIntent.Amount / 100m, // Stripe uses cents
                Currency = paymentIntent.Currency.ToUpperInvariant(),
                ProcessedAt = paymentIntent.Created,
                GatewayResponse = paymentIntent.ToJson(),
                Metadata = new Dictionary<string, object>
                {
                    { "gateway_transaction_id", paymentIntent.Id },
                    { "payment_method", "CreditCard" }
                }
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error during payment processing for transaction {TransactionId}: {Error}",
                request.TransactionId, ex.Message);
            return new PaymentResult
            {
                IsSuccess = false,
                ErrorMessage = $"Payment processing failed: {ex.Message}",
                Status = PaymentStatus.Failed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Stripe payment processing for transaction {TransactionId}", request.TransactionId);
            return new PaymentResult
            {
                IsSuccess = false,
                ErrorMessage = "An unexpected error occurred while processing payment",
                Status = PaymentStatus.Failed
            };
        }
    }

    public async Task<RefundResult> ProcessRefundAsync(ProcessRefundRequest request)
    {
        try
        {
            _logger.LogInformation("Processing Stripe refund for transaction {TransactionId}, amount {Amount}",
                request.OriginalTransactionId, request.RefundAmount);

            var refundService = new Stripe.RefundService();
            var refundOptions = new RefundCreateOptions
            {
                PaymentIntent = request.OriginalTransactionId,
                Amount = (long)(request.RefundAmount * 100), // Convert to cents
                Reason = RefundReasons.RequestedByCustomer,
                Metadata = new Dictionary<string, string>
                {
                    { "original_transaction_id", request.OriginalTransactionId },
                    { "refund_reason", request.Reason ?? "Customer request" }
                }
            };

            var refund = await refundService.CreateAsync(refundOptions);

            return new RefundResult
            {
                IsSuccess = refund.Status == "succeeded",
                RefundId = refund.Id,
                OriginalTransactionId = request.OriginalTransactionId,
                RefundAmount = refund.Amount / 100m,
                Status = refund.Status == "succeeded" ? PaymentStatus.Refunded : PaymentStatus.Processing,
                ProcessedAt = DateTime.UtcNow,
                GatewayResponse = refund.ToJson()
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error during refund processing for transaction {TransactionId}: {Error}",
                request.OriginalTransactionId, ex.Message);
            return new RefundResult
            {
                IsSuccess = false,
                ErrorMessage = $"Refund processing failed: {ex.Message}",
                Status = PaymentStatus.Failed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Stripe refund processing for transaction {TransactionId}", request.OriginalTransactionId);
            return new RefundResult
            {
                IsSuccess = false,
                ErrorMessage = "An unexpected error occurred while processing refund",
                Status = PaymentStatus.Failed
            };
        }
    }

    public async Task<PaymentStatus> GetPaymentStatusAsync(string transactionId)
    {
        try
        {
            var paymentIntent = await _paymentIntentService.GetAsync(transactionId);
            return ConvertStripeStatus(paymentIntent.Status);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error retrieving Stripe payment status for transaction {TransactionId}: {Error}",
                transactionId, ex.Message);
            return PaymentStatus.Failed;
        }
    }

    public async Task<bool> ValidateWebhookAsync(string payload, string signature, IDictionary<string, string> headers)
    {
        return await _webhookVerificationService.VerifyWebhookSignatureAsync(payload, signature, headers);
    }

    public async Task<PaymentWebhookResult> ProcessWebhookAsync(string payload, IDictionary<string, string> headers)
    {
        try
        {
            // First verify the webhook signature using enhanced verification
            var stripeSignature = headers.TryGetValue("Stripe-Signature", out var signature) ? signature : "";
            var verificationResult = await _webhookVerificationService.VerifyAndParseWebhookAsync(
                payload,
                stripeSignature);

            if (verificationResult == null)
            {
                _logger.LogError("Stripe webhook signature verification failed");
                return new PaymentWebhookResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Webhook signature verification failed"
                };
            }

            var stripeEvent = verificationResult;

            // TODO: Implement idempotency check to prevent duplicate processing
            // if (_idempotencyService.HasEventBeenProcessed(stripeEvent))
            // {
            //     _logger.LogInformation("Stripe event {EventId} has already been processed, skipping", stripeEvent.Id);
            //     return new PaymentWebhookResult
            //     {
            //         IsSuccess = true,
            //         Action = "event_already_processed",
            //         Data = new Dictionary<string, object>
            //         {
            //             { "event_type", stripeEvent.Type },
            //             { "event_id", stripeEvent.Id },
            //             { "message", "Event already processed (idempotency check)" }
            //         }
            //     };
            // }

            // TODO: Check for similar events (additional duplicate protection)
            // if (_idempotencyService.HasSimilarEventBeenProcessed(stripeEvent))
            // {
            //     _logger.LogInformation("Similar Stripe event has been processed recently for event {EventId}", stripeEvent.Id);
            //     // Mark this specific event as processed and return success
            //     _idempotencyService.MarkEventAsProcessed(stripeEvent);
            //     return new PaymentWebhookResult
            //     {
            //         IsSuccess = true,
            //         Action = "similar_event_processed",
            //         Data = new Dictionary<string, object>
            //         {
            //             { "event_type", stripeEvent.Type },
            //             { "event_id", stripeEvent.Id },
            //             { "message", "Similar event already processed" }
            //         }
            //     };
            // }

            _logger.LogInformation("Processing Stripe webhook event: {EventType} with ID: {EventId}",
                stripeEvent.Type, stripeEvent.Id);

            // Process the event and mark as processed for idempotency
            PaymentWebhookResult result;
            try
            {
                // Enhanced event handling with comprehensive event types
                result = stripeEvent.Type switch
                {
                    // Payment Intent Events
                    "payment_intent.succeeded" => await HandlePaymentSucceeded(stripeEvent),
                    "payment_intent.payment_failed" => await HandlePaymentFailed(stripeEvent),
                    "payment_intent.canceled" => await HandlePaymentCanceled(stripeEvent),
                    "payment_intent.requires_action" => await HandlePaymentRequiresAction(stripeEvent),
                    "payment_intent.processing" => await HandlePaymentProcessing(stripeEvent),
                    "payment_intent.created" => await HandlePaymentCreated(stripeEvent),

                    // Charge Events
                    "charge.succeeded" => await HandleChargeSucceeded(stripeEvent),
                    "charge.failed" => await HandleChargeFailed(stripeEvent),
                    "charge.dispute.created" => await HandleDispute(stripeEvent),
                    "charge.captured" => await HandleChargeCaptured(stripeEvent),

                    // Payment Method Events
                    "payment_method.attached" => await HandlePaymentMethodAttached(stripeEvent),
                    "payment_method.detached" => await HandlePaymentMethodDetached(stripeEvent),

                    // Customer Events
                    "customer.created" => await HandleCustomerCreated(stripeEvent),
                    "customer.updated" => await HandleCustomerUpdated(stripeEvent),
                    "customer.deleted" => await HandleCustomerDeleted(stripeEvent),

                    // Invoice Events (for subscriptions)
                    "invoice.created" => await HandleInvoiceCreated(stripeEvent),
                    "invoice.payment_succeeded" => await HandleInvoicePaymentSucceeded(stripeEvent),
                    "invoice.payment_failed" => await HandleInvoicePaymentFailed(stripeEvent),

                    // Setup Intent Events
                    "setup_intent.succeeded" => await HandleSetupIntentSucceeded(stripeEvent),
                    "setup_intent.setup_failed" => await HandleSetupIntentFailed(stripeEvent),

                    // Checkout Events
                    "checkout.session.completed" => await HandleCheckoutSessionCompleted(stripeEvent),
                    "checkout.session.expired" => await HandleCheckoutSessionExpired(stripeEvent),

                    // Refund Events
                    "charge.refunded" => await HandleChargeRefunded(stripeEvent),

                    // Account Events
                    "account.updated" => await HandleAccountUpdated(stripeEvent),

                    // Default case for unhandled events
                    _ => await HandleUnknownEvent(stripeEvent)
                };

                // Mark event as processed for idempotency after successful processing
                if (result.IsSuccess)
                {
                    // TODO: Mark event as processed for idempotency
                    // _idempotencyService.MarkEventAsProcessed(stripeEvent);
                    // _idempotencyService.MarkSimilarEventAsProcessed(stripeEvent);

                    _logger.LogInformation("Successfully processed Stripe webhook event {EventId} of type {EventType}",
                        stripeEvent.Id, stripeEvent.Type);
                }

                return result;
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Error processing Stripe webhook event {EventId}", stripeEvent.Id);
                throw; // Re-throw to be caught by outer catch blocks
            }
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook error: {Error}", ex.Message);
            return new PaymentWebhookResult
            {
                IsSuccess = false,
                ErrorMessage = $"Stripe webhook error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing Stripe webhook");
            return new PaymentWebhookResult
            {
                IsSuccess = false,
                ErrorMessage = "An unexpected error occurred while processing webhook"
            };
        }
    }

    public bool SupportsPaymentMethod(CorePaymentMethod method)
    {
        return PaymentHelpers.IsPaymentMethodSupported(method, PaymentGateway.Stripe);
    }

    public IReadOnlyList<string> GetSupportedCurrencies()
    {
        return _settings.SupportedCurrencies.AsReadOnly();
    }

    // Stripe-specific methods implementation

    public async Task<StripePaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentIntentRequest request)
    {
        try
        {
            // Get currency multiplier for smallest unit conversion
            var currencyMultiplier = GetCurrencyMultiplier(request.Currency);

            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(request.Amount * currencyMultiplier), // Convert to smallest currency unit
                Currency = request.Currency.ToLowerInvariant(),
                Description = request.Description,
                PaymentMethodTypes = request.PaymentMethodTypes,
                ConfirmationMethod = request.ConfirmationMethod ? "automatic" : "manual",
                Metadata = request.Metadata
            };

            _logger.LogInformation("Creating Stripe Payment Intent: Amount={Amount}, Currency={Currency}, SmallestUnitAmount={SmallestUnitAmount}",
                request.Amount, request.Currency, options.Amount);

            if (!string.IsNullOrEmpty(request.CustomerId))
            {
                options.Customer = request.CustomerId;
            }

            if (!string.IsNullOrEmpty(request.PaymentMethodId))
            {
                options.PaymentMethod = request.PaymentMethodId;
                options.Confirm = true;
            }

            var paymentIntent = await _paymentIntentService.CreateAsync(options);

            return new StripePaymentIntentResult
            {
                IsSuccess = true,
                PaymentIntentId = paymentIntent.Id,
                ClientSecret = paymentIntent.ClientSecret,
                Status = paymentIntent.Status,
                Amount = paymentIntent.Amount / currencyMultiplier,
                Currency = paymentIntent.Currency.ToUpperInvariant()
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error creating Stripe Payment Intent: {Error}", ex.Message);
            return new StripePaymentIntentResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<PaymentConfirmationResult> ConfirmPaymentIntentAsync(string paymentIntentId)
    {
        try
        {
            var paymentIntent = await _paymentIntentService.ConfirmAsync(paymentIntentId);

            return new PaymentConfirmationResult
            {
                IsSuccess = true,
                PaymentIntentId = paymentIntent.Id,
                Status = paymentIntent.Status,
                Amount = paymentIntent.Amount / 100m,
                Currency = paymentIntent.Currency.ToUpperInvariant(),
                ConfirmedAt = DateTime.UtcNow
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error confirming Stripe Payment Intent {PaymentIntentId}: {Error}",
                paymentIntentId, ex.Message);
            return new PaymentConfirmationResult
            {
                IsSuccess = false,
                PaymentIntentId = paymentIntentId,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<StripeSetupIntentResult> CreateSetupIntentAsync(string customerId, List<string> paymentMethodTypes)
    {
        try
        {
            var options = new SetupIntentCreateOptions
            {
                Customer = customerId,
                PaymentMethodTypes = paymentMethodTypes,
                Usage = "off_session"
            };

            var setupIntent = await _setupIntentService.CreateAsync(options);

            return new StripeSetupIntentResult
            {
                IsSuccess = true,
                SetupIntentId = setupIntent.Id,
                ClientSecret = setupIntent.ClientSecret,
                Status = setupIntent.Status,
                CustomerId = customerId
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error creating Stripe Setup Intent for customer {CustomerId}: {Error}",
                customerId, ex.Message);
            return new StripeSetupIntentResult
            {
                IsSuccess = false,
                CustomerId = customerId,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<PaymentMethodResult> AttachPaymentMethodAsync(string customerId, string paymentMethodId)
    {
        try
        {
            var options = new PaymentMethodAttachOptions
            {
                Customer = customerId
            };

            var paymentMethod = await _paymentMethodService.AttachAsync(paymentMethodId, options);

            return new PaymentMethodResult
            {
                IsSuccess = true,
                PaymentMethodId = paymentMethod.Id,
                Type = paymentMethod.Type,
                CustomerId = customerId,
                IsAttached = paymentMethod.Customer?.Id == customerId
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error attaching payment method {PaymentMethodId} to customer {CustomerId}: {Error}",
                paymentMethodId, customerId, ex.Message);
            return new PaymentMethodResult
            {
                IsSuccess = false,
                PaymentMethodId = paymentMethodId,
                CustomerId = customerId,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<List<string>> GetSupportedPaymentMethodsAsync()
    {
        await Task.CompletedTask; // For async consistency
        return _settings.SupportedPaymentMethods.ToList();
    }

    #region Private Helper Methods

    private static PaymentStatus ConvertStripeStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "requires_payment_method" => PaymentStatus.Pending,
            "requires_confirmation" => PaymentStatus.Pending,
            "requires_action" => PaymentStatus.Processing,
            "processing" => PaymentStatus.Processing,
            "succeeded" => PaymentStatus.Completed,
            "canceled" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Failed
        };
    }

    private Task<PaymentWebhookResult> HandlePaymentSucceeded(Event stripeEvent)
    {
        var paymentIntent = (StripePaymentIntent)stripeEvent.Data.Object;

        _logger.LogInformation("Payment succeeded for Payment Intent: {PaymentIntentId}", paymentIntent.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = paymentIntent.Id,
            Status = PaymentStatus.Completed,
            Action = "payment",
            Data = new Dictionary<string, object>
            {
                { "event_type", "payment.succeeded" },
                { "amount", paymentIntent.Amount / 100m },
                { "currency", paymentIntent.Currency.ToUpperInvariant() },
                { "message", "Payment completed successfully" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandlePaymentFailed(Event stripeEvent)
    {
        var paymentIntent = (StripePaymentIntent)stripeEvent.Data.Object;

        _logger.LogWarning("Payment failed for Payment Intent: {PaymentIntentId}", paymentIntent.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = paymentIntent.Id,
            Status = PaymentStatus.Failed,
            Action = "payment",
            Data = new Dictionary<string, object>
            {
                { "event_type", "payment.failed" },
                { "amount", paymentIntent.Amount / 100m },
                { "currency", paymentIntent.Currency.ToUpperInvariant() },
                { "message", "Payment failed" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandlePaymentCanceled(Event stripeEvent)
    {
        var paymentIntent = (StripePaymentIntent)stripeEvent.Data.Object;

        _logger.LogInformation("Payment canceled for Payment Intent: {PaymentIntentId}", paymentIntent.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = paymentIntent.Id,
            Status = PaymentStatus.Cancelled,
            Action = "payment",
            Data = new Dictionary<string, object>
            {
                { "event_type", "payment.canceled" },
                { "amount", paymentIntent.Amount / 100m },
                { "currency", paymentIntent.Currency.ToUpperInvariant() },
                { "message", "Payment was canceled" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandleDispute(Event stripeEvent)
    {
        var dispute = (Dispute)stripeEvent.Data.Object;

        _logger.LogWarning("Dispute created for charge: {ChargeId}, reason: {Reason}",
            dispute.ChargeId, dispute.Reason);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = dispute.ChargeId,
            Status = PaymentStatus.Failed,
            Action = "chargeback",
            Data = new Dictionary<string, object>
            {
                { "event_type", "dispute.created" },
                { "amount", dispute.Amount / 100m },
                { "currency", dispute.Currency.ToUpperInvariant() },
                { "message", $"Dispute created: {dispute.Reason}" }
            }
        });
    }

    // Enhanced event handlers for comprehensive Stripe webhook support
    private Task<PaymentWebhookResult> HandlePaymentRequiresAction(Event stripeEvent)
    {
        var paymentIntent = (StripePaymentIntent)stripeEvent.Data.Object;

        _logger.LogInformation("Payment requires action for Payment Intent: {PaymentIntentId}", paymentIntent.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = paymentIntent.Id,
            Status = PaymentStatus.Processing,
            Action = "payment",
            Data = new Dictionary<string, object>
            {
                { "event_type", "payment.requires_action" },
                { "amount", paymentIntent.Amount / 100m },
                { "currency", paymentIntent.Currency.ToUpperInvariant() },
                { "message", "Payment requires customer action (e.g., 3D Secure)" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandlePaymentProcessing(Event stripeEvent)
    {
        var paymentIntent = (StripePaymentIntent)stripeEvent.Data.Object;

        _logger.LogInformation("Payment processing for Payment Intent: {PaymentIntentId}", paymentIntent.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = paymentIntent.Id,
            Status = PaymentStatus.Processing,
            Action = "payment",
            Data = new Dictionary<string, object>
            {
                { "event_type", "payment.processing" },
                { "amount", paymentIntent.Amount / 100m },
                { "currency", paymentIntent.Currency.ToUpperInvariant() },
                { "message", "Payment is being processed" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandlePaymentCreated(Event stripeEvent)
    {
        var paymentIntent = (StripePaymentIntent)stripeEvent.Data.Object;

        _logger.LogInformation("Payment Intent created: {PaymentIntentId}", paymentIntent.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = paymentIntent.Id,
            Status = PaymentStatus.Pending,
            Action = "payment",
            Data = new Dictionary<string, object>
            {
                { "event_type", "payment.created" },
                { "amount", paymentIntent.Amount / 100m },
                { "currency", paymentIntent.Currency.ToUpperInvariant() },
                { "message", "Payment Intent created" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandleChargeSucceeded(Event stripeEvent)
    {
        var charge = (Charge)stripeEvent.Data.Object;

        _logger.LogInformation("Charge succeeded: {ChargeId}", charge.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = charge.Id,
            Status = PaymentStatus.Completed,
            Action = "charge",
            Data = new Dictionary<string, object>
            {
                { "event_type", "charge.succeeded" },
                { "amount", charge.Amount / 100m },
                { "currency", charge.Currency.ToUpperInvariant() },
                { "message", "Charge succeeded" },
                { "payment_intent_id", charge.PaymentIntentId ?? string.Empty }
            }
        });
    }

    private Task<PaymentWebhookResult> HandleChargeFailed(Event stripeEvent)
    {
        var charge = (Charge)stripeEvent.Data.Object;

        _logger.LogWarning("Charge failed: {ChargeId}", charge.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = charge.Id,
            Status = PaymentStatus.Failed,
            Action = "charge",
            Data = new Dictionary<string, object>
            {
                { "event_type", "charge.failed" },
                { "amount", charge.Amount / 100m },
                { "currency", charge.Currency.ToUpperInvariant() },
                { "message", "Charge failed" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandleChargeCaptured(Event stripeEvent)
    {
        var charge = (Charge)stripeEvent.Data.Object;

        _logger.LogInformation("Charge captured: {ChargeId}", charge.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = charge.Id,
            Status = PaymentStatus.Completed,
            Action = "charge",
            Data = new Dictionary<string, object>
            {
                { "event_type", "charge.captured" },
                { "amount", charge.Amount / 100m },
                { "currency", charge.Currency.ToUpperInvariant() },
                { "message", "Charge captured" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandleChargeRefunded(Event stripeEvent)
    {
        var charge = (Charge)stripeEvent.Data.Object;

        _logger.LogInformation("Charge refunded: {ChargeId}", charge.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = charge.Id,
            Status = PaymentStatus.Refunded,
            Action = "refund",
            Data = new Dictionary<string, object>
            {
                { "event_type", "charge.refunded" },
                { "amount", charge.Amount / 100m },
                { "currency", charge.Currency.ToUpperInvariant() },
                { "message", "Charge refunded" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandlePaymentMethodAttached(Event stripeEvent)
    {
        var paymentMethod = (StripePaymentMethod)stripeEvent.Data.Object;

        _logger.LogInformation("Payment method attached: {PaymentMethodId} to customer: {CustomerId}",
            paymentMethod.Id, paymentMethod.CustomerId);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = paymentMethod.Id,
            Status = PaymentStatus.Completed,
            Action = "payment_method",
            Data = new Dictionary<string, object>
            {
                { "event_type", "payment_method.attached" },
                { "payment_method_id", paymentMethod.Id },
                { "customer_id", paymentMethod.CustomerId ?? "" },
                { "message", "Payment method attached to customer" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandlePaymentMethodDetached(Event stripeEvent)
    {
        var paymentMethod = (StripePaymentMethod)stripeEvent.Data.Object;

        _logger.LogInformation("Payment method detached: {PaymentMethodId}", paymentMethod.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = paymentMethod.Id,
            Status = PaymentStatus.Completed,
            Action = "payment_method",
            Data = new Dictionary<string, object>
            {
                { "event_type", "payment_method.detached" },
                { "payment_method_id", paymentMethod.Id },
                { "message", "Payment method detached from customer" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandleCustomerCreated(Event stripeEvent)
    {
        var customer = (Customer)stripeEvent.Data.Object;

        _logger.LogInformation("Customer created: {CustomerId}", customer.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = customer.Id,
            Status = PaymentStatus.Completed,
            Action = "customer",
            Data = new Dictionary<string, object>
            {
                { "event_type", "customer.created" },
                { "customer_id", customer.Id },
                { "email", customer.Email ?? "" },
                { "message", "Customer created" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandleCustomerUpdated(Event stripeEvent)
    {
        var customer = (Customer)stripeEvent.Data.Object;

        _logger.LogInformation("Customer updated: {CustomerId}", customer.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = customer.Id,
            Status = PaymentStatus.Completed,
            Action = "customer",
            Data = new Dictionary<string, object>
            {
                { "event_type", "customer.updated" },
                { "customer_id", customer.Id },
                { "email", customer.Email ?? "" },
                { "message", "Customer updated" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandleCustomerDeleted(Event stripeEvent)
    {
        var customer = (Customer)stripeEvent.Data.Object;

        _logger.LogInformation("Customer deleted: {CustomerId}", customer.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = customer.Id,
            Status = PaymentStatus.Completed,
            Action = "customer",
            Data = new Dictionary<string, object>
            {
                { "event_type", "customer.deleted" },
                { "customer_id", customer.Id },
                { "message", "Customer deleted" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandleInvoiceCreated(Event stripeEvent)
    {
        var invoice = (Invoice)stripeEvent.Data.Object;

        _logger.LogInformation("Invoice created: {InvoiceId}", invoice.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = invoice.Id,
            Status = PaymentStatus.Pending,
            Action = "invoice",
            Data = new Dictionary<string, object>
            {
                { "event_type", "invoice.created" },
                { "invoice_id", invoice.Id },
                { "amount", invoice.AmountDue / 100m },
                { "currency", invoice.Currency.ToUpperInvariant() },
                { "message", "Invoice created" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandleInvoicePaymentSucceeded(Event stripeEvent)
    {
        var invoice = (Invoice)stripeEvent.Data.Object;

        _logger.LogInformation("Invoice payment succeeded: {InvoiceId}", invoice.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = invoice.Id,
            Status = PaymentStatus.Completed,
            Action = "invoice",
            Data = new Dictionary<string, object>
            {
                { "event_type", "invoice.payment_succeeded" },
                { "invoice_id", invoice.Id },
                { "amount", invoice.AmountPaid / 100m },
                { "currency", invoice.Currency.ToUpperInvariant() },
                { "message", "Invoice payment succeeded" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandleInvoicePaymentFailed(Event stripeEvent)
    {
        var invoice = (Invoice)stripeEvent.Data.Object;

        _logger.LogWarning("Invoice payment failed: {InvoiceId}", invoice.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = invoice.Id,
            Status = PaymentStatus.Failed,
            Action = "invoice",
            Data = new Dictionary<string, object>
            {
                { "event_type", "invoice.payment_failed" },
                { "invoice_id", invoice.Id },
                { "amount", invoice.AmountDue / 100m },
                { "currency", invoice.Currency.ToUpperInvariant() },
                { "message", "Invoice payment failed" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandleSetupIntentSucceeded(Event stripeEvent)
    {
        var setupIntent = (SetupIntent)stripeEvent.Data.Object;

        _logger.LogInformation("Setup Intent succeeded: {SetupIntentId}", setupIntent.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = setupIntent.Id,
            Status = PaymentStatus.Completed,
            Action = "setup_intent",
            Data = new Dictionary<string, object>
            {
                { "event_type", "setup_intent.succeeded" },
                { "setup_intent_id", setupIntent.Id },
                { "customer_id", setupIntent.CustomerId ?? "" },
                { "message", "Setup Intent succeeded" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandleSetupIntentFailed(Event stripeEvent)
    {
        var setupIntent = (SetupIntent)stripeEvent.Data.Object;

        _logger.LogWarning("Setup Intent failed: {SetupIntentId}", setupIntent.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = setupIntent.Id,
            Status = PaymentStatus.Failed,
            Action = "setup_intent",
            Data = new Dictionary<string, object>
            {
                { "event_type", "setup_intent.setup_failed" },
                { "setup_intent_id", setupIntent.Id },
                { "customer_id", setupIntent.CustomerId ?? "" },
                { "message", "Setup Intent failed" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandleCheckoutSessionCompleted(Event stripeEvent)
    {
        var session = (Session)stripeEvent.Data.Object;

        _logger.LogInformation("Checkout session completed: {SessionId}", session.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = session.Id,
            Status = PaymentStatus.Completed,
            Action = "checkout",
            Data = new Dictionary<string, object>
            {
                { "event_type", "checkout.session.completed" },
                { "session_id", session.Id },
                { "customer_id", session.CustomerId ?? "" },
                { "amount", session.AmountTotal ?? 0 / 100m },
                { "currency", session.Currency?.ToUpperInvariant() ?? "" },
                { "message", "Checkout session completed" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandleCheckoutSessionExpired(Event stripeEvent)
    {
        var session = (Session)stripeEvent.Data.Object;

        _logger.LogInformation("Checkout session expired: {SessionId}", session.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = session.Id,
            Status = PaymentStatus.Cancelled,
            Action = "checkout",
            Data = new Dictionary<string, object>
            {
                { "event_type", "checkout.session.expired" },
                { "session_id", session.Id },
                { "message", "Checkout session expired" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandleAccountUpdated(Event stripeEvent)
    {
        var account = (Account)stripeEvent.Data.Object;

        _logger.LogInformation("Account updated: {AccountId}", account.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            TransactionId = account.Id,
            Status = PaymentStatus.Completed,
            Action = "account",
            Data = new Dictionary<string, object>
            {
                { "event_type", "account.updated" },
                { "account_id", account.Id },
                { "message", "Account updated" }
            }
        });
    }

    private Task<PaymentWebhookResult> HandleUnknownEvent(Event stripeEvent)
    {
        _logger.LogInformation("Received unhandled Stripe webhook event: {EventType} with ID: {EventId}",
            stripeEvent.Type, stripeEvent.Id);

        return Task.FromResult(new PaymentWebhookResult
        {
            IsSuccess = true,
            Action = "event_received",
            Data = new Dictionary<string, object>
            {
                { "event_type", stripeEvent.Type },
                { "event_id", stripeEvent.Id },
                { "message", "Event received but not processed" }
            }
        });
    }

    /// <summary>
    /// Gets the currency multiplier for converting to smallest currency unit
    /// Reference: https://stripe.com/docs/currencies#zero-decimal
    /// </summary>
    /// <param name="currency">Currency code (e.g., USD, VND, JPY)</param>
    /// <returns>Multiplier to convert to smallest unit</returns>
    private static decimal GetCurrencyMultiplier(string currency)
    {
        // Zero-decimal currencies (already in smallest unit)
        var zeroDecimalCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "BIF", "CLP", "DJF", "GNF", "JPY", "KMF", "KRW", "MGA", "PYG", "RWF",
            "UGX", "VND", "VUV", "XAF", "XOF", "XPF"
        };

        return zeroDecimalCurrencies.Contains(currency) ? 1 : 100;
    }

    #endregion
}