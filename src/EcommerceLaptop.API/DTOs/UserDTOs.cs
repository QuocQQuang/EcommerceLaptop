using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.API.DTOs;

#region User DTOs

/// <summary>
/// User data transfer object
/// </summary>
public class UserProfileDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string PhoneNumber { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<AddressDto> Addresses { get; set; } = new();
}

/// <summary>
/// User summary DTO for admin lists
/// </summary>
public class UserSummaryDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
}

/// <summary>
/// Role DTO
/// </summary>
public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

#endregion

#region User Request DTOs

/// <summary>
/// Update user profile request DTO
/// </summary>
public class UpdateUserProfileRequest
{
    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [StringLength(20)]
    [Phone]
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Create user request DTO (Admin only)
/// </summary>
public class CreateUserRequest
{
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = new() { "Customer" };
}

/// <summary>
/// Update user request DTO (Admin only)
/// </summary>
public class UpdateUserRequest
{
    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [StringLength(20)]
    [Phone]
    public string? PhoneNumber { get; set; }

    public bool? IsActive { get; set; }

    public List<string>? Roles { get; set; }
}

/// <summary>
/// Assign role request DTO
/// </summary>
public class AssignRoleRequest
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public string RoleName { get; set; } = string.Empty;
}

/// <summary>
/// Create address request DTO
/// </summary>
public class CreateUserAddressRequest
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Street { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Province { get; set; } = string.Empty;

    [StringLength(100)]
    public string District { get; set; } = string.Empty;

    [StringLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    [StringLength(100)]
    public string Country { get; set; } = string.Empty;

    public bool IsDefault { get; set; } = false;
}

/// <summary>
/// Update address request DTO
/// </summary>
public class UpdateAddressRequest
{
    [StringLength(100)]
    public string? FullName { get; set; }

    [StringLength(15)]
    public string? PhoneNumber { get; set; }

    [StringLength(255)]
    public string? Street { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? Province { get; set; }

    [StringLength(100)]
    public string? District { get; set; }

    [StringLength(20)]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    public bool? IsDefault { get; set; }
}

/// <summary>
/// Change password request DTO with enhanced security validation
/// </summary>
public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Mt khu hin ti l bt buc")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mt khu mi l bt buc")]
    [MinLength(8, ErrorMessage = "Mt khu mi phi c t nht 8 k t")]
    [MaxLength(128, ErrorMessage = "Mt khu mi khng c vt qu 128 k t")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Xc nhn mt khu l bt buc")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mt khu xc nhn khng khp")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

#endregion
