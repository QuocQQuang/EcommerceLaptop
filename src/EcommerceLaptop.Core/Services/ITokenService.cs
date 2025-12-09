using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.DTOs;

namespace EcommerceLaptop.Core.Services;

/// <summary>
/// Unified token service interface
/// Consolidates ITokenService and IAdminTokenService
/// </summary>
public interface ITokenService
{
    Task<(string accessToken, RefreshToken refreshToken)> GenerateTokensAsync(User user, AuthContext context);
    Task<(string? accessToken, RefreshToken? refreshToken)> RefreshTokenAsync(string refreshToken, AuthContext context);
    Task RevokeTokenAsync(string refreshToken);
}
