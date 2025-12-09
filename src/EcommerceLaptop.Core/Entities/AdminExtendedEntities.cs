namespace EcommerceLaptop.Core.Entities;

/// <summary>
/// Product Category entity for hierarchical category structure
/// </summary>
public class ProductCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ProductCategory? Parent { get; set; }
    public ICollection<ProductCategory> Children { get; set; } = new List<ProductCategory>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

/// <summary>
/// Product Brand entity for brand management
/// </summary>
public class ProductBrand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? ContactEmail { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

/// <summary>
/// System Settings entity for key-value configuration storage
/// </summary>
public class SystemSetting
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string SettingKey { get; set; } = string.Empty;
    public string? SettingValue { get; set; }
    public string DataType { get; set; } = "string"; // string, int, boolean, json
    public bool IsEncrypted { get; set; } = false;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}



/// <summary>
/// Blog Tag entity for tagging system
/// </summary>
public class BlogTag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#6B7280";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<BlogPostTag> BlogPostTags { get; set; } = new List<BlogPostTag>();
}

/// <summary>
/// Junction table for BlogPost-Tag many-to-many relationship
/// </summary>
public class BlogPostTag
{
    public int BlogPostId { get; set; }
    public int BlogTagId { get; set; }

    // Navigation properties
    public BlogPost BlogPost { get; set; } = null!;
    public BlogTag BlogTag { get; set; } = null!;
}

/// <summary>
/// User Activity Log for tracking important user actions
/// </summary>
public class UserActivityLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty; // login, order_created, profile_updated, etc.
    public string Description { get; set; } = string.Empty;
    public string? EntityType { get; set; } // Order, Product, etc.
    public string? EntityId { get; set; }
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
}

/// <summary>
/// Order Event for tracking order status changes and history
/// </summary>
public class OrderEvent
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string EventType { get; set; } = string.Empty; // status_change, payment_received, shipped, etc.
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? AdminUserId { get; set; } // Who made the change (if admin)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Order Order { get; set; } = null!;
    public User? AdminUser { get; set; }
}

/// <summary>
/// User VIP Tier for customer segmentation
/// </summary>
public class UserVipTier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Bronze, Silver, Gold, VIP
    public string Description { get; set; } = string.Empty;
    public decimal MinSpendAmount { get; set; } // Minimum spend to achieve this tier
    public decimal DiscountPercentage { get; set; } // Discount for this tier
    public string? Color { get; set; } // UI color representation
    public string? Icon { get; set; } // UI icon
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
}

/// <summary>
/// Refund entity for order refund tracking
/// </summary>
public class Refund
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // pending, approved, rejected, processed
    public string? PaymentGateway { get; set; } // vnpay, zalopay, momo
    public string? TransactionId { get; set; }
    public string? GatewayResponse { get; set; }
    public int? ProcessedByAdminId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Order Order { get; set; } = null!;
    public User? ProcessedByAdmin { get; set; }
}

