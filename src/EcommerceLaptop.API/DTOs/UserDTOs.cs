using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.API.DTOs;

#region User DTOs

/// <summary>
/// User data transfer object
/// </summary>
public record UserProfileDto
{
    public int Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string PhoneNumber { get; init; } = string.Empty;
    public string? ProfilePictureUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public bool IsActive { get; init; }
    public List<string> Roles { get; init; } = new();
    public List<AddressDto> Addresses { get; init; } = new();
}

/// <summary>
/// User summary DTO for admin lists
/// </summary>
public record UserSummaryDto
{
    public int Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string PhoneNumber { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public bool IsActive { get; init; }
    public List<string> Roles { get; init; } = new();
    public int TotalOrders { get; init; }
    public decimal TotalSpent { get; init; }
}

/// <summary>
/// Role DTO
/// </summary>
public record RoleDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

#endregion

#region User Request DTOs

/// <summary>
/// Update user profile request DTO
/// </summary>
public record UpdateUserProfileRequest
{
    [StringLength(100)]
    public string? FirstName { get; init; }

    [StringLength(100)]
    public string? LastName { get; init; }

    [StringLength(20)]
    [Phone]
    public string? PhoneNumber { get; init; }
}

/// <summary>
/// Create user request DTO (Admin only)
/// </summary>
public record CreateUserRequest
{
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Required]
    [StringLength(20)]
    [Phone]
    public string PhoneNumber { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; init; } = string.Empty;

    public List<string> Roles { get; init; } = new() { "Customer" };
}

/// <summary>
/// Update user request DTO (Admin only)
/// </summary>
public record UpdateUserRequest
{
    [StringLength(100)]
    public string? FirstName { get; init; }

    [StringLength(100)]
    public string? LastName { get; init; }

    [StringLength(20)]
    [Phone]
    public string? PhoneNumber { get; init; }

    public bool? IsActive { get; init; }

    public List<string>? Roles { get; init; }
}

/// <summary>
/// Assign role request DTO
/// </summary>
public record AssignRoleRequest
{
    [Required]
    public int UserId { get; init; }

    [Required]
    public string RoleName { get; init; } = string.Empty;
}

/// <summary>
/// Create address request DTO
/// </summary>
public record CreateUserAddressRequest
{
    [Required]
    [StringLength(100)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [StringLength(15)]
    public string PhoneNumber { get; init; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Street { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Province { get; init; } = string.Empty;

    [StringLength(100)]
    public string District { get; init; } = string.Empty;

    [StringLength(20)]
    public string PostalCode { get; init; } = string.Empty;

    [StringLength(100)]
    public string Country { get; init; } = string.Empty;

    public bool IsDefault { get; init; } = false;
}

/// <summary>
/// Update address request DTO
/// </summary>
public record UpdateAddressRequest
{
    [StringLength(100)]
    public string? FullName { get; init; }

    [StringLength(15)]
    public string? PhoneNumber { get; init; }

    [StringLength(255)]
    public string? Street { get; init; }

    [StringLength(100)]
    public string? City { get; init; }

    [StringLength(100)]
    public string? Province { get; init; }

    [StringLength(100)]
    public string? District { get; init; }

    [StringLength(20)]
    public string? PostalCode { get; init; }

    [StringLength(100)]
    public string? Country { get; init; }

    public bool? IsDefault { get; init; }
}

/// <summary>
/// Change password request DTO with enhanced security validation
/// </summary>
public record ChangePasswordRequest
{
    [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
    [MinLength(8, ErrorMessage = "Mật khẩu mới phải có ít nhất 8 ký tự")]
    [MaxLength(128, ErrorMessage = "Mật khẩu mới không được vượt quá 128 ký tự")]
    public string NewPassword { get; init; } = string.Empty;

    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; init; } = string.Empty;
}

#endregion
