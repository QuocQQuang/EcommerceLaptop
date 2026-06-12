using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.API.DTOs;

#region Password Reset DTOs

/// <summary>
/// Request DTO for initiating password reset
/// </summary>
public record ForgotPasswordRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
    [StringLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự")]
    public string Email { get; init; } = string.Empty;
}

/// <summary>
/// Response DTO for forgot password request
/// </summary>
public record ForgotPasswordResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Request DTO for password reset confirmation
/// </summary>
public record ResetPasswordRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Password reset token from email
    /// </summary>
    [Required(ErrorMessage = "Token là bắt buộc")]
    [StringLength(128, MinimumLength = 10, ErrorMessage = "Token không hợp lệ")]
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// New password
    /// </summary>
    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự và không quá 100 ký tự")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt")]
    public string NewPassword { get; init; } = string.Empty;

    /// <summary>
    /// Confirm new password
    /// </summary>
    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; init; } = string.Empty;
}

/// <summary>
/// Response DTO for password reset confirmation
/// </summary>
public record ResetPasswordResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime ResetAt { get; init; } = DateTime.UtcNow;
    public bool RequiresLogin { get; init; } = true;
}

/// <summary>
/// Request DTO for validating password reset token
/// </summary>
public record ValidateResetTokenRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Password reset token to validate
    /// </summary>
    [Required(ErrorMessage = "Token là bắt buộc")]
    [StringLength(128, MinimumLength = 10, ErrorMessage = "Token không hợp lệ")]
    public string Token { get; init; } = string.Empty;
}

/// <summary>
/// Response DTO for token validation
/// </summary>
public record ValidateResetTokenResponse
{
    public bool IsValid { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime? ExpiresAt { get; init; }
    public int? MinutesRemaining { get; init; }
}

#endregion

#region Enhanced Authentication DTOs

/// <summary>
/// Enhanced login request with additional security features
/// </summary>
public record LoginRequest
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Mật khẩu không hợp lệ")]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Remember user login for extended session
    /// </summary>
    public bool RememberMe { get; init; } = false;

    /// <summary>
    /// Client's device fingerprint for security
    /// </summary>
    public string? DeviceFingerprint { get; init; }
}

/// <summary>
/// Enhanced registration request
/// </summary>
public record RegisterRequest
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
    [StringLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự và không quá 100 ký tự")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt")]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
    [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; init; } = string.Empty;

    [Required(ErrorMessage = "Tên là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
    public string FirstName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Họ là bắt buộc")]
    [StringLength(100, ErrorMessage = "Họ không được vượt quá 100 ký tự")]
    public string LastName { get; init; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Accept terms and conditions
    /// </summary>
    [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn phải đồng ý với điều khoản và điều kiện")]
    public bool AcceptTerms { get; init; } = false;
}


/// <summary>
/// Refresh token request
/// </summary>
public record RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token là bắt buộc")]
    public string RefreshToken { get; init; } = string.Empty;
}

/// <summary>
/// Enhanced login response with security information
/// </summary>
public record LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public UserDto User { get; init; } = null!;
    public SecurityInfo Security { get; init; } = new();
}

/// <summary>
/// Token response for refresh operations
/// </summary>
public record TokenResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public DateTime RefreshedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// User information DTO
/// </summary>
public record UserDto
{
    public int Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string PhoneNumber { get; init; } = string.Empty;
    public string? ProfilePictureUrl { get; init; }
    public bool EmailConfirmed { get; init; }
    public DateTime LastPasswordChangeDate { get; init; }
    public List<string> Roles { get; init; } = new();
}

/// <summary>
/// Security information for login response
/// </summary>
public record SecurityInfo
{
    public DateTime LastLogin { get; init; } = DateTime.UtcNow;
    public bool IsPasswordExpired { get; init; } = false;
    public bool RequiresTwoFactor { get; init; } = false;
    public int DaysUntilPasswordExpiry { get; init; } = -1;
}

#endregion