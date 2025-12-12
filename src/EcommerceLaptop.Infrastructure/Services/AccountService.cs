using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Data;

namespace EcommerceLaptop.Infrastructure.Services;

public class AccountService : IAccountService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountService> _logger;

    public AccountService(ApplicationDbContext context, ILogger<AccountService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UnifiedUserDto?> GetUserProfileAsync(int userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null) return null;

            var permissions = new List<AdminPermissionDto>();
            var isAdmin = user.UserRoles.Any(ur => ur.Role.IsAdminRole);
            if (isAdmin)
            {
                permissions = user.UserRoles
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => new AdminPermissionDto
                    {
                        Id = rp.Permission.Id,
                        Name = rp.Permission.Name,
                        Description = rp.Permission.Description,
                        Module = rp.Permission.Module,
                        Action = rp.Permission.Action
                    }).ToList();
            }

            return MapToUnifiedUserDto(user, permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile: {UserId}", userId);
            return null;
        }
    }

    public async Task<UnifiedUserDto?> UpdateUserProfileAsync(int userId, UpdateProfileRequest updateRequest)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            if (!string.IsNullOrEmpty(updateRequest.FirstName)) user.FirstName = updateRequest.FirstName;
            if (!string.IsNullOrEmpty(updateRequest.LastName)) user.LastName = updateRequest.LastName;
            if (!string.IsNullOrEmpty(updateRequest.PhoneNumber)) user.PhoneNumber = updateRequest.PhoneNumber;
            if (!string.IsNullOrEmpty(updateRequest.Avatar)) user.ProfilePictureUrl = updateRequest.Avatar;
            if (!string.IsNullOrEmpty(updateRequest.Notes)) user.Notes = updateRequest.Notes;

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetUserProfileAsync(userId);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash)) return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.LastPasswordChangeDate = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(UnifiedChangePasswordRequest request)
    {
        try
        {
            if (request.NewPassword != request.ConfirmPassword)
            {
                return (false, "Passwords do not match");
            }

            // This would need user ID from JWT token claims
            // For now, delegate to existing method which requires HttpContext (not available here directly unless passed)
            // The original AuthService stubbed this out mostly.
            await Task.CompletedTask;
            return (false, "Change password requires user ID from authentication context");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " Change password failed");
            return (false, "Failed to change password.");
        }
    }

    public async Task<IEnumerable<string>> GetUserRolesAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user?.UserRoles?.Select(ur => ur.Role.Name) ?? Enumerable.Empty<string>();
    }

    public async Task<bool> AssignRoleAsync(int userId, string roleName)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);

            if (user == null || role == null) return false;

            var existingAssignment = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);

            if (existingAssignment != null) return true;

            var userRole = new UserRole { UserId = userId, RoleId = role.Id };
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RemoveRoleAsync(int userId, string roleName)
    {
        try
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null) return false;

            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);

            if (userRole == null) return true;

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<UnifiedUserDto?> CreateUserAsync(string email, string password, string firstName, string lastName,
        UserType userType, IEnumerable<string> roles, string? phoneNumber = null)
    {
        try
        {
            if (await _context.Users.AnyAsync(u => u.Email == email)) return null;

            var user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber ?? string.Empty,
                UserType = userType.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
                EmailConfirmed = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            foreach (var roleName in roles)
            {
                await AssignRoleAsync(user.Id, roleName);
            }

            return await GetUserProfileAsync(user.Id);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeactivateUserAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedReason = "User deactivated";
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int?> MapAdminUserIdToUnifiedUserIdAsync(int adminUserId)
    {
        await Task.CompletedTask;
        return null; // Legacy deprecated
    }

    private UnifiedUserDto MapToUnifiedUserDto(User user, List<AdminPermissionDto> permissions)
    {
        return new UnifiedUserDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.ProfilePictureUrl,
            user.IsActive,
            user.EmailConfirmed,
            user.CreatedAt,
            user.LastLoginAt,
            user.LastLoginIP,
            user.UserRoles.Any(ur => ur.Role.IsAdminRole) ? UserType.Admin : UserType.Customer,
            user.UserRoles.Select(ur => new RoleDto(
                ur.Role.Id,
                ur.Role.Name,
                ur.Role.Description,
                ur.Role.IsAdminRole,
                new List<AdminPermissionDto>()
            )).ToList(),
            permissions,
            user.FailedLoginAttempts,
            user.LockedUntil,
            user.Notes
        );
    }
}
