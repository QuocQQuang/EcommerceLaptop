using System.Text.Json.Serialization;

namespace EcommerceLaptop.Infrastructure.Services.Payment
{

/// <summary>
/// PayPal webhook event structure according to PayPal official documentation
/// Reference: https://developer.paypal.com/api/rest/webhooks/event-names/
/// </summary>
public class PayPalWebhookEvent
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("event_version")]
    public string? EventVersion { get; set; }

    [JsonPropertyName("create_time")]
    public DateTime? CreateTime { get; set; }

    [JsonPropertyName("resource_type")]
    public string? ResourceType { get; set; }

    [JsonPropertyName("resource_version")]
    public string? ResourceVersion { get; set; }

    [JsonPropertyName("event_type")]
    public string? EventType { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("resource")]
    public PayPalWebhookResource? Resource { get; set; }

    [JsonPropertyName("links")]
    public List<PayPalLink>? Links { get; set; }
}

/// <summary>
/// PayPal webhook resource data
/// This can represent different types: capture, order, subscription, dispute, etc.
/// </summary>
public class PayPalWebhookResource
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("custom_id")]
    public string? CustomId { get; set; }

    [JsonPropertyName("invoice_id")]
    public string? InvoiceId { get; set; }

    [JsonPropertyName("intent")]
    public string? Intent { get; set; }

    [JsonPropertyName("amount")]
    public PayPalAmount? Amount { get; set; }

    [JsonPropertyName("seller_protection")]
    public PayPalSellerProtection? SellerProtection { get; set; }

    [JsonPropertyName("final_capture")]
    public bool? FinalCapture { get; set; }

    [JsonPropertyName("create_time")]
    public DateTime? CreateTime { get; set; }

    [JsonPropertyName("update_time")]
    public DateTime? UpdateTime { get; set; }

    [JsonPropertyName("status_details")]
    public PayPalStatusDetails? StatusDetails { get; set; }

    // Dispute-specific fields
    [JsonPropertyName("dispute_id")]
    public string? DisputeId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("dispute_amount")]
    public PayPalAmount? DisputeAmount { get; set; }

    [JsonPropertyName("dispute_outcome")]
    public PayPalDisputeOutcome? DisputeOutcome { get; set; }

    // Subscription-specific fields (for future use)
    [JsonPropertyName("plan_id")]
    public string? PlanId { get; set; }

    [JsonPropertyName("start_time")]
    public DateTime? StartTime { get; set; }

    [JsonPropertyName("billing_info")]
    public PayPalBillingInfo? BillingInfo { get; set; }

    // Payer information
    [JsonPropertyName("payer")]
    public PayPalPayer? Payer { get; set; }

    // Purchase units for orders
    [JsonPropertyName("purchase_units")]
    public List<PayPalPurchaseUnit>? PurchaseUnits { get; set; }
}

/// <summary>
/// PayPal amount structure
/// </summary>
public class PayPalAmount
{
    [JsonPropertyName("currency_code")]
    public string? CurrencyCode { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

/// <summary>
/// PayPal seller protection information
/// </summary>
public class PayPalSellerProtection
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("dispute_categories")]
    public List<string>? DisputeCategories { get; set; }
}

/// <summary>
/// PayPal status details for captures/payments
/// </summary>
public class PayPalStatusDetails
{
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

/// <summary>
/// PayPal dispute outcome information
/// </summary>
public class PayPalDisputeOutcome
{
    [JsonPropertyName("outcome_code")]
    public string? OutcomeCode { get; set; }

    [JsonPropertyName("amount_refunded")]
    public PayPalAmount? AmountRefunded { get; set; }
}

/// <summary>
/// PayPal billing information for subscriptions
/// </summary>
public class PayPalBillingInfo
{
    [JsonPropertyName("outstanding_balance")]
    public PayPalAmount? OutstandingBalance { get; set; }

    [JsonPropertyName("cycle_executions")]
    public List<PayPalCycleExecution>? CycleExecutions { get; set; }
}

/// <summary>
/// PayPal cycle execution for subscription billing
/// </summary>
public class PayPalCycleExecution
{
    [JsonPropertyName("tenure_type")]
    public string? TenureType { get; set; }

    [JsonPropertyName("sequence")]
    public int? Sequence { get; set; }

    [JsonPropertyName("cycles_completed")]
    public int? CyclesCompleted { get; set; }
}

/// <summary>
/// PayPal payer information
/// </summary>
public class PayPalPayer
{
    [JsonPropertyName("payer_id")]
    public string? PayerId { get; set; }

    [JsonPropertyName("email_address")]
    public string? EmailAddress { get; set; }

    [JsonPropertyName("name")]
    public PayPalName? Name { get; set; }

    [JsonPropertyName("address")]
    public PayPalAddress? Address { get; set; }
}

/// <summary>
/// PayPal name structure
/// </summary>
public class PayPalName
{
    [JsonPropertyName("given_name")]
    public string? GivenName { get; set; }

    [JsonPropertyName("surname")]
    public string? Surname { get; set; }
}

/// <summary>
/// PayPal address structure
/// </summary>
public class PayPalAddress
{
    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }
}

/// <summary>
/// PayPal purchase unit for orders
/// </summary>
public class PayPalPurchaseUnit
{
    [JsonPropertyName("reference_id")]
    public string? ReferenceId { get; set; }

    [JsonPropertyName("amount")]
    public PayPalAmount? Amount { get; set; }

    [JsonPropertyName("payee")]
    public PayPalPayee? Payee { get; set; }

    [JsonPropertyName("payments")]
    public PayPalPayments? Payments { get; set; }
}

/// <summary>
/// PayPal payee information
/// </summary>
public class PayPalPayee
{
    [JsonPropertyName("email_address")]
    public string? EmailAddress { get; set; }

    [JsonPropertyName("merchant_id")]
    public string? MerchantId { get; set; }
}

/// <summary>
/// PayPal payments information within purchase unit
/// </summary>
public class PayPalPayments
{
    [JsonPropertyName("captures")]
    public List<PayPalCapture>? Captures { get; set; }

    [JsonPropertyName("refunds")]
    public List<PayPalRefund>? Refunds { get; set; }
}

/// <summary>
/// PayPal capture information
/// </summary>
public class PayPalCapture
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("amount")]
    public PayPalAmount? Amount { get; set; }

    [JsonPropertyName("final_capture")]
    public bool? FinalCapture { get; set; }

    [JsonPropertyName("create_time")]
    public DateTime? CreateTime { get; set; }

    [JsonPropertyName("update_time")]
    public DateTime? UpdateTime { get; set; }
}

/// <summary>
/// PayPal refund information
/// </summary>
public class PayPalRefund
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("amount")]
    public PayPalAmount? Amount { get; set; }

    [JsonPropertyName("create_time")]
    public DateTime? CreateTime { get; set; }

    [JsonPropertyName("update_time")]
    public DateTime? UpdateTime { get; set; }
}

/// <summary>
/// PayPal link structure for HATEOAS
/// </summary>
public class PayPalLink
{
    [JsonPropertyName("href")]
    public string? Href { get; set; }

    [JsonPropertyName("rel")]
    public string? Rel { get; set; }

    [JsonPropertyName("method")]
    public string? Method { get; set; }
}
}