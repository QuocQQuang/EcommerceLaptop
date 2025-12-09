using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Services.Security;
using EcommerceLaptop.Infrastructure.Data;
using BCrypt.Net;
using System.Text.Json;

namespace EcommerceLaptop.Infrastructure.Services;

/// <summary>
/// Authentication service implementation
/// Consolidates customer and admin authentication while preserving existing business logic
/// Strategy: Context-aware authentication with backward compatibility
/// </summary>
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;
    private readonly IPasswordResetService _passwordResetService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ISecurityEventService _securityEventService;
    private readonly IIPBlockingService _ipBlockingService;

    public AuthService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IPasswordResetService passwordResetService,
        IEmailService emailService,
        ISecurityEventService securityEventService,
        IIPBlockingService ipBlockingService)
    {
        _context = context;
        _tokenService = new TokenService(configuration, context);
        _logger = logger;
        _passwordResetService = passwordResetService;
        _emailService = emailService;
        _configuration = configuration;
        _securityEventService = securityEventService;
        _ipBlockingService = ipBlockingService;
    }

    // =====================================================
    // Core Authentication Methods
    // =====================================================

    public async Task<UnifiedAuthResult?> AuthenticateAsync(string email, string password, AuthContext context, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            // Check IP block before any processing
            if (!string.IsNullOrEmpty(ipAddress))
            {
                var ipCheck = await _ipBlockingService.IsIPBlockedAsync(ipAddress);
                if (ipCheck.IsSuccess && ipCheck.Data == true)
                {
                    await _securityEventService.LogEventAsync(
                        "ip_login_attempt_blocked",
                        $"Login attempt blocked due to IP rule: {ipAddress}",
                        userId: null,
                        adminUserId: null,
                        ipAddress: ipAddress,
                        userAgent: userAgent,
                        correlationId: null,
                        metadata: new Dictionary<string, object>
                        {
                            ["ipAddress"] = ipAddress,
                            ["reason"] = "blocked_by_ip_rule"
                        }
                    );
                    _logger.LogWarning(" AUTH BLOCKED - IP blocked: {IP}", ipAddress);
                    return new UnifiedAuthResult
                    {
                        Success = false,
                        ErrorMessage = "a ch IP ca bn ang b kha tm thi do ng nhp sai qu nhiu. Vui lng th li sau.",
                        Context = context
                    };
                }
            }

            // Find user in users table with context filtering
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);

            if (user == null || !IsValidUserTypeForContext(user, context))
            {
                _logger.LogWarning(" AUTH FAILED - Invalid user or context: {Email} | Context: {Context}",
                    email, context);
                return new UnifiedAuthResult
                {
                    Success = false,
                    ErrorMessage = "Invalid email or password",
                    Context = context
                };
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                await HandleFailedLogin(user, ipAddress, userAgent);
                _logger.LogWarning(" AUTH FAILED - Invalid password: {Email} | Context: {Context}",
                    email, context);

                return new UnifiedAuthResult
                {
                    Success = false,
                    ErrorMessage = "Invalid email or password",
                    Context = context
                };
            }

            // Phase 1: Account locking will be implemented after database migration
            // TODO: Phase 2 - Check if account is locked (for admin users)
            // After migration adds: UserType, LockedUntil properties
            /*
            if (user.UserType == (int)UserType.Admin && user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
            {
                _logger.LogWarning(" AUTH FAILED - Account locked: {Email} | LockedUntil: {LockedUntil}", 
                    email, user.LockedUntil);
                
                return new UnifiedAuthResult 
                { 
                    Success = false, 
                    ErrorMessage = "Account is temporarily locked",
                    Context = context
                };
            }
            */

            // Generate tokens
            (string accessToken, RefreshToken refreshToken) = await _tokenService.GenerateTokensAsync(user, context);

            // Update login information
            await UpdateSuccessfulLogin(user, ipAddress, userAgent);

            // Build permissions (for admin users)
            var permissions = new List<AdminPermissionDto>();

            // Phase 1: Determine admin status from roles instead of UserType property
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

            // Build user DTO
            var unifiedUser = MapToUnifiedUserDto(user, permissions);

            // Record audit log for admin users
            if (isAdmin)
            {
                await RecordAuditLogAsync(user.Id, "Login", "User", user.Id.ToString(),
                    "Successful login", null, null, ipAddress, userAgent);
            }

            _logger.LogInformation(" AUTH SUCCESS - {Email} | Context: {Context} | UserType: {UserType}",
                email, context, isAdmin ? "Admin" : "Customer");

            return new UnifiedAuthResult
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30), // Match token configuration
                ExpiresIn = 30 * 60,
                User = unifiedUser,
                Context = context
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " AUTH ERROR - {Email} | Context: {Context}", email, context);
            return new UnifiedAuthResult
            {
                Success = false,
                ErrorMessage = "An error occurred during authentication",
                Context = context
            };
        }
    }

    public async Task<UnifiedRefreshResult?> RefreshTokenAsync(string refreshToken, AuthContext context, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            _logger.LogInformation(" REFRESH - Refreshing token for context: {Context}", context);

            var (newAccessToken, newRefreshToken) = await _tokenService.RefreshTokenAsync(refreshToken, context);

            if (newAccessToken == null || newRefreshToken == null)
            {
                _logger.LogWarning(" UNIFIED REFRESH FAILED - Invalid refresh token | Context: {Context}", context);
                return new UnifiedRefreshResult
                {
                    Success = false,
                    ErrorMessage = "Invalid refresh token"
                };
            }



            return new UnifiedRefreshResult
            {
                Success = true,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                ExpiresIn = 30 * 60
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " UNIFIED REFRESH ERROR - Context: {Context}", context);
            return new UnifiedRefreshResult
            {
                Success = false,
                ErrorMessage = "An error occurred during token refresh"
            };
        }
    }

    public async Task<bool> LogoutAsync(int userId, AuthContext context, string? refreshToken = null)
    {
        try
        {
            _logger.LogInformation(" UNIFIED LOGOUT - UserId: {UserId} | Context: {Context}", userId, context);

            // Revoke refresh token if provided
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _tokenService.RevokeTokenAsync(refreshToken);
            }

            // Record audit log for admin users
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var isAdmin = user?.UserRoles.Any(ur => ur.Role.IsAdminRole) == true;
            if (isAdmin)
            {
                await RecordAuditLogAsync(userId, "Logout", "User", userId.ToString(),
                    "User logout", null, null, null, null);
            }


            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " UNIFIED LOGOUT ERROR - UserId: {UserId}", userId);
            return false;
        }
    }

    // =====================================================
    // Permission Management
    // =====================================================

    public async Task<bool> HasPermissionAsync(int userId, string permission)
    {
        try
        {
            return await _context.Users
                .Where(u => u.Id == userId && u.IsActive)
                .SelectMany(u => u.UserRoles)
                .SelectMany(ur => ur.Role.RolePermissions)
                .AnyAsync(rp => rp.Permission.Name == permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission} for user: {UserId}", permission, userId);
            return false;
        }
    }

    public async Task<IEnumerable<AdminPermissionDto>> GetUserPermissionsAsync(int userId)
    {
        try
        {
            var permissions = await _context.Users
                .Where(u => u.Id == userId && u.IsActive)
                .SelectMany(u => u.UserRoles)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => new AdminPermissionDto
                {
                    Id = rp.Permission.Id,
                    Name = rp.Permission.Name,
                    Description = rp.Permission.Description,
                    Module = rp.Permission.Module,
                    Action = rp.Permission.Action
                })
                .ToListAsync();

            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for user: {UserId}", userId);
            return Enumerable.Empty<AdminPermissionDto>();
        }
    }

    public async Task<IEnumerable<AdminPermissionDto>> GetAllPermissionsAsync()
    {
        try
        {
            var permissions = await _context.Permissions
                .OrderBy(p => p.Name)
                .Select(p => new AdminPermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Module = p.Module,
                    Action = p.Action
                })
                .ToListAsync();

            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading all permissions");
            throw;
        }
    }

    public async Task<Dictionary<string, IEnumerable<AdminPermissionDto>>> GetRolePermissionMappingsAsync()
    {
        try
        {
            var rolePermissions = await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .ToDictionaryAsync(
                    role => role.Name,
                    role => role.RolePermissions
                        .Select(rp => new AdminPermissionDto
                        {
                            Id = rp.Permission.Id,
                            Name = rp.Permission.Name,
                            Description = rp.Permission.Description,
                            Module = rp.Permission.Module,
                            Action = rp.Permission.Action
                        }).AsEnumerable()
                );

            return rolePermissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading role permission mappings");
            throw;
        }
    }

    // =====================================================
    // User Profile Management
    // =====================================================

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

            // Build permissions (for admin users)
            var permissions = new List<AdminPermissionDto>();

            // Phase 1: Determine admin status from roles instead of UserType property
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

    // =====================================================
    // Helper Methods
    // =====================================================

    private bool IsValidUserTypeForContext(User user, AuthContext context)
    {
        // Phase 1: Determine user type from roles since UserType property doesn't exist yet
        // Quick compatibility: treat roles explicitly marked IsAdminRole OR with name containing "admin" as admin.
        var isAdmin = user.UserRoles.Any(ur => ur.Role.IsAdminRole ||
                                                (!string.IsNullOrEmpty(ur.Role.Name) &&
                                                 ur.Role.Name.ToLower().Contains("admin")));

        return context switch
        {
            AuthContext.Customer => !isAdmin, // Non-admin users are customers
            AuthContext.Admin => isAdmin, // Users with admin roles
            AuthContext.Employee => isAdmin, // For now, map to admin
            AuthContext.Partner => !isAdmin, // For now, map to customer
            AuthContext.API => true, // API context can authenticate any user type
            AuthContext.Mobile => !isAdmin, // Mobile typically for customers
            _ => false
        };
    }

    private UnifiedUserDto MapToUnifiedUserDto(User user, List<AdminPermissionDto> permissions)
    {
        return new UnifiedUserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Avatar = user.ProfilePictureUrl, // Use existing ProfilePictureUrl
            IsActive = user.IsActive,
            IsEmailVerified = user.EmailConfirmed,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            LastLoginIP = user.LastLoginIP,
            UserType = user.UserRoles.Any(ur => ur.Role.IsAdminRole) ? UserType.Admin : UserType.Customer, // Determine from roles in Phase 1
            Roles = user.UserRoles.Select(ur => new RoleDto
            {
                Id = ur.Role.Id,
                Name = ur.Role.Name,
                Description = ur.Role.Description,
                IsAdminRole = ur.Role.IsAdminRole
            }).ToList(),
            Permissions = permissions,
            FailedLoginAttempts = user.FailedLoginAttempts,
            LockedUntil = user.LockedUntil,
            Notes = user.Notes
        };
    }

    private async Task HandleFailedLogin(User user, string? ipAddress, string? userAgent)
    {
        if (string.IsNullOrEmpty(ipAddress)) ipAddress = "Unknown";
        if (string.IsNullOrEmpty(userAgent)) userAgent = "Unknown";

        var email = user.Email;

        // Check for sequential patterns
        var isSequentialEmail = IsSequentialString(email);
        var isSequentialPassword = false; // Password not available here, but can check in controller if needed

        // Log failure event
        await _securityEventService.LogEventAsync(
            eventType: "login_failure",
            description: $"Failed login attempt for email: {email}",
            userId: user.Id,
            adminUserId: null,
            ipAddress: ipAddress,
            userAgent: userAgent,
            correlationId: null,
            metadata: new Dictionary<string, object>
            {
                ["email"] = email,
                ["isSequentialEmail"] = isSequentialEmail,
                ["isSequentialPassword"] = isSequentialPassword,
                ["userId"] = user.Id
            }
        );

        // Increment failed attempts and optionally block IP
        user.FailedLoginAttempts += 1;
        user.UpdatedAt = DateTime.UtcNow;

        if (user.FailedLoginAttempts >= 5 && ipAddress != "Unknown")
        {
            await _ipBlockingService.BlockIPAsync(ipAddress, "Too many failed login attempts (>=5)", expiresAt: DateTime.UtcNow.AddMinutes(15));
            await _securityEventService.LogEventAsync(
                "ip_blocked_due_to_failed_attempts",
                $"IP {ipAddress} blocked after {user.FailedLoginAttempts} failed login attempts",
                userId: user.Id,
                adminUserId: null,
                ipAddress: ipAddress,
                userAgent: userAgent,
                correlationId: null,
                metadata: new Dictionary<string, object>
                {
                    ["email"] = email,
                    ["failedAttempts"] = user.FailedLoginAttempts,
                    ["threshold"] = 5,
                    ["lockDurationMinutes"] = 15
                }
            );
            _logger.LogWarning(" IP BLOCKED due to too many failed attempts for {Email} from {IP}", email, ipAddress);
        }

        // Check for brute force patterns
        var bruteForceDetected = await CheckForBruteForcePatternsAsync(email, ipAddress);
        if (bruteForceDetected)
        {
            // Log brute force detection
            await _securityEventService.LogEventAsync(
                eventType: "brute_force_detected",
                description: $"Brute force attack detected for email: {email} from IP: {ipAddress}",
                userId: user.Id,
                adminUserId: null,
                ipAddress: ipAddress,
                userAgent: userAgent,
                correlationId: null,
                metadata: new Dictionary<string, object>
                {
                    ["email"] = email,
                    ["ipAddress"] = ipAddress!,
                    ["trigger"] = "multiple_failures_or_patterns"
                }
            );

            // Temporarily block IP
            await _ipBlockingService.BlockIPAsync(ipAddress, "Brute force attack detected", expiresAt: DateTime.UtcNow.AddHours(1));

            _logger.LogCritical(" BRUTE FORCE DETECTED - Email: {Email} | IP: {IP} | Blocked IP for 1 hour", email, ipAddress);
        }

        await _context.SaveChangesAsync();

        _logger.LogWarning("AUTH - Failed login for user {Email} from IP {IP} | Sequential Email: {IsSequential}",
            email, ipAddress, isSequentialEmail);
    }

    private async Task UpdateSuccessfulLogin(User user, string? ipAddress, string? userAgent)
    {
        if (string.IsNullOrEmpty(ipAddress)) ipAddress = "Unknown";
        if (string.IsNullOrEmpty(userAgent)) userAgent = "Unknown";

        // Log success event
        await _securityEventService.LogEventAsync(
            eventType: "login_success",
            description: $"Successful login for email: {user.Email}",
            userId: user.Id,
            adminUserId: null,
            ipAddress: ipAddress,
            userAgent: userAgent,
            correlationId: null,
            metadata: new Dictionary<string, object>
            {
                ["email"] = user.Email,
                ["userId"] = user.Id
            }
        );

        // Update user last activity and login info
        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIP = ipAddress;
        user.FailedLoginAttempts = 0;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("AUTH - Successful login for user {Email} from IP {IP}",
            user.Email, ipAddress);
    }

    // =====================================================
    // Stub Methods (To be implemented in subsequent phases)
    // =====================================================

    public async Task<UnifiedUserDto?> UpdateUserProfileAsync(int userId, UpdateProfileRequest updateRequest)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return null;

            // Update fields if provided
            if (!string.IsNullOrEmpty(updateRequest.FirstName))
                user.FirstName = updateRequest.FirstName;

            if (!string.IsNullOrEmpty(updateRequest.LastName))
                user.LastName = updateRequest.LastName;

            if (!string.IsNullOrEmpty(updateRequest.PhoneNumber))
                user.PhoneNumber = updateRequest.PhoneNumber;

            if (!string.IsNullOrEmpty(updateRequest.Avatar))
                user.ProfilePictureUrl = updateRequest.Avatar;

            if (!string.IsNullOrEmpty(updateRequest.Notes))
                user.Notes = updateRequest.Notes;

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await RecordAuditLogAsync(userId, "UpdateProfile", "User", userId.ToString(),
                "User profile updated");

            // Return updated user profile
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
            if (user == null)
                return false;

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                return false;

            // Hash new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.LastPasswordChangeDate = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await RecordAuditLogAsync(userId, "ChangePassword", "User", userId.ToString(),
                "User changed password successfully");

            return true;
        }
        catch
        {
            return false;
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

            if (user == null || role == null)
                return false;

            // Check if role assignment already exists
            var existingAssignment = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);

            if (existingAssignment != null)
                return true; // Already assigned

            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = role.Id
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            await RecordAuditLogAsync(userId, "AssignRole", "UserRole", $"{userId}:{role.Id}",
                $"Assigned role '{roleName}' to user");

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
            if (role == null)
                return false;

            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);

            if (userRole == null)
                return true; // Not assigned, consider as success

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            await RecordAuditLogAsync(userId, "RemoveRole", "UserRole", $"{userId}:{role.Id}",
                $"Removed role '{roleName}' from user");

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task RecordAuditLogAsync(int userId, string action, string entity, string? entityId = null,
        string? details = null, string? oldValues = null, string? newValues = null,
        string? ipAddress = null, string? userAgent = null)
    {
        // Legacy AdminAuditLog system removed - now using unified audit system
        // This method is deprecated - use IAuditLoggingService instead
        await Task.CompletedTask;
    }

    public async Task SyncPermissionsAsync()
    {
        // This method would typically sync permissions from a configuration source
        // For now, ensure basic permissions exist
        var basicPermissions = new[]
        {
            new { Name = "dashboard:read", Description = "View dashboard", Module = "dashboard", Action = "read" },
            new { Name = "users:read", Description = "View users", Module = "users", Action = "read" },
            new { Name = "users:write", Description = "Create/update users", Module = "users", Action = "write" },
            new { Name = "users:delete", Description = "Delete users", Module = "users", Action = "delete" },
            new { Name = "products:read", Description = "View products", Module = "products", Action = "read" },
            new { Name = "products:write", Description = "Create/update products", Module = "products", Action = "write" },
            new { Name = "orders:read", Description = "View orders", Module = "orders", Action = "read" },
            new { Name = "orders:manage", Description = "Manage orders", Module = "orders", Action = "manage" }
        };

        foreach (var perm in basicPermissions)
        {
            var existingPermission = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Name == perm.Name);

            if (existingPermission == null)
            {
                var permission = new Permission
                {
                    Name = perm.Name,
                    Description = perm.Description,
                    Module = perm.Module,
                    Action = perm.Action,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Permissions.Add(permission);
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<UnifiedUserDto?> CreateUserAsync(string email, string password, string firstName, string lastName,
        UserType userType, IEnumerable<string> roles, string? phoneNumber = null)
    {
        try
        {
            // Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email == email))
                return null;

            // Create new user
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

            // Assign roles
            foreach (var roleName in roles)
            {
                await AssignRoleAsync(user.Id, roleName);
            }

            await RecordAuditLogAsync(user.Id, "CreateUser", "User", user.Id.ToString(),
                $"Created {userType} user: {email}");

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
            if (user == null)
                return false;

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            // Revoke all active refresh tokens for this user
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedReason = "User deactivated";
            }

            await _context.SaveChangesAsync();

            await RecordAuditLogAsync(userId, "DeactivateUser", "User", userId.ToString(),
                "User account deactivated");

            return true;
        }
        catch
        {
            return false;
        }
    }

    public UnifiedUserDto? GetCurrentUserFromStorage()
    {
        // This method would typically interact with HTTP context or session storage
        // For now, return null as this would be implemented with proper context injection
        // In a real implementation, this would access HttpContext.User claims
        return null;
    }

    public async Task<int?> MapAdminUserIdToUnifiedUserIdAsync(int adminUserId)
    {
        // Legacy migration system removed - all admin users now use unified Users table
        // This method is deprecated - admin users are directly stored in Users table
        await Task.CompletedTask;
        return null;
    }

    // =====================================================
    // Registration & Password Management Implementation
    // =====================================================

    public async Task<UnifiedAuthResult?> RegisterAsync(UnifiedRegisterRequest request)
    {
        try
        {
            // Default to Customer context if not specified
            var context = request.Context ?? AuthContext.Customer;

            // Validate passwords match
            if (request.Password != request.ConfirmPassword)
            {
                return new UnifiedAuthResult
                {
                    Success = false,
                    ErrorMessage = "Passwords do not match"
                };
            }

            // Validate terms acceptance
            if (!request.AcceptTerms)
            {
                return new UnifiedAuthResult
                {
                    Success = false,
                    ErrorMessage = "Bn phi ng  vi iu khon v iu kin"
                };
            }

            // Validate password strength including sequential check
            var passwordValidation = ValidatePasswordStrength(request.Password);
            if (!passwordValidation.IsValid)
            {
                return new UnifiedAuthResult
                {
                    Success = false,
                    ErrorMessage = passwordValidation.ErrorMessage
                };
            }

            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (existingUser != null)
            {
                return new UnifiedAuthResult
                {
                    Success = false,
                    ErrorMessage = "User with this email already exists"
                };
            }

            // Hash the password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create new user
            var user = new User
            {
                Email = request.Email,
                PasswordHash = hashedPassword,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber ?? string.Empty,
                UserType = context == AuthContext.Admin ? "Admin" : "Customer",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                EmailConfirmed = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Assign default role based on context
            var defaultRoleName = context == AuthContext.Admin ? "Admin" : "Customer";
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == defaultRoleName);

            if (role != null)
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id
                };
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
            }

            // Generate tokens
            var (accessToken, refreshToken) = await _tokenService.GenerateTokensAsync(user, context);

            // Record audit log only for admin users
            if (context == AuthContext.Admin)
            {
                await RecordAuditLogAsync(user.Id, "Register", "User", user.Id.ToString(),
                    $"New {context} user registered: {request.Email}", ipAddress: request.IpAddress, userAgent: request.UserAgent);
            }

            // Generate and send secure email confirmation token (non-blocking failure)
            try
            {
                var (tokenSuccess, confirmationToken) = await GenerateEmailConfirmationTokenAsync(
                    user.Id, user.Email, request.IpAddress, request.UserAgent);

                if (tokenSuccess && !string.IsNullOrEmpty(confirmationToken))
                {
                    var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
                    var confirmUrl = $"{frontendUrl}/confirm-email?token={Uri.EscapeDataString(confirmationToken)}";
                    _ = _emailService.SendEmailConfirmationEmailAsync(user, confirmUrl);
                    _logger.LogInformation(" Email confirmation token generated and sent for user {UserId}", user.Id);
                }
                else
                {
                    _logger.LogWarning(" Failed to generate email confirmation token for user {UserId}", user.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, " Failed to send email confirmation for {Email}", user.Email);
            }

            return new UnifiedAuthResult
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = refreshToken.ExpiresAt,
                User = new UnifiedUserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UserType = user.UserType == "Admin" ? UserType.Admin : UserType.Customer,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " Registration failed for {Email}", request.Email);
            return new UnifiedAuthResult
            {
                Success = false,
                ErrorMessage = "Registration failed"
            };
        }
    }

    public async Task<(bool Success, string Message)> ForgotPasswordAsync(UnifiedForgotPasswordRequest request)
    {
        try
        {
            // Initiate password reset using dedicated service (handles token + email)
            var result = await _passwordResetService.InitiatePasswordResetAsync(request.Email);

            // Always return generic success message to avoid user enumeration
            var message = result.Success
                ? "If your email exists in our system, you will receive password reset instructions."
                : (string.IsNullOrWhiteSpace(result.Message)
                    ? "If your email exists in our system, you will receive password reset instructions."
                    : result.Message);

            _logger.LogInformation(" Forgot password processed for {Email} | EmailSent: {EmailSent}", request.Email, result.EmailSent);
            return (true, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " Forgot password failed for {Email}", request.Email);
            return (false, "Failed to initiate password reset.");
        }
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(UnifiedResetPasswordRequest request)
    {
        try
        {
            // Validate passwords match
            if (request.NewPassword != request.ConfirmPassword)
            {
                return (false, "Mt khu xc nhn khng khp");
            }

            _logger.LogInformation(" Password reset attempt with token: {Token}", request.Token);

            // Use PasswordResetService to handle the reset
            var result = await _passwordResetService.ConfirmPasswordResetAsync(
                request.Email,
                request.Token,
                request.NewPassword,
                request.IpAddress
            );

            if (result.Success)
            {
                _logger.LogInformation(" Password reset successful for email: {Email}", request.Email);
                return (true, "Mt khu  c t li thnh cng. Bn c th ng nhp vi mt khu mi.");
            }
            else
            {
                _logger.LogWarning(" Password reset failed for email: {Email}, Error: {Error}", request.Email, result.Message);
                return (false, result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " Password reset failed for token {Token}", request.Token);
            return (false, " xy ra li khi t li mt khu. Vui lng th li sau.");
        }
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(UnifiedChangePasswordRequest request)
    {
        try
        {
            // Validate passwords match
            if (request.NewPassword != request.ConfirmPassword)
            {
                return (false, "Passwords do not match");
            }

            // This would need user ID from JWT token claims
            // For now, delegate to existing method which requires HttpContext
            await Task.CompletedTask; // Remove async warning
            return (false, "Change password requires user ID from authentication context");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " Change password failed");
            return (false, "Failed to change password.");
        }
    }

    /// <summary>
    /// Generate a secure email confirmation token for a user
    /// </summary>
    public async Task<(bool Success, string? Token)> GenerateEmailConfirmationTokenAsync(int userId, string email, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Email.ToLower() != email.ToLower())
                return (false, null);

            // If user is already confirmed, don't generate new tokens
            if (user.EmailConfirmed)
                return (false, null);

            // Invalidate any existing unused tokens for this user
            var existingTokens = await _context.EmailConfirmationTokens
                .Where(t => t.UserId == userId && !t.IsUsed)
                .ToListAsync();

            foreach (var token in existingTokens)
            {
                token.IsUsed = true;
                token.UpdatedAt = DateTime.UtcNow;
            }

            // Generate cryptographically secure token
            var confirmationToken = GenerateSecureToken();

            var emailToken = new EmailConfirmationToken
            {
                UserId = userId,
                Token = confirmationToken,
                Email = email,
                ExpiresAt = DateTime.UtcNow.AddHours(24), // 24-hour expiration
                GeneratedFromIp = ipAddress,
                GeneratedFromUserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.EmailConfirmationTokens.Add(emailToken);
            await _context.SaveChangesAsync();

            _logger.LogInformation(" Email confirmation token generated for user {UserId} from IP {IP}", userId, ipAddress);
            return (true, confirmationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " Failed to generate email confirmation token for user {UserId}", userId);
            return (false, null);
        }
    }

    /// <summary>
    /// Validate and consume an email confirmation token
    /// </summary>
    public async Task<(bool Success, string Message)> ConfirmEmailWithTokenAsync(string token, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            // Find the token
            var emailToken = await _context.EmailConfirmationTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token);

            if (emailToken == null)
            {
                _logger.LogWarning(" Invalid email confirmation token attempted from IP {IP}", ipAddress);
                return (false, "Invalid or expired confirmation token.");
            }

            // Check if token is valid
            if (!emailToken.IsValid)
            {
                _logger.LogWarning(" Expired/used email confirmation token attempted for user {UserId} from IP {IP}",
                    emailToken.UserId, ipAddress);
                return (false, "Invalid or expired confirmation token.");
            }

            // Mark token as used
            emailToken.IsUsed = true;
            emailToken.UsedAt = DateTime.UtcNow;
            emailToken.UsedFromIp = ipAddress;
            emailToken.UsedFromUserAgent = userAgent;
            emailToken.UpdatedAt = DateTime.UtcNow;

            // Confirm user's email
            var user = emailToken.User;
            if (user != null && !user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                user.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(" Email confirmed successfully for user {UserId} from IP {IP}",
                emailToken.UserId, ipAddress);

            return (true, "Email confirmed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " Failed to confirm email with token from IP {IP}", ipAddress);
            return (false, "An error occurred while confirming your email.");
        }
    }

    /// <summary>
    /// Legacy method for backward compatibility - will be deprecated
    /// </summary>
    [Obsolete("Use ConfirmEmailWithTokenAsync for secure token-based confirmation")]
    public async Task<bool> ConfirmEmailAsync(string email)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (user == null)
                return true; // Do not leak existence

            if (user.EmailConfirmed)
                return true;

            user.EmailConfirmed = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Resend email confirmation with new secure token
    /// </summary>
    public async Task<(bool Success, string Message)> ResendEmailConfirmationAsync(string email, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            // Find user by email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null)
            {
                // Don't leak user existence, return success message
                _logger.LogWarning(" Resend confirmation requested for non-existent email: {Email} from IP {IP}",
                    email, ipAddress);
                return (true, "If an account exists, a confirmation email has been sent.");
            }

            if (user.EmailConfirmed)
            {
                _logger.LogInformation(" Resend confirmation requested for already confirmed email: {Email} from IP {IP}",
                    email, ipAddress);
                return (true, "Email is already confirmed.");
            }

            // Generate new confirmation token
            var (tokenSuccess, confirmationToken) = await GenerateEmailConfirmationTokenAsync(
                user.Id, user.Email, ipAddress, userAgent);

            if (tokenSuccess && !string.IsNullOrEmpty(confirmationToken))
            {
                // Send new confirmation email
                var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
                var confirmUrl = $"{frontendUrl}/confirm-email?token={Uri.EscapeDataString(confirmationToken)}";

                _ = _emailService.SendEmailConfirmationEmailAsync(user, confirmUrl);

                _logger.LogInformation(" Email confirmation resent for user {UserId} from IP {IP}", user.Id, ipAddress);
                return (true, " gi m mi.");
            }
            else
            {
                _logger.LogError(" Failed to generate confirmation token for resend request - User: {UserId}", user.Id);
                return (false, "Failed to send confirmation email. Please try again later.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " Failed to resend email confirmation for {Email} from IP {IP}", email, ipAddress);
            return (false, "An error occurred while sending confirmation email.");
        }
    }

    /// <summary>
    /// Generate a cryptographically secure random token
    /// </summary>
    private static string GenerateSecureToken()
    {
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[32]; // 256-bit token
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    /// <summary>
    /// Validates password strength with comprehensive security checks
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <returns>Validation result</returns>
    private (bool IsValid, string ErrorMessage) ValidatePasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
            return (false, "Mt khu khng c  trng");

        if (password.Length < 8)
            return (false, "Mt khu phi c t nht 8 k t");

        if (password.Length > 128)
            return (false, "Mt khu khng c vt qu 128 k t");

        // Check for common weak patterns
        if (password.All(c => c == password[0]))
            return (false, "Mt khu khng c cha tt c k t ging nhau");

        // Check for sequential characters
        if (IsSequential(password))
            return (false, "Mt khu khng c cha chui k t lin tip");

        // Check for common passwords
        var commonPasswords = new[]
        {
            "password", "123456", "123456789", "qwerty", "abc123", "password123",
            "admin", "letmein", "welcome", "monkey", "1234567890", "dragon",
            "master", "hello", "freedom", "whatever", "qazwsx", "trustno1"
        };

        if (commonPasswords.Any(common => password.Equals(common, StringComparison.OrdinalIgnoreCase)))
            return (false, "Mt khu qu ph bin, vui lng chn mt khu mnh hn");

        // Check for at least one character from each category
        var hasLower = password.Any(c => char.IsLower(c));
        var hasUpper = password.Any(c => char.IsUpper(c));
        var hasDigit = password.Any(c => char.IsDigit(c));
        var hasSpecial = password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c));

        var categoryCount = new[] { hasLower, hasUpper, hasDigit, hasSpecial }.Count(x => x);

        if (categoryCount < 3)
            return (false, "Mt khu phi cha t nht 3 trong 4 loi: ch thng, ch hoa, s, k t c bit");

        return (true, string.Empty);
    }

    // Removed duplicate definitions of CheckForBruteForcePatternsAsync and IsSequentialString

    private async Task<bool> CheckForBruteForcePatternsAsync(string email, string ipAddress)
    {
        var timeWindow = DateTime.UtcNow.AddMinutes(-15);
        var failureEventType = "login_failure";

        // 1. Multiple failures from single IP
        var ipFailures = await _context.SecurityEvents
            .CountAsync(e => e.EventType == failureEventType &&
                            e.IPAddress == ipAddress &&
                            e.CreatedAt > timeWindow);
        if (ipFailures > 5) return true;

        // 2. Multiple identifiers from single IP
        var ipEvents = await _context.SecurityEvents
            .Where(e => e.EventType == failureEventType &&
                       e.IPAddress == ipAddress &&
                       e.CreatedAt > timeWindow)
            .ToListAsync();
        var uniqueEmailsFromIP = ipEvents
            .Select(e =>
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(e.Details);
                return dict != null && dict.ContainsKey("email") ? dict["email"]?.ToString() : null;
            })
            .Where(v => !string.IsNullOrEmpty(v))
            .Distinct()
            .Count();
        if (uniqueEmailsFromIP > 3) return true;

        // 3. Single identifier from multiple IPs
        var emailEvents = await _context.SecurityEvents
            .Where(e => e.EventType == failureEventType &&
                       e.CreatedAt > timeWindow)
            .ToListAsync();
        var uniqueIPsForEmail = emailEvents
            .Where(e =>
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(e.Details);
                var value = dict != null && dict.ContainsKey("email") ? dict["email"]?.ToString() : null;
                return value == email;
            })
            .Select(e => e.IPAddress)
            .Distinct()
            .Count();
        if (uniqueIPsForEmail > 3) return true;

        return false;
    }

    private bool IsSequentialString(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Length < 3) return false;

        // Check for sequential numbers in email (e.g., user123, test456)
        var hasDigits = false;
        for (int i = 0; i < input.Length - 2; i++)
        {
            if (char.IsDigit(input[i]) && char.IsDigit(input[i + 1]) && char.IsDigit(input[i + 2]))
            {
                hasDigits = true;
                if (input[i + 1] == input[i] + 1 && input[i + 2] == input[i] + 2) return true;
                if (input[i + 1] == input[i] - 1 && input[i + 2] == input[i] - 2) return true;
            }
        }

        // If no digits, check alphabetical sequences (less common for emails, but possible)
        if (!hasDigits)
        {
            for (int i = 0; i < input.Length - 2; i++)
            {
                if (char.IsLetter(input[i]) && char.IsLetter(input[i + 1]) && char.IsLetter(input[i + 2]))
                {
                    var c1 = char.ToLower(input[i]);
                    var c2 = char.ToLower(input[i + 1]);
                    var c3 = char.ToLower(input[i + 2]);
                    if (c2 == c1 + 1 && c3 == c1 + 2) return true;
                    if (c2 == c1 - 1 && c3 == c1 - 2) return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if password contains sequential characters
    /// </summary>
    /// <param name="password">Password to check</param>
    /// <returns>True if contains sequential characters</returns>
    private bool IsSequential(string password)
    {
        for (int i = 0; i < password.Length - 2; i++)
        {
            if (char.IsDigit(password[i]) && char.IsDigit(password[i + 1]) && char.IsDigit(password[i + 2]))
            {
                if (password[i + 1] == password[i] + 1 && password[i + 2] == password[i] + 2)
                    return true;
                if (password[i + 1] == password[i] - 1 && password[i + 2] == password[i] - 2)
                    return true;
            }
        }
        return false;
    }
}
