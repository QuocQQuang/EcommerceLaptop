using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Data;

namespace EcommerceLaptop.Infrastructure.Services;

/// <summary>
/// Secure password reset service implementation following OWASP standards
/// Implements comprehensive security measures including rate limiting, token management, and audit logging
/// </summary>
public class PasswordResetService : IPasswordResetService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasswordResetService> _logger;

    // Configuration constants
    private const int DefaultTokenExpiryMinutes = 15;
    private const int DefaultMaxAttemptsPerHour = 3;
    private const int DefaultMaxAttemptsPerDay = 10;
    private const int DefaultCooldownMinutes = 60;
    private const int DefaultTokenLength = 64;

    public PasswordResetService(
        ApplicationDbContext context,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<PasswordResetService> logger)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PasswordResetResult> InitiatePasswordResetAsync(string email, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            // Normalize email
            email = email.Trim().ToLowerInvariant();

            // Rate limiting check
            var rateLimitResult = await CheckRateLimitAsync(email, ipAddress);
            if (!rateLimitResult.IsAllowed)
            {
                _logger.LogWarning("Password reset rate limit exceeded for email: {Email}, IP: {IP}", email, ipAddress);
                return new PasswordResetResult
                {
                    Success = false,
                    Message = "Quá nhiều yêu cầu đặt lại mật khẩu. Vui lòng thử lại sau.",
                    ErrorCode = "RATE_LIMIT_EXCEEDED",
                    RemainingAttempts = rateLimitResult.RemainingAttempts,
                    CooldownPeriod = rateLimitResult.CooldownPeriod
                };
            }

            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                // Security: Don't reveal if email exists, but log for monitoring
                _logger.LogInformation("Password reset requested for non-existent email: {Email}, IP: {IP}", email, ipAddress);
                
                // Return success to prevent user enumeration attacks
                return new PasswordResetResult
                {
                    Success = true,
                    Message = "Nếu tài khoản tồn tại, chúng tôi đã gửi email đặt lại mật khẩu.",
                    EmailSent = false
                };
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Password reset requested for inactive user: {UserId}, Email: {Email}", user.Id, email);
                
                // Return success to prevent user enumeration attacks
                return new PasswordResetResult
                {
                    Success = true,
                    Message = "Nếu tài khoản tồn tại, chúng tôi đã gửi email đặt lại mật khẩu.",
                    EmailSent = false
                };
            }

            // Invalidate existing tokens for this user
            await InvalidateUserTokensAsync(user.Id);

            // Generate secure token
            var token = GenerateSecureToken();
            var expiryMinutes = _configuration.GetValue("PasswordReset:ExpiryMinutes", DefaultTokenExpiryMinutes);
            var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

            // Create password reset record
            var passwordReset = new PasswordReset
            {
                UserId = user.Id,
                ResetToken = token,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow,
                RequestIpAddress = ipAddress,
                RequestUserAgent = userAgent,
                IsUsed = false
            };

            _context.PasswordResets.Add(passwordReset);
            await _context.SaveChangesAsync();

            // Generate reset URL
            var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
            var resetUrl = $"{frontendUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

            // Send password reset email
            bool emailSent = false;
            try
            {
                emailSent = await _emailService.SendPasswordResetEmailAsync(user, resetUrl);
                
                if (emailSent)
                {
                    _logger.LogInformation("Password reset email sent successfully for user: {UserId}, Email: {Email}", user.Id, email);
                }
                else
                {
                    _logger.LogError("Failed to send password reset email for user: {UserId}, Email: {Email}", user.Id, email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending password reset email for user: {UserId}, Email: {Email}", user.Id, email);
            }

            return new PasswordResetResult
            {
                Success = true,
                Message = "Nếu tài khoản tồn tại, chúng tôi đã gửi email đặt lại mật khẩu.",
                EmailSent = emailSent,
                ExpiresAt = expiresAt,
                RemainingAttempts = rateLimitResult.RemainingAttempts - 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating password reset for email: {Email}", email);
            return new PasswordResetResult
            {
                Success = false,
                Message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau.",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    public async Task<TokenValidationResult> ValidateResetTokenAsync(string email, string token)
    {
        try
        {
            email = email.Trim().ToLowerInvariant();

            var passwordReset = await _context.PasswordResets
                .Include(pr => pr.User)
                .FirstOrDefaultAsync(pr => 
                    pr.User.Email == email && 
                    pr.ResetToken == token && 
                    !pr.IsUsed && 
                    pr.ExpiresAt > DateTime.UtcNow);

            if (passwordReset == null)
            {
                _logger.LogWarning("Invalid password reset token validation attempt for email: {Email}", email);
                return new TokenValidationResult
                {
                    IsValid = false,
Message = "Token không hợp lệ hoặc đã hết hạn.",
                    ErrorCode = "INVALID_TOKEN"
                };
            }

            var minutesRemaining = (int)(passwordReset.ExpiresAt - DateTime.UtcNow).TotalMinutes;

            return new TokenValidationResult
            {
                IsValid = true,
                Message = "Token hợp lệ.",
                ExpiresAt = passwordReset.ExpiresAt,
                MinutesRemaining = minutesRemaining,
                User = passwordReset.User
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating reset token for email: {Email}", email);
            return new TokenValidationResult
            {
                IsValid = false,
                Message = "Đã xảy ra lỗi khi xác thực token.",
                ErrorCode = "VALIDATION_ERROR"
            };
        }
    }

    public async Task<PasswordResetConfirmationResult> ConfirmPasswordResetAsync(string email, string token, string newPassword, string? ipAddress = null)
    {
        try
        {
            email = email.Trim().ToLowerInvariant();

            // Validate token first
            var tokenValidation = await ValidateResetTokenAsync(email, token);
            if (!tokenValidation.IsValid || tokenValidation.User == null)
            {
                return new PasswordResetConfirmationResult
                {
                    Success = false,
                    Message = tokenValidation.Message,
                    ErrorCode = tokenValidation.ErrorCode
                };
            }

            // Validate new password strength
            if (!IsPasswordStrong(newPassword))
            {
                return new PasswordResetConfirmationResult
                {
                    Success = false,
                    Message = "Mật khẩu không đủ mạnh. Vui lòng chọn mật khẩu chứa ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.",
                    ErrorCode = "WEAK_PASSWORD"
                };
            }

            var user = tokenValidation.User;

            // Check if new password is different from current
            if (BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash))
            {
                return new PasswordResetConfirmationResult
                {
                    Success = false,
                    Message = "Mật khẩu mới phải khác với mật khẩu hiện tại.",
                    ErrorCode = "SAME_PASSWORD"
                };
            }

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, 12);
            user.LastPasswordChangeDate = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            // Mark token as used
            var passwordReset = await _context.PasswordResets
                .FirstOrDefaultAsync(pr => 
                    pr.User.Email == email && 
                    pr.ResetToken == token && 
                    !pr.IsUsed);

            if (passwordReset != null)
            {
                passwordReset.IsUsed = true;
                passwordReset.UsedAt = DateTime.UtcNow;
                passwordReset.UsedIpAddress = ipAddress;
            }

            // Invalidate all refresh tokens to force re-login
            var refreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
                .ToListAsync();

            foreach (var refreshToken in refreshTokens)
            {
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.RevokedReason = "Password reset";
                refreshToken.RevokedByIp = ipAddress;
            }

            await _context.SaveChangesAsync();

            // Send password changed notification
            bool notificationSent = false;
            try
            {
                notificationSent = await _emailService.SendPasswordChangedNotificationAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password changed notification for user: {UserId}", user.Id);
            }

            _logger.LogInformation("Password reset successful for user: {UserId}, Email: {Email}, IP: {IP}", user.Id, email, ipAddress);

            return new PasswordResetConfirmationResult
            {
                Success = true,
                Message = "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập lại.",
                ResetAt = DateTime.UtcNow,
                NotificationSent = notificationSent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming password reset for email: {Email}", email);
            return new PasswordResetConfirmationResult
            {
                Success = false,
                Message = "Đã xảy ra lỗi khi đặt lại mật khẩu. Vui lòng thử lại.",
                ErrorCode = "RESET_ERROR"
            };
        }
    }

    public async Task<int> CleanupExpiredTokensAsync()
    {
        try
        {
            var expiredTokens = await _context.PasswordResets
                .Where(pr => pr.ExpiresAt < DateTime.UtcNow || pr.IsUsed)
                .ToListAsync();

            if (expiredTokens.Any())
            {
                _context.PasswordResets.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} expired password reset tokens", expiredTokens.Count);
            }

            return expiredTokens.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired password reset tokens");
            return 0;
        }
    }

    public async Task<RateLimitResult> CheckRateLimitAsync(string email, string? ipAddress = null)
    {
        try
        {
            var now = DateTime.UtcNow;
            var oneHourAgo = now.AddHours(-1);
            var oneDayAgo = now.AddDays(-1);

            var maxAttemptsPerHour = _configuration.GetValue("PasswordReset:MaxAttemptsPerHour", DefaultMaxAttemptsPerHour);
            var maxAttemptsPerDay = _configuration.GetValue("PasswordReset:MaxAttemptsPerDay", DefaultMaxAttemptsPerDay);
            var cooldownMinutes = _configuration.GetValue("PasswordReset:CooldownMinutes", DefaultCooldownMinutes);

            // Check attempts by email in the last hour
            var emailAttemptsInHour = await _context.PasswordResets
                .CountAsync(pr => pr.User.Email == email && pr.CreatedAt >= oneHourAgo);

            // Check attempts by email in the last day
            var emailAttemptsInDay = await _context.PasswordResets
                .CountAsync(pr => pr.User.Email == email && pr.CreatedAt >= oneDayAgo);

            // Check attempts by IP in the last hour (if IP provided)
            var ipAttemptsInHour = 0;
            if (!string.IsNullOrEmpty(ipAddress))
            {
                ipAttemptsInHour = await _context.PasswordResets
                    .CountAsync(pr => pr.RequestIpAddress == ipAddress && pr.CreatedAt >= oneHourAgo);
            }

            // Check if rate limited
            var isEmailRateLimited = emailAttemptsInHour >= maxAttemptsPerHour || emailAttemptsInDay >= maxAttemptsPerDay;
            var isIpRateLimited = ipAttemptsInHour >= maxAttemptsPerHour;

            if (isEmailRateLimited || isIpRateLimited)
            {
                var lastAttempt = await _context.PasswordResets
                    .Where(pr => (pr.User.Email == email || pr.RequestIpAddress == ipAddress))
                    .OrderByDescending(pr => pr.CreatedAt)
                    .FirstOrDefaultAsync();

                var nextAllowedAt = lastAttempt?.CreatedAt.AddMinutes(cooldownMinutes) ?? now;
                var cooldownPeriod = nextAllowedAt > now ? nextAllowedAt - now : TimeSpan.Zero;

                return new RateLimitResult
                {
                    IsAllowed = false,
                    Message = $"Quá nhiều yêu cầu. Vui lòng thử lại sau {cooldownMinutes} phút.",
                    RemainingAttempts = 0,
                    CooldownPeriod = cooldownPeriod,
                    NextAllowedAt = nextAllowedAt
                };
            }

            var remainingEmailAttempts = Math.Max(0, maxAttemptsPerHour - emailAttemptsInHour);
            var remainingDayAttempts = Math.Max(0, maxAttemptsPerDay - emailAttemptsInDay);
            var remainingAttempts = Math.Min(remainingEmailAttempts, remainingDayAttempts);

            return new RateLimitResult
            {
                IsAllowed = true,
                Message = "Yêu cầu được phép.",
                RemainingAttempts = remainingAttempts
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for email: {Email}, IP: {IP}", email, ipAddress);
            
            // On error, allow the request but log it
            return new RateLimitResult
            {
                IsAllowed = true,
                Message = "Không thể kiểm tra giới hạn tốc độ.",
                RemainingAttempts = 1
            };
        }
    }

    public async Task<int> InvalidateUserTokensAsync(int userId)
    {
        try
        {
            var userTokens = await _context.PasswordResets
                .Where(pr => pr.UserId == userId && !pr.IsUsed && pr.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var token in userTokens)
            {
                token.IsUsed = true;
                token.UsedAt = DateTime.UtcNow;
            }

            if (userTokens.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Invalidated {Count} password reset tokens for user: {UserId}", userTokens.Count, userId);
            }

            return userTokens.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating tokens for user: {UserId}", userId);
            return 0;
        }
    }

    public async Task<PasswordResetStatistics> GetStatisticsAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            var resets = await _context.PasswordResets
                .Where(pr => pr.CreatedAt >= fromDate && pr.CreatedAt <= toDate)
                .ToListAsync();

            var statistics = new PasswordResetStatistics
            {
                TotalRequests = resets.Count,
                SuccessfulResets = resets.Count(r => r.IsUsed),
                ExpiredTokens = resets.Count(r => !r.IsUsed && r.ExpiresAt < DateTime.UtcNow),
                RequestsByDay = resets
                    .GroupBy(r => r.CreatedAt.Date.ToString("yyyy-MM-dd"))
                    .ToDictionary(g => g.Key, g => g.Count()),
                TopRequestIPs = resets
                    .Where(r => !string.IsNullOrEmpty(r.RequestIpAddress))
                    .GroupBy(r => r.RequestIpAddress!)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting password reset statistics");
            return new PasswordResetStatistics();
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Generates a cryptographically secure random token
    /// </summary>
    private string GenerateSecureToken()
    {
        var tokenLength = _configuration.GetValue("PasswordReset:TokenLength", DefaultTokenLength);
        
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[tokenLength];
        rng.GetBytes(tokenBytes);
        
        // Convert to URL-safe Base64
        return Convert.ToBase64String(tokenBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Validates password strength according to security requirements
    /// </summary>
    private bool IsPasswordStrong(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return false;

        var hasUpperCase = password.Any(char.IsUpper);
        var hasLowerCase = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecialChar = password.Any(ch => !char.IsLetterOrDigit(ch));

        return hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar;
    }

    #endregion
}