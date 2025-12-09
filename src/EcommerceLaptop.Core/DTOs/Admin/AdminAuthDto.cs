using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Core.DTOs.Admin;

/// <summary>
/// Admin login request DTO
/// </summary>
public class AdminLoginRequestDto
{
    [Required(ErrorMessage = "Email l bt buc")]
    [EmailAddress(ErrorMessage = "Email khng hp l")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mt khu l bt buc")]
    [MinLength(6, ErrorMessage = "Mt khu phi c t nht 6 k t")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}

/// <summary>
/// Admin login response DTO
/// </summary>
public class AdminLoginResponseDto
{
    public AdminUserDto User { get; set; } = null!;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int ExpiresIn { get; set; } // Seconds until expiration
}

/// <summary>
/// Admin user information DTO
/// </summary>
public class AdminUserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string? Avatar { get; set; }
    public bool IsActive { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public AdminRoleDto Role { get; set; } = null!;
    public List<AdminPermissionDto> Permissions { get; set; } = new();
}

/// <summary>
/// Admin role information DTO
/// </summary>
public class AdminRoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<AdminPermissionDto> Permissions { get; set; } = new();
}

/// <summary>
/// Admin permission information DTO
/// </summary>
public class AdminPermissionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "users:read"
    public string Description { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty; // e.g., "users"
    public string Action { get; set; } = string.Empty; // e.g., "read"
}

/// <summary>
/// Token refresh request DTO
/// </summary>
public class AdminRefreshTokenRequestDto
{
    [Required(ErrorMessage = "Refresh token l bt buc")]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Token refresh response DTO
/// </summary>
public class AdminRefreshTokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int ExpiresIn { get; set; }
}

/// <summary>
/// Update admin profile request DTO
/// </summary>
public class UpdateAdminProfileRequestDto
{
    [Required(ErrorMessage = "Tn l bt buc")]
    [StringLength(50, ErrorMessage = "Tn khng c qu 50 k t")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "H l bt buc")]
    [StringLength(50, ErrorMessage = "H khng c qu 50 k t")]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email khng hp l")]
    public string? Email { get; set; }

    public string? Avatar { get; set; }
}

/// <summary>
/// Change admin password request DTO
/// </summary>
public class ChangeAdminPasswordRequestDto
{
    [Required(ErrorMessage = "Mt khu hin ti l bt buc")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mt khu mi l bt buc")]
    [MinLength(8, ErrorMessage = "Mt khu mi phi c t nht 8 k t")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]", 
        ErrorMessage = "Mt khu phi cha t nht 1 ch thng, 1 ch hoa, 1 s v 1 k t c bit")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Xc nhn mt khu l bt buc")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mt khu xc nhn khng khp")]
    public string ConfirmPassword { get; set; } = string.Empty;
}