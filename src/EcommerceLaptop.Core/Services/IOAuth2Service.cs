using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Services;

/// <summary>
/// OAuth2 integration service for social authentication
/// Supports Google and Facebook OAuth 2.0 flows
/// </summary>
public interface IOAuth2Service
{
    /// <summary>
    /// Generates OAuth2 authorization URL for provider
    /// </summary>
    /// <param name="provider">OAuth provider (Google, Facebook)</param>
    /// <param name="state">CSRF protection state parameter</param>
    /// <returns>Authorization URL for redirection</returns>
    Task<string> GetAuthorizationUrlAsync(OAuth2Provider provider, string state);

    /// <summary>
    /// Handles OAuth2 callback and exchanges code for tokens
    /// </summary>
    /// <param name="provider">OAuth provider</param>
    /// <param name="code">Authorization code from callback</param>
    /// <param name="state">State parameter for CSRF validation</param>
    /// <returns>Authentication result with user information</returns>
    Task<AuthenticationResult> HandleCallbackAsync(OAuth2Provider provider, string code, string state);

    /// <summary>
    /// Links existing user account with OAuth2 provider
    /// </summary>
    /// <param name="userId">Existing user ID</param>
    /// <param name="provider">OAuth provider</param>
    /// <param name="providerUserId">Provider's user ID</param>
    /// <returns>True if account linked successfully</returns>
    Task<bool> LinkAccountAsync(int userId, OAuth2Provider provider, string providerUserId);

    /// <summary>
    /// Unlinks OAuth2 provider from user account
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="provider">OAuth provider to unlink</param>
    /// <returns>True if account unlinked successfully</returns>
    Task<bool> UnlinkAccountAsync(int userId, OAuth2Provider provider);
}

/// <summary>
/// Supported OAuth2 providers
/// </summary>
public enum OAuth2Provider
{
    Google,
    Facebook
}

/// <summary>
/// OAuth2 user information from provider
/// </summary>
public class OAuth2UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public OAuth2Provider Provider { get; set; }
}
