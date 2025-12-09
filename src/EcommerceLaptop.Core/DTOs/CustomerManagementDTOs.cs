namespace EcommerceLaptop.Core.DTOs;

/// <summary>
/// Customer management DTO for admin dashboard listing
/// Contains privacy-filtered data based on user permissions
/// </summary>
public class CustomerManagementDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();

    // Conditional fields based on permissions
    public string Email { get; set; } = string.Empty; // Requires customers:read permission
    public string? MaskedEmail { get; set; } // Fallback for limited permissions
    public string? PhoneNumber { get; set; } // Requires customers:read permission
    public string? MaskedPhoneNumber { get; set; } // Fallback for limited permissions

    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Order statistics
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastOrderDate { get; set; }

    // VIP information
    public int? VipTierId { get; set; }
    public string? VipTierName { get; set; }

    // Risk indicators (admin only)
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }

    // Permission flags (determined at runtime)
    public bool CanViewDetails { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanViewPersonalData { get; set; }
}

/// <summary>
/// Detailed customer information DTO
/// Contains comprehensive customer data with permission-based filtering
/// </summary>
public class CustomerDetailDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; } // PII - requires special permission
    public string? Gender { get; set; }
    public string? ProfilePictureUrl { get; set; }

    // Account information
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIP { get; set; } // Requires customers:manage permission

    // Security information (admin only)
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastPasswordChangeDate { get; set; }

    // VIP information
    public int? VipTierId { get; set; }
    public string? VipTierName { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? VipTierUpdatedAt { get; set; }

    // Order statistics
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public DateTime? FirstOrderDate { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public decimal AverageOrderValue { get; set; }

    // Addresses (filtered based on permissions)
    public List<CustomerAddressDto> Addresses { get; set; } = new();

    // Recent activity summary
    public List<CustomerRecentActivityDto> RecentActivities { get; set; } = new();

    // Administrative notes (admin only)
    public string? Notes { get; set; }
    public DateTime? NotesUpdatedAt { get; set; }
    public string? NotesUpdatedBy { get; set; }

    // Permission flags
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanViewOrderHistory { get; set; }
    public bool CanViewPersonalData { get; set; }
    public bool CanViewSecurityInfo { get; set; }
}

/// <summary>
/// Customer address DTO with privacy controls
/// </summary>
public class CustomerAddressDto
{
    public int Id { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string? ZipCode { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }

    // Masked version for limited permissions
    public string? MaskedStreet { get; set; }
    public bool IsFullDataVisible { get; set; }
}

/// <summary>
/// Customer recent activity DTO
/// </summary>
public class CustomerRecentActivityDto
{
    public DateTime ActivityDate { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IpAddress { get; set; } // Admin only
    public string? UserAgent { get; set; } // Admin only
}

/// <summary>
/// Customer order history summary DTO
/// </summary>
public class CustomerOrderHistoryDto
{
    public int CustomerId { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AverageOrderValue { get; set; }
    public DateTime? FirstOrderDate { get; set; }
    public DateTime? LastOrderDate { get; set; }

    // Order status breakdown
    public int PendingOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int RefundedOrders { get; set; }

    // Recent orders (limited list)
    public List<CustomerOrderSummaryDto> RecentOrders { get; set; } = new();

    // Spending patterns
    public List<MonthlySpendingDto> MonthlySpending { get; set; } = new();
}

/// <summary>
/// Customer order summary for history
/// </summary>
public class CustomerOrderSummaryDto
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

/// <summary>
/// Monthly spending data for charts
/// </summary>
public class MonthlySpendingDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public int OrderCount { get; set; }
}

/// <summary>
/// Customer statistics for dashboard
/// </summary>
public class CustomerStatisticsDto
{
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int NewCustomersThisMonth { get; set; }
    public int NewCustomersToday { get; set; }
    public decimal AverageCustomerValue { get; set; }
    public int EmailVerifiedCustomers { get; set; }
    public int UnverifiedCustomers { get; set; }

    // VIP tier breakdown
    public List<VipTierStatDto> VipTierStats { get; set; } = new();

    // Registration trends
    public List<CustomerRegistrationTrendDto> RegistrationTrends { get; set; } = new();

    // Activity stats
    public int CustomersLoggedInToday { get; set; }
    public int CustomersLoggedInThisWeek { get; set; }
    public int InactiveCustomers30Days { get; set; }
}

/// <summary>
/// VIP tier statistics
/// </summary>
public class VipTierStatDto
{
    public int TierId { get; set; }
    public string TierName { get; set; } = string.Empty;
    public int CustomerCount { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AverageSpent { get; set; }
}

/// <summary>
/// Customer registration trend data
/// </summary>
public class CustomerRegistrationTrendDto
{
    public DateTime Date { get; set; }
    public int NewRegistrations { get; set; }
    public int EmailVerifications { get; set; }
}

/// <summary>
/// Update customer request DTO
/// </summary>
public class UpdateCustomerRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }

    // Admin-only fields
    public bool? EmailConfirmed { get; set; }
    public DateTime? LockedUntil { get; set; } // For account locking/unlocking
    public int? VipTierId { get; set; }
}

/// <summary>
/// Customer notification request
/// </summary>
public class CustomerNotificationRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string NotificationType { get; set; } = "general"; // general, promotion, security, system
    public bool SendEmail { get; set; } = true;
    public bool SendInApp { get; set; } = false;
    public DateTime? ScheduledAt { get; set; } // For scheduled notifications
}

/// <summary>
/// Customer activity log DTO
/// </summary>
public class CustomerActivityLogDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Metadata { get; set; } // JSON metadata for additional context
}

/// <summary>
/// Activity log search parameters
/// </summary>
public class ActivityLogParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? ActivityType { get; set; }
    public string? SortOrder { get; set; } = "desc";
}

/// <summary>
/// Export result DTO
/// </summary>
public class ExportResult
{
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>
/// Export formats enumeration
/// </summary>
public enum ExportFormat
{
    CSV,
    Excel,
    PDF
}