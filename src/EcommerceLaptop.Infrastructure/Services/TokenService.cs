using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.Infrastructure.Data;

namespace EcommerceLaptop.Infrastructure.Services;

/// <summary>
/// Token service implementation that consolidates customer and admin token management
/// Provides context-aware token generation with enhanced security features
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpiryMinutes;
    private readonly int _refreshTokenExpiryDays;

    public TokenService(IConfiguration configuration, ApplicationDbContext context)
    {
        _configuration = configuration;
        _context = context;
        _secretKey = configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        _issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
        _audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");
        _accessTokenExpiryMinutes = int.Parse(configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15");
        _refreshTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryDays"] ?? "7");
    }

    public async Task<(string accessToken, RefreshToken refreshToken)> GenerateTokensAsync(User user, AuthContext context)
    {
        // Determine user type based on context
        var userType = context == AuthContext.Admin ? "Admin" : "Customer";

        // Get user roles and permissions
        var roles = await GetUserRolesAsync(user.Id, context);
        var permissions = context == AuthContext.Admin ? await GetUserPermissionsAsync(user.Id) : null;

        // Generate access token
        var accessToken = await GenerateAccessTokenAsync(user, userType, roles, permissions);

        // Generate refresh token
        var refreshToken = await GenerateRefreshTokenAsync(user.Id, context);

        return (accessToken, refreshToken);
    }

    public async Task<(string? accessToken, RefreshToken? refreshToken)> RefreshTokenAsync(string refreshToken, AuthContext context)
    {
        try
        {
            // Validate current refresh token
            var currentToken = await ValidateRefreshTokenAsync(refreshToken);
            if (currentToken == null)
                return (null, null);

            // Get user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentToken.UserId);
            if (user == null)
                return (null, null);

            // Revoke old token
            currentToken.RevokedAt = DateTime.UtcNow;
            currentToken.ReplacedByToken = Guid.NewGuid().ToString(); // Will be updated with new token

            // Generate new tokens
            var (newAccessToken, newRefreshToken) = await GenerateTokensAsync(user, context);

            // Update replaced token reference
            currentToken.ReplacedByToken = newRefreshToken.Token;

            await _context.SaveChangesAsync();

            return (newAccessToken, newRefreshToken);
        }
        catch
        {
            return (null, null);
        }
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        try
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

            if (token != null)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedReason = "Revoked by user";
                await _context.SaveChangesAsync();
            }
        }
        catch
        {
            // Log error but don't throw - revocation should be graceful
        }
    }

    private Task<string> GenerateAccessTokenAsync(User user, string userType, IEnumerable<string> roles, IEnumerable<AdminPermissionDto>? permissions)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new("user_type", userType),
            new("jti", Guid.NewGuid().ToString()),
            new("iat", new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add permission claims for admin users
        if (userType == "Admin" && permissions != null)
        {
            // Add the is_admin claim required by RequireAdmin attribute
            claims.Add(new Claim("is_admin", "true"));
            
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission.Name));
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return Task.FromResult(tokenHandler.WriteToken(token));
    }

    private async Task<RefreshToken> GenerateRefreshTokenAsync(int userId, AuthContext context)
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        var refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(randomNumber),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = "System" // Will be updated with actual IP in higher-level services
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken;
    }

    private async Task<RefreshToken?> ValidateRefreshTokenAsync(string refreshToken)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken &&
                                       !rt.IsRevoked &&
                                       rt.ExpiresAt > DateTime.UtcNow);
    }

    private async Task<IEnumerable<string>> GetUserRolesAsync(int userId, AuthContext context)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.UserRoles == null)
            return Enumerable.Empty<string>();

        // Filter roles based on context
        return user.UserRoles
            .Where(ur => context == AuthContext.Admin ? ur.Role.IsAdminRole : !ur.Role.IsAdminRole)
            .Select(ur => ur.Role.Name);
    }

    private async Task<IEnumerable<AdminPermissionDto>> GetUserPermissionsAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId);

        Console.WriteLine($" TOKEN DEBUG - Getting permissions for user ID: {userId}");
        Console.WriteLine($" TOKEN DEBUG - User found: {user?.Email}");
        Console.WriteLine($" TOKEN DEBUG - User roles count: {user?.UserRoles?.Count ?? 0}");

        if (user?.UserRoles == null)
        {
            Console.WriteLine($" TOKEN DEBUG - No user or user roles found for ID: {userId}");
            return Enumerable.Empty<AdminPermissionDto>();
        }

        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => new AdminPermissionDto
            {
                Id = rp.Permission.Id,
                Name = rp.Permission.Name,
                Description = rp.Permission.Description,
                Module = rp.Permission.Module,
                Action = rp.Permission.Action
            })
            .Distinct()
            .ToList();

        Console.WriteLine($" TOKEN DEBUG - Permissions found: {permissions.Count}");
        foreach (var perm in permissions.Take(5))
        {
            Console.WriteLine($" TOKEN DEBUG - Permission: {perm.Name}");
        }

        return permissions;
    }
}
