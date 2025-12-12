using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Infrastructure.Services.Security;
using EcommerceLaptop.Core.Interfaces.Services;
using System.Text.Json;

namespace EcommerceLaptop.Infrastructure.Services;

public class IdentityService : IIdentityService
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly ILogger<IdentityService> _logger;
    private readonly IPasswordResetService _passwordResetService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ISecurityEventService _securityEventService;
    private readonly IIPBlockingService _ipBlockingService;

    public IdentityService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<IdentityService> logger,
        IPasswordResetService passwordResetService,
        IEmailService emailService,
        ISecurityEventService securityEventService,
        IIPBlockingService ipBlockingService)
    {
        _context = context;
        _tokenService = new TokenService(configuration, context); // Assuming TokenService is not registered in DI yet as per AuthService
        _logger = logger;
        _passwordResetService = passwordResetService;
        _emailService = emailService;
        _configuration = configuration;
        _securityEventService = securityEventService;
        _ipBlockingService = ipBlockingService;
    }

    public async Task<UnifiedAuthResult?> AuthenticateAsync(string email, string password, AuthContext context, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
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
                    return new UnifiedAuthResult(
                    false,
                    "a ch IP ca bn ang b kha tm thi do ng nhp sai qu nhiu. Vui lng th li sau.",
                    null, null, default, 0, null, context
                );
            }
        }

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
            return new UnifiedAuthResult(
                false,
                "Invalid email or password",
                null, null, default, 0, null, context
            );
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            await HandleFailedLogin(user, ipAddress, userAgent);
            _logger.LogWarning(" AUTH FAILED - Invalid password: {Email} | Context: {Context}",
                email, context);

            return new UnifiedAuthResult(
                false,
                "Invalid email or password",
                null, null, default, 0, null, context
            );
        }

        (string accessToken, RefreshToken refreshToken) = await _tokenService.GenerateTokensAsync(user, context);

        await UpdateSuccessfulLogin(user, ipAddress, userAgent);

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

        var unifiedUser = MapToUnifiedUserDto(user, permissions);

        _logger.LogInformation(" AUTH SUCCESS - {Email} | Context: {Context} | UserType: {UserType}",
            email, context, isAdmin ? "Admin" : "Customer");

        return new UnifiedAuthResult(
            true,
            null,
            accessToken,
            refreshToken.Token,
            DateTime.UtcNow.AddMinutes(30),
            30 * 60,
            unifiedUser,
            context
        );
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, " AUTH ERROR - {Email} | Context: {Context}", email, context);
        return new UnifiedAuthResult(
            false,
            "An error occurred during authentication",
            null, null, default, 0, null, context
        );
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
                return new UnifiedRefreshResult(
                    false,
                    "Invalid refresh token",
                    null, null, default, 0
                );
            }

            return new UnifiedRefreshResult(
                true,
                null,
                newAccessToken,
                newRefreshToken.Token,
                DateTime.UtcNow.AddMinutes(30),
                30 * 60
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " UNIFIED REFRESH ERROR - Context: {Context}", context);
            return new UnifiedRefreshResult(
                false,
                "An error occurred during token refresh",
                null, null, default, 0
            );
        }
    }

    public async Task<bool> LogoutAsync(int userId, AuthContext context, string? refreshToken = null)
    {
        try
        {
            _logger.LogInformation(" UNIFIED LOGOUT - UserId: {UserId} | Context: {Context}", userId, context);

            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _tokenService.RevokeTokenAsync(refreshToken);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " UNIFIED LOGOUT ERROR - UserId: {UserId}", userId);
            return false;
        }
    }

    public async Task<UnifiedAuthResult?> RegisterAsync(UnifiedRegisterRequest request)
    {
        try
        {
            var context = request.Context ?? AuthContext.Customer;

            if (request.Password != request.ConfirmPassword)
            {
                return new UnifiedAuthResult(false, "Passwords do not match", null, null, default, 0, null, context);
            }

            if (!request.AcceptTerms)
            {
                return new UnifiedAuthResult(false, "Bn phi ng  vi iu khon v iu kin", null, null, default, 0, null, context);
            }

            var passwordValidation = ValidatePasswordStrength(request.Password);
            if (!passwordValidation.IsValid)
            {
                return new UnifiedAuthResult(false, passwordValidation.ErrorMessage, null, null, default, 0, null, context);
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (existingUser != null)
            {
                return new UnifiedAuthResult(false, "User with this email already exists", null, null, default, 0, null, context);
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

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

            var defaultRoleName = context == AuthContext.Admin ? "Admin" : "Customer";
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == defaultRoleName);

            if (role != null)
            {
                var userRole = new UserRole { UserId = user.Id, RoleId = role.Id };
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
            }

            var (accessToken, refreshToken) = await _tokenService.GenerateTokensAsync(user, context);

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
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, " Failed to send email confirmation for {Email}", user.Email);
            }

            return new UnifiedAuthResult(
                true,
                null,
                accessToken,
                refreshToken.Token,
                refreshToken.ExpiresAt,
                30 * 60, // Assuming standard expiry
                new UnifiedUserDto(
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.PhoneNumber,
                    user.ProfilePictureUrl,
                    user.IsActive,
                    user.EmailConfirmed,
                    user.CreatedAt,
                    null,
                    null,
                    user.UserType == "Admin" ? UserType.Admin : UserType.Customer,
                    new List<RoleDto>(),
                    new List<AdminPermissionDto>(),
                    0,
                    null,
                    null
                ),
                context
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " Registration failed for {Email}", request.Email);
            return new UnifiedAuthResult(false, "Registration failed", null, null, default, 0, null, request.Context ?? AuthContext.Customer);
        }
    }

    public async Task<(bool Success, string Message)> ForgotPasswordAsync(UnifiedForgotPasswordRequest request)
    {
        try
        {
            var result = await _passwordResetService.InitiatePasswordResetAsync(request.Email);
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
            if (request.NewPassword != request.ConfirmPassword)
            {
                return (false, "Mt khu xc nhn khng khp");
            }

            _logger.LogInformation(" Password reset attempt with token: {Token}", request.Token);

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

    public async Task<(bool Success, string? Token)> GenerateEmailConfirmationTokenAsync(int userId, string email, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Email.ToLower() != email.ToLower()) return (false, null);
            if (user.EmailConfirmed) return (false, null);

            var existingTokens = await _context.EmailConfirmationTokens
                .Where(t => t.UserId == userId && !t.IsUsed)
                .ToListAsync();

            foreach (var token in existingTokens)
            {
                token.IsUsed = true;
                token.UpdatedAt = DateTime.UtcNow;
            }

            var confirmationToken = GenerateSecureToken();
            var emailToken = new EmailConfirmationToken
            {
                UserId = userId,
                Token = confirmationToken,
                Email = email,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
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

    public async Task<(bool Success, string Message)> ConfirmEmailWithTokenAsync(string token, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            var emailToken = await _context.EmailConfirmationTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token);

            if (emailToken == null || !emailToken.IsValid)
            {
                _logger.LogWarning(" Invalid/expired email confirmation token attempted from IP {IP}", ipAddress);
                return (false, "Invalid or expired confirmation token.");
            }

            emailToken.IsUsed = true;
            emailToken.UsedAt = DateTime.UtcNow;
            emailToken.UsedFromIp = ipAddress;
            emailToken.UsedFromUserAgent = userAgent;
            emailToken.UpdatedAt = DateTime.UtcNow;

            var user = emailToken.User;
            if (user != null && !user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                user.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(" Email confirmed successfully for user {UserId} from IP {IP}", emailToken.UserId, ipAddress);
            return (true, "Email confirmed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " Failed to confirm email with token from IP {IP}", ipAddress);
            return (false, "An error occurred while confirming your email.");
        }
    }

    public async Task<(bool Success, string Message)> ResendEmailConfirmationAsync(string email, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (user == null)
            {
                _logger.LogWarning(" Resend confirmation requested for non-existent email: {Email}", email);
                return (true, "If an account exists, a confirmation email has been sent.");
            }

            if (user.EmailConfirmed)
            {
                return (true, "Email is already confirmed.");
            }

            var (tokenSuccess, confirmationToken) = await GenerateEmailConfirmationTokenAsync(user.Id, user.Email, ipAddress, userAgent);

            if (tokenSuccess && !string.IsNullOrEmpty(confirmationToken))
            {
                var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
                var confirmUrl = $"{frontendUrl}/confirm-email?token={Uri.EscapeDataString(confirmationToken)}";
                _ = _emailService.SendEmailConfirmationEmailAsync(user, confirmUrl);

                _logger.LogInformation(" Email confirmation resent for user {UserId}", user.Id);
                return (true, " gi m mi.");
            }
            else
            {
                return (false, "Failed to send confirmation email. Please try again later.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " Failed to resend email confirmation for {Email}", email);
            return (false, "An error occurred while sending confirmation email.");
        }
    }

    [Obsolete("Use ConfirmEmailWithTokenAsync for secure token-based confirmation")]
    public async Task<bool> ConfirmEmailAsync(string email)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (user == null || user.EmailConfirmed) return true;

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

    public UnifiedUserDto? GetCurrentUserFromStorage()
    {
        return null;
    }

    // Helper Methods

    private bool IsValidUserTypeForContext(User user, AuthContext context)
    {
        var isAdmin = user.UserRoles.Any(ur => ur.Role.IsAdminRole ||
                                                (!string.IsNullOrEmpty(ur.Role.Name) &&
                                                 ur.Role.Name.ToLower().Contains("admin")));
        return context switch
        {
            AuthContext.Customer => !isAdmin,
            AuthContext.Admin => isAdmin,
            AuthContext.Employee => isAdmin,
            AuthContext.Partner => !isAdmin,
            AuthContext.API => true,
            AuthContext.Mobile => !isAdmin,
            _ => false
        };
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
                new List<AdminPermissionDto>() // Permissions not loaded deep here in original
            )).ToList(),
            permissions,
            user.FailedLoginAttempts,
            user.LockedUntil,
            user.Notes
        );
    }

    private async Task HandleFailedLogin(User user, string? ipAddress, string? userAgent)
    {
        if (string.IsNullOrEmpty(ipAddress)) ipAddress = "Unknown";
        if (string.IsNullOrEmpty(userAgent)) userAgent = "Unknown";

        var email = user.Email;
        var isSequentialEmail = IsSequentialString(email);

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
                ["userId"] = user.Id
            }
        );

        user.FailedLoginAttempts += 1;
        user.UpdatedAt = DateTime.UtcNow;

        if (user.FailedLoginAttempts >= 5 && ipAddress != "Unknown")
        {
            await _ipBlockingService.BlockIPAsync(ipAddress, "Too many failed login attempts (>=5)", expiresAt: DateTime.UtcNow.AddMinutes(15));
            _logger.LogWarning(" IP BLOCKED due to too many failed attempts for {Email} from {IP}", email, ipAddress);
        }

        var bruteForceDetected = await CheckForBruteForcePatternsAsync(email, ipAddress);
        if (bruteForceDetected)
        {
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
            await _ipBlockingService.BlockIPAsync(ipAddress, "Brute force attack detected", expiresAt: DateTime.UtcNow.AddHours(1));
            _logger.LogCritical(" BRUTE FORCE DETECTED - Email: {Email} | IP: {IP} | Blocked IP for 1 hour", email, ipAddress);
        }

        await _context.SaveChangesAsync();
    }

    private async Task UpdateSuccessfulLogin(User user, string? ipAddress, string? userAgent)
    {
        if (string.IsNullOrEmpty(ipAddress)) ipAddress = "Unknown";
        if (string.IsNullOrEmpty(userAgent)) userAgent = "Unknown";

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

        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIP = ipAddress;
        user.FailedLoginAttempts = 0;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("AUTH - Successful login for user {Email} from IP {IP}", user.Email, ipAddress);
    }

    private static string GenerateSecureToken()
    {
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private (bool IsValid, string ErrorMessage) ValidatePasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password)) return (false, "Mt khu khng c  trng");
        if (password.Length < 8) return (false, "Mt khu phi c t nht 8 k t");
        if (password.Length > 128) return (false, "Mt khu khng c vt qu 128 k t");
        if (password.All(c => c == password[0])) return (false, "Mt khu khng c cha tt c k t ging nhau");
        if (IsSequential(password)) return (false, "Mt khu khng c cha chui k t lin tip");

        var commonPasswords = new[] { "password", "123456", "123456789", "qwerty", "abc123", "password123", "admin", "letmein", "welcome" };
        if (commonPasswords.Any(common => password.Equals(common, StringComparison.OrdinalIgnoreCase))) return (false, "Mt khu qu ph bin");

        var hasLower = password.Any(c => char.IsLower(c));
        var hasUpper = password.Any(c => char.IsUpper(c));
        var hasDigit = password.Any(c => char.IsDigit(c));
        var hasSpecial = password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c));

        if (new[] { hasLower, hasUpper, hasDigit, hasSpecial }.Count(x => x) < 3)
            return (false, "Mt khu phi cha t nht 3 trong 4 loi: ch thng, ch hoa, s, k t c bit");

        return (true, string.Empty);
    }

    private async Task<bool> CheckForBruteForcePatternsAsync(string email, string ipAddress)
    {
        var timeWindow = DateTime.UtcNow.AddMinutes(-15);
        var failureEventType = "login_failure";

        var ipFailures = await _context.SecurityEvents.CountAsync(e => e.EventType == failureEventType && e.IPAddress == ipAddress && e.CreatedAt > timeWindow);
        if (ipFailures > 5) return true;

        var ipEvents = await _context.SecurityEvents.Where(e => e.EventType == failureEventType && e.IPAddress == ipAddress && e.CreatedAt > timeWindow).ToListAsync();
        var uniqueEmailsFromIP = ipEvents.Select(e =>
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(e.Details);
            return dict != null && dict.ContainsKey("email") ? dict["email"]?.ToString() : null;
        }).Where(v => !string.IsNullOrEmpty(v)).Distinct().Count();

        if (uniqueEmailsFromIP > 3) return true;

        return false;
    }

    private bool IsSequentialString(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Length < 3) return false;
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
        return false;
    }

    private bool IsSequential(string password)
    {
        for (int i = 0; i < password.Length - 2; i++)
        {
            if (char.IsDigit(password[i]) && char.IsDigit(password[i + 1]) && char.IsDigit(password[i + 2]))
            {
                if (password[i + 1] == password[i] + 1 && password[i + 2] == password[i] + 2) return true;
                if (password[i + 1] == password[i] - 1 && password[i + 2] == password[i] - 2) return true;
            }
        }
        return false;
    }
}
