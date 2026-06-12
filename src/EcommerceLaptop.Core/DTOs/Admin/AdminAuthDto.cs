using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Core.DTOs.Admin;

/// <summary>
/// Admin login request DTO
/// </summary>
public class AdminLoginRequestDto
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
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
    [Required(ErrorMessage = "Refresh token là bắt buộc")]
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
    [Required(ErrorMessage = "Tên là bắt buộc")]
    [StringLength(50, ErrorMessage = "Tên không được quá 50 ký tự")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ là bắt buộc")]
    [StringLength(50, ErrorMessage = "Họ không được quá 50 ký tự")]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? Email { get; set; }

    public string? Avatar { get; set; }
}

/// <summary>
/// Change admin password request DTO
/// </summary>
public class ChangeAdminPasswordRequestDto
{
    [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
    [MinLength(8, ErrorMessage = "Mật khẩu mới phải có ít nhất 8 ký tự")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]", 
        ErrorMessage = "Mật khẩu phải chứa ít nhất 1 chữ thường, 1 chữ hoa, 1 số và 1 ký tự đặc biệt")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; set; } = string.Empty;
}