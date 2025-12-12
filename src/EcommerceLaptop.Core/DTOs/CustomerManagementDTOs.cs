namespace EcommerceLaptop.Core.DTOs;

/// <summary>
/// Customer management DTO for admin dashboard listing
/// Contains privacy-filtered data based on user permissions
/// </summary>
public record CustomerManagementDto(
    int Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email, // Requires customers:read permission
    string? MaskedEmail, // Fallback for limited permissions
    string? PhoneNumber, // Requires customers:read permission
    string? MaskedPhoneNumber, // Fallback for limited permissions
    bool IsActive,
    bool EmailConfirmed,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    int TotalOrders,
    decimal TotalSpent,
    DateTime? LastOrderDate,
    int? VipTierId,
    string? VipTierName,
    int FailedLoginAttempts,
    DateTime? LockedUntil,
    bool CanViewDetails,
    bool CanEdit,
    bool CanDelete,
    bool CanViewPersonalData);

/// <summary>
/// Detailed customer information DTO
/// Contains comprehensive customer data with permission-based filtering
/// </summary>
public record CustomerDetailDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    DateTime? DateOfBirth, // PII - requires special permission
    string? Gender,
    string? ProfilePictureUrl,
    bool IsActive,
    bool EmailConfirmed,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastLoginAt,
    string? LastLoginIP, // Requires customers:manage permission
    int FailedLoginAttempts,
    DateTime? LockedUntil,
    DateTime? LastPasswordChangeDate,
    int? VipTierId,
    string? VipTierName,
    decimal TotalSpent,
    DateTime? VipTierUpdatedAt,
    int TotalOrders,
    int CompletedOrders,
    int CancelledOrders,
    DateTime? FirstOrderDate,
    DateTime? LastOrderDate,
    decimal AverageOrderValue,
    List<CustomerAddressDto> Addresses,
    List<CustomerRecentActivityDto> RecentActivities,
    string? Notes,
    DateTime? NotesUpdatedAt,
    string? NotesUpdatedBy,
    bool CanEdit,
    bool CanDelete,
    bool CanViewOrderHistory,
    bool CanViewPersonalData,
    bool CanViewSecurityInfo);

/// <summary>
/// Customer address DTO with privacy controls
/// </summary>
public record CustomerAddressDto(
    int Id,
    string Street,
    string City,
    string District,
    string Ward,
    string? ZipCode,
    bool IsDefault,
    DateTime CreatedAt,
    string? MaskedStreet,
    bool IsFullDataVisible);

/// <summary>
/// Customer recent activity DTO
/// </summary>
public record CustomerRecentActivityDto(
    DateTime ActivityDate,
    string ActivityType,
    string Description,
    string? IpAddress, // Admin only
    string? UserAgent); // Admin only

/// <summary>
/// Customer order history summary DTO
/// </summary>
public record CustomerOrderHistoryDto(
    int CustomerId,
    int TotalOrders,
    decimal TotalSpent,
    decimal AverageOrderValue,
    DateTime? FirstOrderDate,
    DateTime? LastOrderDate,
    int PendingOrders,
    int CompletedOrders,
    int CancelledOrders,
    int RefundedOrders,
    List<CustomerOrderSummaryDto> RecentOrders,
    List<MonthlySpendingDto> MonthlySpending);

/// <summary>
/// Customer order summary for history
/// </summary>
public record CustomerOrderSummaryDto(
    int OrderId,
    string OrderNumber,
    DateTime OrderDate,
    decimal Total,
    string Status,
    int ItemCount);

/// <summary>
/// Monthly spending data for charts
/// </summary>
public record MonthlySpendingDto(
    int Year,
    int Month,
    decimal Amount,
    int OrderCount);

/// <summary>
/// Customer statistics for dashboard
/// </summary>
public record CustomerStatisticsDto(
    int TotalCustomers,
    int ActiveCustomers,
    int NewCustomersThisMonth,
    int NewCustomersToday,
    decimal AverageCustomerValue,
    int EmailVerifiedCustomers,
    int UnverifiedCustomers,
    List<VipTierStatDto> VipTierStats,
    List<CustomerRegistrationTrendDto> RegistrationTrends,
    int CustomersLoggedInToday,
    int CustomersLoggedInThisWeek,
    int InactiveCustomers30Days);

/// <summary>
/// VIP tier statistics
/// </summary>
public record VipTierStatDto(
    int TierId,
    string TierName,
    int CustomerCount,
    decimal TotalSpent,
    decimal AverageSpent);

/// <summary>
/// Customer registration trend data
/// </summary>
public record CustomerRegistrationTrendDto(
    DateTime Date,
    int NewRegistrations,
    int EmailVerifications);

/// <summary>
/// Update customer request DTO
/// </summary>
public record UpdateCustomerRequest(
    string? FirstName = null,
    string? LastName = null,
    string? PhoneNumber = null,
    DateTime? DateOfBirth = null,
    string? Gender = null,
    bool? IsActive = null,
    string? Notes = null,
    bool? EmailConfirmed = null,
    DateTime? LockedUntil = null, // For account locking/unlocking
    int? VipTierId = null);

/// <summary>
/// Customer notification request
/// </summary>
public record CustomerNotificationRequest(
    string Subject,
    string Message,
    string NotificationType = "general", // general, promotion, security, system
    bool SendEmail = true,
    bool SendInApp = false,
    DateTime? ScheduledAt = null); // For scheduled notifications

/// <summary>
/// Customer activity log DTO
/// </summary>
public record CustomerActivityLogDto(
    int Id,
    int CustomerId,
    string Action,
    string Description,
    DateTime CreatedAt,
    string? IpAddress,
    string? UserAgent,
    string? EntityType,
    string? EntityId,
    string? Metadata); // JSON metadata for additional context

/// <summary>
/// Activity log search parameters
/// </summary>
public record ActivityLogParameters(
    int Page = 1,
    int PageSize = 20,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? ActivityType = null,
    string? SortOrder = "desc");

/// <summary>
/// Export result DTO
/// </summary>
public record ExportResult(
    byte[] Data,
    string FileName,
    string ContentType);

/// <summary>
/// Export formats enumeration
/// </summary>
public enum ExportFormat
{
    CSV,
    Excel,
    PDF
}