using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Core.DTOs;

/// <summary>
/// Request DTO for assigning VIP tier to user
/// </summary>
public class AssignUserVipTierRequest
{
    [Required(ErrorMessage = "User ID is required")]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "VIP Tier ID is required")]
    public int VipTierId { get; set; }

    public DateTime? ExpiresAt { get; set; }

    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string? Reason { get; set; }
}

/// <summary>
/// Request DTO for updating user VIP tier assignment
/// </summary>
public class UpdateUserVipTierRequest
{
    [Required(ErrorMessage = "VIP Tier ID is required")]
    public int VipTierId { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string? Reason { get; set; }
}

/// <summary>
/// Request DTO for revoking VIP tier assignment
/// </summary>
public class RevokeVipTierRequest
{
    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string? Reason { get; set; }
}

/// <summary>
/// Request DTO for bulk updating VIP tier assignments
/// </summary>
public class BulkUpdateVipTiersRequest
{
    [Required(ErrorMessage = "User IDs are required")]
    [MinLength(1, ErrorMessage = "At least one user ID is required")]
    public List<string> UserIds { get; set; } = new();

    [Required(ErrorMessage = "VIP Tier ID is required")]
    public int VipTierId { get; set; }

    public DateTime? ExpiresAt { get; set; }

    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string? Reason { get; set; }
}