/// <summary>
/// Shipping Rate entity for shipping cost calculation
/// </summary>
public class ShippingRate
{
    public int Id { get; set; }
    public string Provider { get; set; } = string.Empty; // GHN, ViettelPost, etc.
    public string ServiceName { get; set; } = string.Empty;
    public string FromProvince { get; set; } = string.Empty;
    public string ToProvince { get; set; } = string.Empty;
    public decimal BaseRate { get; set; }
    public decimal RatePerKg { get; set; }
    public int EstimatedDeliveryDays { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

#region Security Entities

/// <summary>
/// IP Block Rule entity for managing IP access control
/// </summary>
public class IPBlockRule
{
    public int Id { get; set; }
    public string IPAddress { get; set; } = string.Empty; // IP or CIDR range (e.g., "192.168.1.0/24")
    public string Type { get; set; } = "blacklist"; // blacklist or whitelist
    public string Reason { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string? LastUpdatedBy { get; set; }
    
    // Enhanced fields
    public string ThreatLevel { get; set; } = "medium"; // low, medium, high, critical
    public string? CountryCode { get; set; }
    public int ViolationCount { get; set; } = 0;
    public DateTime? LastViolation { get; set; }
    public string? RuleSource { get; set; } // manual, automated, external
    public bool AutoGenerated { get; set; } = false;
    public int TriggerCount { get; set; } = 0; // How many times this rule has been triggered
    public string? LastTriggeredBy { get; set; } // IP that last triggered this rule
    public DateTime? LastTriggeredAt { get; set; } // When this rule was last triggered
}

/// <summary>
/// Rate Limiting Rule entity for controlling API/endpoint access rates
/// </summary>
public class RateLimitRule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty; // URL pattern or specific endpoint
    public string HttpMethod { get; set; } = "ALL"; // GET, POST, PUT, DELETE, ALL
    public int RequestsPerMinute { get; set; } = 60;
    public int RequestsPerHour { get; set; } = 1000;
    public int RequestsPerDay { get; set; } = 10000;
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 0; // Higher number = higher priority
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string? LastUpdatedBy { get; set; }
    
    // Enhanced configuration
    public string? Description { get; set; }
    public string IPWhitelist { get; set; } = "[]"; // JSON array of whitelisted IPs
    public string UserRoleExceptions { get; set; } = "[]"; // JSON array of roles that bypass this rule
    public string ApiKeyExceptions { get; set; } = "[]"; // JSON array of API keys that bypass
    public int CooldownSeconds { get; set; } = 60; // How long to wait after limit exceeded
    public string? CustomErrorMessage { get; set; }
    public bool BlockOnExceed { get; set; } = false; // Temporarily block IP after exceeding limit
    public int BlockDurationMinutes { get; set; } = 15;
    public bool IsSystemRule { get; set; } = false; // Whether this is a system-defined rule
}

/// <summary>
/// Rate Limiting Violation tracking
/// </summary>
public class RateLimitViolation
{
    public int Id { get; set; }
    public int RuleId { get; set; }
    public string IPAddress { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public int LimitExceeded { get; set; }
    public DateTime TimeWindow { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? UserId { get; set; }
    public string? UserAgent { get; set; }
    public string? SessionId { get; set; }
    public int RateLimitRuleId { get; set; } // Foreign key to RateLimitRule
    public DateTime WindowStart { get; set; } // Start of the rate limit window
    public DateTime WindowEnd { get; set; } // End of the rate limit window
    public bool WasBlocked { get; set; } = true; // Whether the request was blocked
    public string Action { get; set; } = "blocked"; // Action taken (blocked, warned, etc.)
    
    // Navigation
    public RateLimitRule Rule { get; set; } = null!;
}

/// <summary>
/// Security Event entity for logging security-related incidents
/// </summary>
public class SecurityEvent
{
    public int Id { get; set; }
    public string EventType { get; set; } = string.Empty; // failed_login, ip_blocked, rate_limit_exceeded, etc.
    public string Severity { get; set; } = "medium"; // low, medium, high, critical
    public string IPAddress { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public int? AdminUserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Details { get; set; } = "{}"; // JSON details
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "new"; // new, investigating, resolved, false_positive
    public string? InvestigatedBy { get; set; }
    public DateTime? InvestigatedAt { get; set; }
    public string? Resolution { get; set; }
    
    // Enhanced tracking
    public string? UserAgent { get; set; }
    public string? SessionId { get; set; }
    public string? Endpoint { get; set; }
    public string? RequestMethod { get; set; }
    public string? CorrelationId { get; set; } // Link related events
    public bool RequiresReview { get; set; } = false;
    public string? Tags { get; set; } // JSON array for categorization
    public string RiskScore { get; set; } = "0"; // 0-100 risk assessment
    public bool WasBlocked { get; set; } = false; // Whether the action was blocked
    public string? InvestigationNotes { get; set; } // Additional investigation notes
    
    // Navigation properties
    public User? User { get; set; }
}

/// <summary>
/// System Audit Log for comprehensive activity tracking
/// </summary>
public class SystemAuditLog
{
    public int Id { get; set; }
    public string EventCategory { get; set; } = string.Empty; // authentication, authorization, data_access, etc.
    public string EventType { get; set; } = string.Empty; // login, logout, create, update, delete
    public string EntityType { get; set; } = string.Empty; // User, Product, Order, etc.
    public string? EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? OldValues { get; set; } // JSON of old values
    public string? NewValues { get; set; } // JSON of new values
    public string? ChangedFields { get; set; } // JSON array of changed field names
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // User context
    public int? UserId { get; set; }
    public int? AdminUserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserRole { get; set; }
    
    // Request context
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? SessionId { get; set; }
    public string? RequestId { get; set; }
    public string? Endpoint { get; set; }
    public string? HttpMethod { get; set; }
    public int? ExecutionTimeMs { get; set; }
    
    // Risk and compliance
    public string RiskLevel { get; set; } = "low"; // low, medium, high, critical
    public bool RequiresReview { get; set; } = false;
    public string? ComplianceNotes { get; set; }
    public string? AdditionalMetadata { get; set; } // JSON for extra context
    
    // Navigation properties
    public User? User { get; set; }
    public User? AdminUser { get; set; }
}

/// <summary>
/// Login Attempt tracking for security analysis
/// </summary>
public class LoginAttempt
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string IPAddress { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    public string? UserAgent { get; set; }
    public string? SessionId { get; set; }
    public string UserType { get; set; } = "user"; // user, admin
    
    // Additional tracking
    public int? UserId { get; set; }
    public int? AdminUserId { get; set; }
    public string? TwoFactorMethod { get; set; }
    public bool TwoFactorRequired { get; set; } = false;
    public bool TwoFactorSuccess { get; set; } = false;
    public string? GeoLocation { get; set; } // Country/City if available
    public bool SuspiciousActivity { get; set; } = false;
    public string? RiskFactors { get; set; } // JSON array of risk indicators
}

#endregion