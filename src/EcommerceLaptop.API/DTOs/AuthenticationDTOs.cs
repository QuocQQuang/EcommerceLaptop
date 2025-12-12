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
    [Required(ErrorMessage = "Email l bt buc")]
    [EmailAddress(ErrorMessage = "nh dng email khng hp l")]
    [StringLength(255, ErrorMessage = "Email khng c vt qu 255 k t")]
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
    [Required(ErrorMessage = "Email l bt buc")]
    [EmailAddress(ErrorMessage = "nh dng email khng hp l")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Password reset token from email
    /// </summary>
    [Required(ErrorMessage = "Token l bt buc")]
    [StringLength(128, MinimumLength = 10, ErrorMessage = "Token khng hp l")]
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// New password
    /// </summary>
    [Required(ErrorMessage = "Mt khu mi l bt buc")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mt khu phi c t nht 8 k t v khng qu 100 k t")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Mt khu phi cha t nht 1 ch hoa, 1 ch thng, 1 s v 1 k t c bit")]
    public string NewPassword { get; init; } = string.Empty;

    /// <summary>
    /// Confirm new password
    /// </summary>
    [Required(ErrorMessage = "Xc nhn mt khu l bt buc")]
    [Compare("NewPassword", ErrorMessage = "Mt khu xc nhn khng khp")]
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
    [Required(ErrorMessage = "Email l bt buc")]
    [EmailAddress(ErrorMessage = "nh dng email khng hp l")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Password reset token to validate
    /// </summary>
    [Required(ErrorMessage = "Token l bt buc")]
    [StringLength(128, MinimumLength = 10, ErrorMessage = "Token khng hp l")]
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
    [Required(ErrorMessage = "Email l bt buc")]
    [EmailAddress(ErrorMessage = "nh dng email khng hp l")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Mt khu l bt buc")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Mt khu khng hp l")]
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
    [Required(ErrorMessage = "Email l bt buc")]
    [EmailAddress(ErrorMessage = "nh dng email khng hp l")]
    [StringLength(255, ErrorMessage = "Email khng c vt qu 255 k t")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Mt khu l bt buc")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mt khu phi c t nht 8 k t v khng qu 100 k t")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Mt khu phi cha t nht 1 ch hoa, 1 ch thng, 1 s v 1 k t c bit")]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "Xc nhn mt khu l bt buc")]
    [Compare("Password", ErrorMessage = "Mt khu xc nhn khng khp")]
    public string ConfirmPassword { get; init; } = string.Empty;

    [Required(ErrorMessage = "Tn l bt buc")]
    [StringLength(100, ErrorMessage = "Tn khng c vt qu 100 k t")]
    public string FirstName { get; init; } = string.Empty;

    [Required(ErrorMessage = "H l bt buc")]
    [StringLength(100, ErrorMessage = "H khng c vt qu 100 k t")]
    public string LastName { get; init; } = string.Empty;

    [Phone(ErrorMessage = "S in thoi khng hp l")]
    [StringLength(20, ErrorMessage = "S in thoi khng c vt qu 20 k t")]
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Accept terms and conditions
    /// </summary>
    [Range(typeof(bool), "true", "true", ErrorMessage = "Bn phi ng  vi iu khon v iu kin")]
    public bool AcceptTerms { get; init; } = false;
}


/// <summary>
/// Refresh token request
/// </summary>
public record RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token l bt buc")]
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