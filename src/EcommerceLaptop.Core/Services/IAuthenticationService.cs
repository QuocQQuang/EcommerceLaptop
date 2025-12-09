using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Services;

/// <summary>
/// Authentication service interface implementing enterprise security patterns
/// Supports multiple authentication methods with comprehensive security features
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates user with email and password
    /// </summary>
    /// <param name="email">User email address</param>
    /// <param name="password">Plain text password</param>
    /// <returns>Authentication result with user and tokens</returns>
    Task<AuthenticationResult> AuthenticateAsync(string email, string password);

    /// <summary>
    /// Registers a new user account
    /// </summary>
    /// <param name="request">Registration request with user details</param>
    /// <returns>Registration result with user and tokens</returns>
    Task<AuthenticationResult> RegisterAsync(UserRegistrationRequest request);

    /// <summary>
    /// Refreshes access token using refresh token
    /// </summary>
    /// <param name="refreshToken">Valid refresh token</param>
    /// <returns>New access token and refresh token pair</returns>
    Task<AuthenticationResult> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Revokes refresh token (logout)
    /// </summary>
    /// <param name="refreshToken">Refresh token to revoke</param>
    /// <returns>True if successfully revoked</returns>
    Task<bool> RevokeTokenAsync(string refreshToken);

    /// <summary>
    /// Initiates password reset process
    /// </summary>
    /// <param name="email">User email address</param>
    /// <returns>True if reset email sent</returns>
    Task<bool> ForgotPasswordAsync(string email);

    /// <summary>
    /// Resets password using reset token
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="token">Password reset token</param>
    /// <param name="newPassword">New password</param>
    /// <returns>True if password reset successful</returns>
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword);

    /// <summary>
    /// Changes user password (authenticated user)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="currentPassword">Current password</param>
    /// <param name="newPassword">New password</param>
    /// <returns>True if password changed successfully</returns>
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

    /// <summary>
    /// Validates user email address
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="token">Email validation token</param>
    /// <returns>True if email validated successfully</returns>
    Task<bool> ValidateEmailAsync(string email, string token);
}

/// <summary>
/// Authentication result containing user information and tokens
/// </summary>
public class AuthenticationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public User? User { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime AccessTokenExpiry { get; set; }
    public IEnumerable<string> Roles { get; set; } = new List<string>();
}

/// <summary>
/// User registration request model
/// </summary>
public class UserRegistrationRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}
