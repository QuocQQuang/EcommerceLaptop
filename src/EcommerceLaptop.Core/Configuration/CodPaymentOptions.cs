namespace EcommerceLaptop.Core.Configuration;

/// <summary>
/// Configuration options for COD (Cash on Delivery) payment
/// </summary>
public class CodPaymentOptions
{
    public const string SectionName = "CodPayment";

    /// <summary>
    /// Default fee structure for COD payments
    /// </summary>
    public Dictionary<string, CodFeeStructure> DefaultFeeStructure { get; set; } = new();

    /// <summary>
    /// Maximum COD amount allowed
    /// </summary>
    public decimal MaxCodAmount { get; set; } = 50000000; // 50M VND

    /// <summary>
    /// Minimum COD amount required
    /// </summary>
    public decimal MinCodAmount { get; set; } = 10000; // 10k VND

    /// <summary>
    /// Collection timeout in days
    /// </summary>
    public int CollectionTimeoutDays { get; set; } = 7;

    /// <summary>
    /// Return policy settings
    /// </summary>
    public CodReturnPolicy ReturnPolicy { get; set; } = new();

    /// <summary>
    /// Reconciliation settings
    /// </summary>
    public CodReconciliationOptions Reconciliation { get; set; } = new();
}

/// <summary>
/// COD fee structure for a specific provider
/// </summary>
public class CodFeeStructure
{
    /// <summary>
    /// Fee percentage (e.g., 0.02 = 2%)
    /// </summary>
    public decimal FeePercentage { get; set; } = 0.02m;

    /// <summary>
    /// Minimum fee amount
    /// </summary>
    public decimal MinimumFee { get; set; } = 5000;

    /// <summary>
    /// Maximum fee amount
    /// </summary>
    public decimal MaximumFee { get; set; } = 50000;

    /// <summary>
    /// Free COD threshold (no fee above this amount)
    /// </summary>
    public decimal FreeThreshold { get; set; } = 0;

    /// <summary>
    /// Flat fee (if not using percentage)
    /// </summary>
    public decimal? FlatFee { get; set; }

    /// <summary>
    /// Use flat fee instead of percentage
    /// </summary>
    public bool UseFlatFee { get; set; } = false;
}

/// <summary>
/// COD return policy settings
/// </summary>
public class CodReturnPolicy
{
    /// <summary>
    /// Allow returns for COD orders
    /// </summary>
    public bool AllowReturns { get; set; } = true;

    /// <summary>
    /// Return timeout in days
    /// </summary>
    public int ReturnTimeoutDays { get; set; } = 3;

    /// <summary>
    /// Who pays return shipping fee (Customer, Merchant, Shared)
    /// </summary>
    public string ReturnShippingFeePolicy { get; set; } = "Customer";

    /// <summary>
    /// Automatic return initiation
    /// </summary>
    public bool AutoInitiateReturn { get; set; } = false;

    /// <summary>
    /// Require reason for returns
    /// </summary>
    public bool RequireReturnReason { get; set; } = true;
}

/// <summary>
/// COD reconciliation options
/// </summary>
public class CodReconciliationOptions
{
    /// <summary>
    /// Enable automatic reconciliation
    /// </summary>
    public bool EnableAutoReconciliation { get; set; } = true;

    /// <summary>
    /// Reconciliation frequency in hours
    /// </summary>
    public int ReconciliationFrequencyHours { get; set; } = 24;

    /// <summary>
    /// Grace period for settlement in days
    /// </summary>
    public int SettlementGracePeriodDays { get; set; } = 3;

    /// <summary>
    /// Alert threshold for discrepancies
    /// </summary>
    public decimal DiscrepancyAlertThreshold { get; set; } = 10000;

    /// <summary>
    /// Email notifications for reconciliation
    /// </summary>
    public bool EnableEmailNotifications { get; set; } = true;

    /// <summary>
    /// Notification email addresses
    /// </summary>
    public List<string> NotificationEmails { get; set; } = new();
}