using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Services;

/// <summary>
/// Customer management service interface with comprehensive CRUD operations
/// Includes privacy controls and permission-based data filtering
/// </summary>
public interface ICustomerManagementService
{
    /// <summary>
    /// Get paginated list of customers with advanced filtering and search
    /// </summary>
    /// <param name="parameters">Search and filter parameters</param>
    /// <returns>Paginated customer list</returns>
    Task<PagedResult<CustomerManagementDto>> GetCustomersAsync(CustomerSearchParameters parameters);

    /// <summary>
    /// Get detailed customer information by ID
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="requesterUserId">ID of user requesting the data (for permission checking)</param>
    /// <returns>Customer details with permission-based filtering</returns>
    Task<CustomerDetailDto?> GetCustomerDetailAsync(int customerId, int requesterUserId);

    /// <summary>
    /// Update customer information (admin only)
    /// </summary>
    /// <param name="customerId">Customer ID to update</param>
    /// <param name="updateRequest">Update data</param>
    /// <param name="updatedByUserId">ID of admin making the update</param>
    /// <returns>Updated customer data</returns>
    Task<CustomerDetailDto?> UpdateCustomerAsync(int customerId, UpdateCustomerRequest updateRequest, int updatedByUserId);

    /// <summary>
    /// Deactivate customer account (soft delete)
    /// </summary>
    /// <param name="customerId">Customer ID to deactivate</param>
    /// <param name="deactivatedByUserId">ID of admin performing the action</param>
    /// <param name="reason">Reason for deactivation</param>
    /// <returns>Success status</returns>
    Task<bool> DeactivateCustomerAsync(int customerId, int deactivatedByUserId, string reason);

    /// <summary>
    /// Reactivate customer account
    /// </summary>
    /// <param name="customerId">Customer ID to reactivate</param>
    /// <param name="reactivatedByUserId">ID of admin performing the action</param>
    /// <param name="reason">Reason for reactivation</param>
    /// <returns>Success status</returns>
    Task<bool> ReactivateCustomerAsync(int customerId, int reactivatedByUserId, string reason);

    /// <summary>
    /// Get customer order history summary
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="requesterUserId">ID of user requesting the data</param>
    /// <returns>Order history summary</returns>
    Task<CustomerOrderHistoryDto> GetCustomerOrderHistoryAsync(int customerId, int requesterUserId);

    /// <summary>
    /// Get customer statistics for admin dashboard
    /// </summary>
    /// <returns>Customer statistics</returns>
    Task<CustomerStatisticsDto> GetCustomerStatisticsAsync();

    /// <summary>
    /// Export customer data (with privacy filtering)
    /// </summary>
    /// <param name="parameters">Export parameters</param>
    /// <param name="requesterUserId">ID of user requesting export</param>
    /// <param name="format">Export format (CSV, Excel, PDF)</param>
    /// <returns>Export data</returns>
    Task<ExportResult> ExportCustomersAsync(CustomerExportParameters parameters, int requesterUserId, ExportFormat format);

    /// <summary>
    /// Send notification to customer (admin only)
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="notification">Notification data</param>
    /// <param name="sentByUserId">ID of admin sending notification</param>
    /// <returns>Success status</returns>
    Task<bool> SendCustomerNotificationAsync(int customerId, CustomerNotificationRequest notification, int sentByUserId);

    /// <summary>
    /// Get customer activity logs (admin only)
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="requesterUserId">ID of user requesting logs</param>
    /// <param name="parameters">Filter parameters</param>
    /// <returns>Activity logs</returns>
    Task<PagedResult<CustomerActivityLogDto>> GetCustomerActivityLogsAsync(int customerId, int requesterUserId, ActivityLogParameters parameters);
}

/// <summary>
/// Customer search and filter parameters
/// </summary>
public class CustomerSearchParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public string? Email { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? RegisteredFrom { get; set; }
    public DateTime? RegisteredTo { get; set; }
    public string? SortBy { get; set; } = "CreatedAt";
    public string? SortOrder { get; set; } = "desc";
    public decimal? MinTotalSpent { get; set; }
    public decimal? MaxTotalSpent { get; set; }
    public int? MinOrderCount { get; set; }
    public int? MaxOrderCount { get; set; }
    public bool? EmailVerified { get; set; }
    public int? VipTierId { get; set; }
}

/// <summary>
/// Customer export parameters
/// </summary>
public class CustomerExportParameters : CustomerSearchParameters
{
    public string[] IncludeFields { get; set; } = Array.Empty<string>();
    public bool IncludeOrderHistory { get; set; } = false;
    public bool IncludeAddresses { get; set; } = false;
    public bool IncludePersonalData { get; set; } = false; // Requires special permission
}

/// <summary>
// Types moved to CustomerManagementDTOs.cs to avoid duplication