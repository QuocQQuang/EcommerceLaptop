using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.ValueObjects;

namespace EcommerceLaptop.Core.Services;

/// <summary>
/// Service interface for managing user VIP tier assignments
/// </summary>
public interface IUserVipTierService
{
    /// <summary>
    /// Get paginated list of user VIP tier assignments
    /// </summary>
    Task<ServiceResult<PaginatedList<UserVipTierAssignmentDto>>> GetUserVipTiersAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        int? tierId = null,
        bool? isActive = null);

    /// <summary>
    /// Get user VIP tier assignment by ID
    /// </summary>
    Task<ServiceResult<UserVipTierAssignmentDto?>> GetUserVipTierByIdAsync(int id);

    /// <summary>
    /// Get user's current active VIP tier
    /// </summary>
    Task<ServiceResult<UserCurrentTierDto?>> GetUserCurrentTierAsync(string userId);

    /// <summary>
    /// Assign VIP tier to user
    /// </summary>
    Task<ServiceResult<User>> AssignVipTierAsync(
        string userId, 
        int vipTierId, 
        string? reason = null);

    /// <summary>
    /// Update VIP tier assignment
    /// </summary>
    Task<ServiceResult<User>> UpdateVipTierAssignmentAsync(
        string userId, 
        int vipTierId, 
        string? reason = null);

    /// <summary>
    /// Revoke VIP tier assignment
    /// </summary>
    Task<ServiceResult<bool>> RevokeVipTierAsync(int assignmentId, string? reason = null);

    /// <summary>
    /// Get VIP tier statistics
    /// </summary>
    Task<ServiceResult<VipTierStatistics>> GetVipTierStatisticsAsync();

    /// <summary>
    /// Bulk update VIP tier assignments for multiple users
    /// </summary>
    Task<ServiceResult<BulkUpdateResult>> BulkUpdateVipTiersAsync(
        List<string> userIds, 
        int vipTierId, 
        DateTime? expiresAt = null, 
        string? reason = null);

    /// <summary>
    /// Check if user qualifies for automatic tier upgrade
    /// </summary>
    Task<ServiceResult<TierUpgradeEligibility>> CheckTierUpgradeEligibilityAsync(string userId);

    /// <summary>
    /// Process automatic tier upgrades based on user activity
    /// </summary>
    Task<ServiceResult<List<TierUpgradeResult>>> ProcessAutomaticTierUpgradesAsync();
}

/// <summary>
/// DTO for user VIP tier assignment with user and tier details
/// </summary>
public class UserVipTierAssignmentDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int VipTierId { get; set; }
    public string VipTierName { get; set; } = string.Empty;
    public string VipTierColor { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public string? Reason { get; set; }
    public string? AssignedBy { get; set; }
}

/// <summary>
/// DTO for user's current VIP tier information
/// </summary>
public class UserCurrentTierDto
{
    public int? VipTierId { get; set; }
    public string? VipTierName { get; set; }
    public string? VipTierColor { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool HasActiveTier { get; set; }
    public List<string> Benefits { get; set; } = new();
}

/// <summary>
/// VIP tier statistics
/// </summary>
public class VipTierStatistics
{
    public int TotalActiveAssignments { get; set; }
    public int TotalExpiredAssignments { get; set; }
    public int TotalRevokedAssignments { get; set; }
    public Dictionary<string, int> TierDistribution { get; set; } = new();
    public int AssignmentsExpiringThisWeek { get; set; }
    public int AssignmentsExpiringThisMonth { get; set; }
    public decimal AverageDiscountPercentage { get; set; }
}

/// <summary>
/// Bulk update operation result
/// </summary>
public class BulkUpdateResult
{
    public int TotalProcessed { get; set; }
    public int SuccessfulUpdates { get; set; }
    public int FailedUpdates { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Tier upgrade eligibility information
/// </summary>
public class TierUpgradeEligibility
{
    public bool IsEligible { get; set; }
    public int? RecommendedTierId { get; set; }
    public string? RecommendedTierName { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal TotalSpent { get; set; }
    public int TotalOrders { get; set; }
    public DateTime RegistrationDate { get; set; }
}

/// <summary>
/// Automatic tier upgrade result
/// </summary>
public class TierUpgradeResult
{
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int OldTierId { get; set; }
    public int NewTierId { get; set; }
    public string OldTierName { get; set; } = string.Empty;
    public string NewTierName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}