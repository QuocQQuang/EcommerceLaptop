namespace EcommerceLaptop.Core.Entities;

/// <summary>
/// Payment audit trail entity for regulatory compliance and security monitoring
/// Following PCI DSS requirements for payment transaction logging
/// </summary>
public class PaymentAudit
{
    public int Id { get; set; }
    
    /// <summary>
    /// Related payment identifier
    /// </summary>
    public int PaymentId { get; set; }

    /// <summary>
    /// Action performed (Initialize, Process, Refund, Webhook, Query)
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Payment status at time of audit
    /// </summary>
    public PaymentStatus Status { get; set; }

    /// <summary>
    /// Payment gateway used
    /// </summary>
    public PaymentGateway Gateway { get; set; }

    /// <summary>
    /// Transaction identifier from gateway
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Sanitized request data (sensitive data removed)
    /// </summary>
    public string RequestData { get; set; } = string.Empty;

    /// <summary>
    /// Sanitized response data (sensitive data removed)
    /// </summary>
    public string ResponseData { get; set; } = string.Empty;

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Processing duration in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Timestamp when action was performed
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// IP address of the request origin
    /// </summary>
    public string CreatedByIp { get; set; } = string.Empty;

    /// <summary>
    /// User agent of the request
    /// </summary>
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// User who initiated the action (if applicable)
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata for the audit entry
    /// </summary>
    public string Metadata { get; set; } = string.Empty; // JSON format

    // Navigation property
    public Payment Payment { get; set; } = null!;
}

/// <summary>
/// Payment method configuration entity for dynamic payment options
/// </summary>
public class PaymentMethodConfiguration
{
    public int Id { get; set; }
    
    /// <summary>
    /// Payment gateway
    /// </summary>
    public PaymentGateway Gateway { get; set; }

    /// <summary>
    /// Payment method
    /// </summary>
    public PaymentMethod Method { get; set; }

    /// <summary>
    /// Whether this method is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Minimum transaction amount
    /// </summary>
    public decimal MinAmount { get; set; }

    /// <summary>
    /// Maximum transaction amount
    /// </summary>
    public decimal MaxAmount { get; set; }

    /// <summary>
    /// Supported currencies (JSON array)
    /// </summary>
    public string SupportedCurrencies { get; set; } = string.Empty;

    /// <summary>
    /// Processing fee percentage
    /// </summary>
    public decimal FeePercentage { get; set; }

    /// <summary>
    /// Fixed processing fee
    /// </summary>
    public decimal FixedFee { get; set; }

    /// <summary>
    /// Display order for UI
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Configuration metadata (JSON)
    /// </summary>
    public string Configuration { get; set; } = string.Empty;

    /// <summary>
    /// When this configuration was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this configuration was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}