using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.API.DTOs;

#region Password Reset DTOs

/// <summary>
/// Request DTO for initiating password reset
/// </summary>
public class ForgotPasswordRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    [Required(ErrorMessage = "Email l bt buc")]
    [EmailAddress(ErrorMessage = "nh dng email khng hp l")]
    [StringLength(255, ErrorMessage = "Email khng c vt qu 255 k t")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for forgot password request
/// </summary>
public class ForgotPasswordResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Request DTO for password reset confirmation
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    [Required(ErrorMessage = "Email l bt buc")]
    [EmailAddress(ErrorMessage = "nh dng email khng hp l")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password reset token from email
    /// </summary>
    [Required(ErrorMessage = "Token l bt buc")]
    [StringLength(128, MinimumLength = 10, ErrorMessage = "Token khng hp l")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// New password
    /// </summary>
    [Required(ErrorMessage = "Mt khu mi l bt buc")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mt khu phi c t nht 8 k t v khng qu 100 k t")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Mt khu phi cha t nht 1 ch hoa, 1 ch thng, 1 s v 1 k t c bit")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirm new password
    /// </summary>
    [Required(ErrorMessage = "Xc nhn mt khu l bt buc")]
    [Compare("NewPassword", ErrorMessage = "Mt khu xc nhn khng khp")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for password reset confirmation
/// </summary>
public class ResetPasswordResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ResetAt { get; set; } = DateTime.UtcNow;
    public bool RequiresLogin { get; set; } = true;
}

/// <summary>
/// Request DTO for validating password reset token
/// </summary>
public class ValidateResetTokenRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    [Required(ErrorMessage = "Email l bt buc")]
    [EmailAddress(ErrorMessage = "nh dng email khng hp l")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password reset token to validate
    /// </summary>
    [Required(ErrorMessage = "Token l bt buc")]
    [StringLength(128, MinimumLength = 10, ErrorMessage = "Token khng hp l")]
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for token validation
/// </summary>
public class ValidateResetTokenResponse
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public int? MinutesRemaining { get; set; }
}

#endregion

#region Enhanced Authentication DTOs

/// <summary>
/// Enhanced login request with additional security features
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Email l bt buc")]
    [EmailAddress(ErrorMessage = "nh dng email khng hp l")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mt khu l bt buc")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Mt khu khng hp l")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Remember user login for extended session
    /// </summary>
    public bool RememberMe { get; set; } = false;

    /// <summary>
    /// Client's device fingerprint for security
    /// </summary>
    public string? DeviceFingerprint { get; set; }
}

/// <summary>
/// Enhanced registration request
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "Email l bt buc")]
    [EmailAddress(ErrorMessage = "nh dng email khng hp l")]
    [StringLength(255, ErrorMessage = "Email khng c vt qu 255 k t")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mt khu l bt buc")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mt khu phi c t nht 8 k t v khng qu 100 k t")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Mt khu phi cha t nht 1 ch hoa, 1 ch thng, 1 s v 1 k t c bit")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Xc nhn mt khu l bt buc")]
    [Compare("Password", ErrorMessage = "Mt khu xc nhn khng khp")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tn l bt buc")]
    [StringLength(100, ErrorMessage = "Tn khng c vt qu 100 k t")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "H l bt buc")]
    [StringLength(100, ErrorMessage = "H khng c vt qu 100 k t")]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "S in thoi khng hp l")]
    [StringLength(20, ErrorMessage = "S in thoi khng c vt qu 20 k t")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Accept terms and conditions
    /// </summary>
    [Range(typeof(bool), "true", "true", ErrorMessage = "Bn phi ng  vi iu khon v iu kin")]
    public bool AcceptTerms { get; set; } = false;
}


/// <summary>
/// Refresh token request
/// </summary>
public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token l bt buc")]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Enhanced login response with security information
/// </summary>
public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
    public SecurityInfo Security { get; set; } = new();
}

/// <summary>
/// Token response for refresh operations
/// </summary>
public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime RefreshedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// User information DTO
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string PhoneNumber { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime LastPasswordChangeDate { get; set; }
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Security information for login response
/// </summary>
public class SecurityInfo
{
    public DateTime LastLogin { get; set; } = DateTime.UtcNow;
    public bool IsPasswordExpired { get; set; } = false;
    public bool RequiresTwoFactor { get; set; } = false;
    public int DaysUntilPasswordExpiry { get; set; } = -1;
}

#endregion